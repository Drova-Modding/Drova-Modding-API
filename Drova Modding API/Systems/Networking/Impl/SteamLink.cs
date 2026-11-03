using System.Runtime.InteropServices;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Networking.Diagnostics;
using LiteNetLib.Utils;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// A session over Steam's peer-to-peer messages. The host lets in its Steam friends, and anyone while its lobby is open.
    /// Everything travels on Steam channel 0 behind one byte naming the API channel. Channel 63 carries the link's own
    /// hello, welcome and goodbye. Steam's session callbacks run inside <see cref="Poll"/>, on the main thread.
    /// </summary>
    internal sealed unsafe class SteamLink
    {
        private const byte OpHello = 1;
        private const byte OpWelcome = 2;
        private const byte OpBye = 3;

        private const int ReceiveBatch = 64;

        private const string PlayerGroupKey = "steam_player_group";

        /// <summary>Steam's largest message.</summary>
        private const int MaxMessageBytes = 512 * 1024;

        /// <summary>k_ESteamNetworkingConfig_TimeoutInitial.</summary>
        private const int ConfigTimeoutInitial = 24;

        /// <summary>k_ESteamNetworkingConfig_TimeoutConnected.</summary>
        private const int ConfigTimeoutConnected = 25;

        /// <summary>A story teleport stalls the game for well over Steam's 10 s.</summary>
        private const int TimeoutMs = 30_000;

        /// <summary>
        /// k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable: 0 is relay only, -1 Steam's default. ICE setup stalls the main
        /// thread ~2 s on every first session to a peer, see <see cref="SteamAccess.AllowDirectConnections"/>.
        /// </summary>
        private const int ConfigIceEnable = 104;

        private const long PingRefreshMs = 1_000;

        /// <summary>Size of <c>SteamNetConnectionRealTimeStatus_t</c>, rounded up. The ping is its second int.</summary>
        private const int StatusSize = 256;

        private static SessionCallback? _onRequest;
        private static SessionCallback? _onFailed;
        private static SteamLink? _active;

        private readonly Action _hostLost;
        private readonly Dictionary<ulong, SteamPeer> _peers = new();
        private readonly List<SteamPeer> _scratch = new();
        private readonly byte[] _receiveBuffer = new byte[MaxMessageBytes];
        private readonly byte[] _sendBuffer = new byte[MaxMessageBytes];
        private readonly NetDataReader _reader = new();
        private readonly IntPtr[] _received = new IntPtr[ReceiveBatch];

        private NetRole _role;
        private ulong _hostId;
        private int _nextPeerId = 1;
        private long _lastPingMs;
        private bool _loggedSendFailure;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SessionCallback(IntPtr identity);

        internal SteamLink(Action hostLost)
        {
            _hostLost = hostLost;
        }

        internal int PeerCount => _peers.Count;

        internal bool IsConnected => _peers.Count > 0;

        /// <summary>False without Steam, as on a GOG copy.</summary>
        internal bool StartHost()
        {
            if (!Prepare(NetRole.Host)) return false;

            SetPlayerGroup(SteamNative.LocalId);
            MelonLogger.Msg($"[Networking] hosting over Steam as {SteamNative.LocalId}.");
            return true;
        }

        /// <summary>Asks a host for a place. The host's welcome arrives through <see cref="Poll"/>.</summary>
        internal bool Connect(ulong hostId)
        {
            if (!Prepare(NetRole.Client)) return false;

            _hostId = hostId;
            SendControl(hostId, OpHello);
            return true;
        }

        private bool Prepare(NetRole role)
        {
            Stop();

            if (!SteamNative.Available) return false;

            InstallCallbacks();
            SteamNative.SteamAPI_ISteamNetworkingUtils_SetGlobalConfigValueInt32(SteamNative.Utils, ConfigTimeoutInitial, TimeoutMs);
            SteamNative.SteamAPI_ISteamNetworkingUtils_SetGlobalConfigValueInt32(SteamNative.Utils, ConfigTimeoutConnected, TimeoutMs);
            SteamNative.SteamAPI_ISteamNetworkingUtils_SetGlobalConfigValueInt32(SteamNative.Utils, ConfigIceEnable,
                SteamAccess.AllowDirectConnections ? -1 : 0);
            SteamNative.SteamAPI_ISteamNetworkingUtils_InitRelayNetworkAccess(SteamNative.Utils);

            _role = role;
            _active = this;
            return true;
        }

        internal void Poll()
        {
            if (_active != this) return;

            SteamNative.SteamAPI_ISteamNetworkingSockets_RunCallbacks(SteamNative.Sockets);

            int count;
            do
            {
                fixed (IntPtr* messages = _received)
                {
                    count = SteamNative.SteamAPI_ISteamNetworkingMessages_ReceiveMessagesOnChannel(SteamNative.Messages, 0, messages, ReceiveBatch);
                }

                for (int index = 0; index < count; index++)
                {
                    IntPtr message = _received[index];
                    try
                    {
                        Receive(message);
                    }
                    catch (Exception e)
                    {
                        NetworkDiagnostics.CountReceiveFailure();
                        MelonLogger.Error("[Networking] a Steam message failed: " + e);
                    }
                    finally
                    {
                        SteamNative.SteamAPI_SteamNetworkingMessage_t_Release(message);
                    }

                    // A handler may have ended the session. The rest of the batch is still Steam's memory to give back.
                    if (_active != this)
                    {
                        for (int rest = index + 1; rest < count; rest++)
                        {
                            SteamNative.SteamAPI_SteamNetworkingMessage_t_Release(_received[rest]);
                        }

                        return;
                    }
                }
            }
            while (count == ReceiveBatch);

            RefreshPings();
        }

        private void Receive(IntPtr message)
        {
            byte* data = *(byte**)message;
            int size = *(int*)((byte*)message + 8);
            ulong sender = SteamNative.IdOf((byte*)message + SteamNative.MessagePeerOffset);

            if (size < 1) return;

            byte channel = data[0];
            if (channel == LiteTransport.ControlChannel)
            {
                if (size >= 2) OnControl(sender, data[1]);
                return;
            }

            if (!_peers.TryGetValue(sender, out SteamPeer? peer)) return;

            int length = size - 1;
            new ReadOnlySpan<byte>(data + 1, length).CopyTo(_receiveBuffer);

            if (channel == LiteTransport.TypedChannel)
            {
                _reader.SetSource(_receiveBuffer, 0, length);
                Dispatcher.OnTypedReceived(peer, _reader);
            }
            else
            {
                Dispatcher.OnRawReceived(peer, channel, new ReadOnlySpan<byte>(_receiveBuffer, 0, length));
            }
        }

        private void OnControl(ulong sender, byte opcode)
        {
            switch (opcode)
            {
                case OpHello when _role == NetRole.Host:
                    // Steam authenticates the sender id.
                    if (!MayJoin(sender)) return;

                    bool fresh = !_peers.ContainsKey(sender);
                    SteamPeer joined = GetOrAdd(sender);

                    // Repeated, since a hello can be answered while the joiner is still waiting on an earlier welcome.
                    SendControl(sender, OpWelcome);
                    if (fresh) NetworkEvents.RaisePeerConnected(joined);
                    break;

                case OpWelcome when _role == NetRole.Client && sender == _hostId:
                    if (_peers.ContainsKey(sender)) return;

                    SteamPeer host = GetOrAdd(sender);
                    SetPlayerGroup(sender);
                    // Before the generic event, so a subscriber can special-case the host.
                    NetworkEvents.RaiseConnectedToHost();
                    NetworkEvents.RaisePeerConnected(host);
                    break;

                case OpBye:
                    Lose(sender, DisconnectReason.RemoteClose);
                    break;
            }
        }

        private SteamPeer GetOrAdd(ulong id)
        {
            if (_peers.TryGetValue(id, out SteamPeer? existing)) return existing;

            SteamPeer peer = new(this, id, _nextPeerId++);
            _peers[id] = peer;

            // Lists the player under "Recent players" in Steam, to befriend or invite again.
            SteamNative.SteamAPI_ISteamFriends_SetPlayedWith(SteamNative.Friends, id);
            return peer;
        }

        private static bool MayJoin(ulong id) => SteamNative.IsFriend(id) || SteamLobbies.IsOpen;

        private void Lose(ulong id, DisconnectReason reason)
        {
            if (_peers.Remove(id, out SteamPeer? peer))
            {
                Close(id);
                NetworkEvents.RaisePeerDisconnected(peer, reason);
            }

            if (_role == NetRole.Client && id == _hostId)
            {
                Stop();
                _hostLost();
            }
        }

        internal void Disconnect(SteamPeer peer)
        {
            if (_active != this || !_peers.ContainsKey(peer.SteamId)) return;

            // A client cannot evict its host, so asking to is leaving.
            if (_role == NetRole.Client)
            {
                Stop();
                _hostLost();
                return;
            }

            SendControl(peer.SteamId, OpBye);
            Lose(peer.SteamId, DisconnectReason.LocalClose);
        }

        internal void Stop()
        {
            if (_active == this)
            {
                foreach (ulong id in _peers.Keys)
                {
                    SendControl(id, OpBye);
                    Close(id);
                }

                // A joiner still waiting on its welcome has a session too.
                if (_hostId != 0 && !_peers.ContainsKey(_hostId)) Close(_hostId);

                SetPlayerGroup(0);
                SteamLobbies.Close();
                _active = null;
            }

            _peers.Clear();
            _role = NetRole.None;
            _hostId = 0;
        }

        internal void CopyPeers(List<INetPeer> buffer)
        {
            foreach (SteamPeer peer in _peers.Values)
            {
                buffer.Add(peer);
            }
        }

        internal void Send(SteamPeer peer, byte channel, ReadOnlySpan<byte> payload, Delivery delivery)
        {
            if (_active != this || !_peers.ContainsKey(peer.SteamId))
            {
                NetworkDiagnostics.CountSendWithoutSession();
                return;
            }

            SendFramed(peer.SteamId, channel, payload, delivery);
        }

        internal void SendToAll(byte channel, ReadOnlySpan<byte> payload, Delivery delivery)
        {
            if (_active != this)
            {
                NetworkDiagnostics.CountSendWithoutSession();
                return;
            }

            foreach (ulong id in _peers.Keys)
            {
                SendFramed(id, channel, payload, delivery);
            }
        }

        private void SendControl(ulong id, byte opcode)
        {
            ReadOnlySpan<byte> payload = stackalloc byte[] { opcode };
            SendFramed(id, LiteTransport.ControlChannel, payload, Delivery.Reliable);
        }

        private void SendFramed(ulong id, byte channel, ReadOnlySpan<byte> payload, Delivery delivery)
        {
            if (payload.Length + 1 > MaxMessageBytes)
            {
                NetworkDiagnostics.CountOversizedSend();
                return;
            }

            _sendBuffer[0] = channel;
            payload.CopyTo(new Span<byte>(_sendBuffer, 1, payload.Length));

            // Sequenced rides as plain unreliable, so a rare Steam reorder can deliver a stale packet.
            int flags = delivery is Delivery.Unreliable or Delivery.Sequenced ? SteamNative.SendUnreliable : SteamNative.SendReliable;

            byte* identity = stackalloc byte[SteamNative.IdentitySize];
            SteamNative.SetIdentity(identity, id);

            int result;
            fixed (byte* data = _sendBuffer)
            {
                result = SteamNative.SteamAPI_ISteamNetworkingMessages_SendMessageToUser(SteamNative.Messages, identity, data,
                    (uint)(payload.Length + 1), flags, 0);
            }

            // 1 is k_EResultOK.
            if (result != 1 && !_loggedSendFailure)
            {
                _loggedSendFailure = true;
                MelonLogger.Warning($"[Networking] Steam refused a message to {id} (EResult {result}); later failures are not logged.");
            }
        }

        /// <summary>Steam lists friends in the same group as playing together. The host's id names the group, 0 leaves it.</summary>
        private static void SetPlayerGroup(ulong hostId)
        {
            SteamNative.SteamAPI_ISteamFriends_SetRichPresence(SteamNative.Friends, PlayerGroupKey, hostId == 0 ? null : hostId.ToString());
        }

        private static void Close(ulong id)
        {
            byte* identity = stackalloc byte[SteamNative.IdentitySize];
            SteamNative.SetIdentity(identity, id);
            SteamNative.SteamAPI_ISteamNetworkingMessages_CloseSessionWithUser(SteamNative.Messages, identity);
        }

        private void RefreshPings()
        {
            long now = Environment.TickCount64;
            if (now - _lastPingMs < PingRefreshMs) return;

            _lastPingMs = now;

            byte* identity = stackalloc byte[SteamNative.IdentitySize];
            byte* status = stackalloc byte[StatusSize];

            _scratch.Clear();
            _scratch.AddRange(_peers.Values);

            foreach (SteamPeer peer in _scratch)
            {
                SteamNative.SetIdentity(identity, peer.SteamId);
                SteamNative.SteamAPI_ISteamNetworkingMessages_GetSessionConnectionInfo(SteamNative.Messages, identity, IntPtr.Zero, status);

                // Steam measures the round trip. A peer reports one way.
                peer.Ping = Math.Max(0, *(int*)(status + 4) / 2);
            }
        }

        private static void InstallCallbacks()
        {
            if (_onRequest != null) return;

            // Held so Steam's pointers stay valid.
            _onRequest = OnSessionRequest;
            _onFailed = OnSessionFailed;

            SteamNative.SteamAPI_ISteamNetworkingUtils_SetGlobalCallback_MessagesSessionRequest(SteamNative.Utils,
                Marshal.GetFunctionPointerForDelegate(_onRequest));
            SteamNative.SteamAPI_ISteamNetworkingUtils_SetGlobalCallback_MessagesSessionFailed(SteamNative.Utils,
                Marshal.GetFunctionPointerForDelegate(_onFailed));
        }

        /// <summary>Accepts the same senders whose hello would be welcomed.</summary>
        private static void OnSessionRequest(IntPtr request)
        {
            try
            {
                SteamLink? link = _active;
                if (link == null) return;

                // SteamNetworkingMessagesSessionRequest_t starts with the remote identity.
                ulong id = SteamNative.IdOf((byte*)request);
                bool wanted = link._role == NetRole.Host ? MayJoin(id) : id == link._hostId;
                if (wanted) SteamNative.SteamAPI_ISteamNetworkingMessages_AcceptSessionWithUser(SteamNative.Messages, (byte*)request);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Networking] answering a Steam session request failed: " + e);
            }
        }

        /// <summary>A session dropped: timed out, or the other end could not be reached at all.</summary>
        private static void OnSessionFailed(IntPtr info)
        {
            try
            {
                SteamLink? link = _active;
                if (link == null) return;

                // SteamNetConnectionInfo_t starts with the remote identity.
                ulong id = SteamNative.IdOf((byte*)info);

                if (link._role == NetRole.Client && id == link._hostId && !link._peers.ContainsKey(id))
                {
                    NetworkEvents.RaiseNetworkError("could not reach the host over Steam");
                }

                link.Lose(id, DisconnectReason.Timeout);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Networking] handling a lost Steam session failed: " + e);
            }
        }
    }

    internal sealed class SteamPeer : INetPeer
    {
        private readonly SteamLink _link;

        internal SteamPeer(SteamLink link, ulong steamId, int id)
        {
            _link = link;
            SteamId = steamId;
            Id = id;
        }

        internal ulong SteamId { get; }

        /// <inheritdoc/>
        public int Id { get; }

        /// <inheritdoc/>
        public int Ping { get; internal set; }

        /// <inheritdoc/>
        public void Disconnect()
        {
            _link.Disconnect(this);
        }
    }
}
