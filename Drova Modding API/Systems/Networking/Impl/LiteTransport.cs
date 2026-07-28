using System.Net;
using System.Net.Sockets;
using Drova_Modding_API.Access;
using LiteNetLib;
using LiteNetLib.Utils;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Owns the LiteNetLib <see cref="NetManager"/> and translates its callbacks into
    /// <see cref="NetworkEvents"/> and <see cref="Dispatcher"/> calls. Events are pumped manually
    /// from <see cref="Poll"/> (called by <c>Core.OnUpdate</c>), so every callback below runs on the
    /// Unity main thread and may touch game state directly.
    ///
    /// Two modes share this class. In <see cref="TransportMode.Direct"/> every remote player is a real
    /// connection; in <see cref="TransportMode.Relay"/> there is one connection to a relay and players
    /// are virtual ids behind it (see <see cref="RelayLink"/>). The public surface is identical either
    /// way - only which start method was called differs.
    /// </summary>
    internal sealed class LiteTransport
    {
        /// <summary>
        /// The channel typed messages travel on; raw channels use 1 and up.
        /// </summary>
        internal const byte TypedChannel = 0;

        /// <summary>
        /// Number of channels the manager multiplexes. Channel 0 is typed, 1..62 are raw, and 63 is
        /// reserved for relay control traffic.
        /// </summary>
        internal const int ChannelCount = 64;

        private const long DropLogIntervalMs = 5_000;

        private readonly EventBasedNetListener _listener = new();
        private readonly RelayLink _link = new();
        private NetManager? _manager;
        private TransportMode _mode = TransportMode.Direct;
        private string? _connectKey;
        private long _undecryptable;
        private long _lastDropLogMs;
        private long _lastRefusedLogMs;

        internal NetRole Role { get; private set; } = NetRole.None;

        internal bool IsConnected => _mode == TransportMode.Relay
            ? _link.Welcomed && _link.PeerCount > 0
            : _manager != null && _manager.ConnectedPeersCount > 0;

        /// <summary>
        /// How many remote peers are currently known.
        /// </summary>
        internal int PeerCount => _mode == TransportMode.Relay
            ? _link.PeerCount
            : _manager?.ConnectedPeersCount ?? 0;

        /// <summary>
        /// Round trip to the relay, or 0 outside a relay session. Every
        /// <see cref="RelayPeer.Ping"/> reports this, because the second leg is not measurable here.
        /// </summary>
        internal int RelayPing => _link.Connection?.Ping ?? 0;

        /// <summary>
        /// The relay session code in use, or null outside a relay session. A host that asked the relay to
        /// pick one sees the assigned code here by the time <see cref="NetworkEvents.OnHostStarted"/> fires.
        /// </summary>
        internal string? SessionCode => _mode == TransportMode.Relay && !string.IsNullOrEmpty(_link.SessionCode)
            ? _link.SessionCode
            : null;

        /// <summary>
        /// Whether payloads are sealed end to end, which needs a password-protected relay session.
        /// </summary>
        internal bool IsEncrypted => _mode == TransportMode.Relay && _link.IsEncrypted;

        internal LiteTransport()
        {
            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _listener.NetworkReceiveEvent += OnNetworkReceive;
            _listener.NetworkErrorEvent += OnNetworkError;
        }

        internal void StartHost(int port, string? key)
        {
            Stop();
            _mode = TransportMode.Direct;
            _connectKey = key;
            _manager = NewManager();
            _manager.Start(port);
            Role = NetRole.Host;
            NetworkEvents.RaiseHostStarted();
        }

        internal void Connect(string address, int port, string? key)
        {
            Stop();
            _mode = TransportMode.Direct;
            _connectKey = key;
            _manager = NewManager();
            _manager.Start();
            _manager.Connect(address, port, key ?? string.Empty);
            Role = NetRole.Client;
        }

        /// <summary>
        /// Claims a session code on a relay, or asks the relay to pick one when
        /// <paramref name="sessionCode"/> is null or empty. Unlike <see cref="StartHost"/> this cannot
        /// report success immediately: <see cref="NetworkEvents.OnHostStarted"/> fires when the relay has
        /// accepted the code, and <see cref="NetworkEvents.OnConnectionRejected"/> when it refused it.
        /// </summary>
        internal void StartHostViaRelay(string address, int port, string? sessionCode, string? password)
        {
            ConnectToRelay(address, port, RelayProtocol.RoleHost, sessionCode, password, NetRole.Host);
        }

        /// <summary>
        /// Joins a session code on a relay. <see cref="NetworkEvents.OnConnectedToHost"/> fires once the
        /// relay has put us in the session.
        /// </summary>
        internal void ConnectViaRelay(string address, int port, string sessionCode, string? password)
        {
            ConnectToRelay(address, port, RelayProtocol.RoleClient, sessionCode, password, NetRole.Client);
        }

        internal void Poll()
        {
            _manager?.PollEvents();
            FlushUndecryptableLog();
            FlushRefusedLog();
        }

        internal void Stop()
        {
            if (_manager != null)
            {
                _manager.Stop();
                _manager = null;
            }
            _link.Reset();
            _mode = TransportMode.Direct;
            Role = NetRole.None;
        }

        /// <summary>
        /// Appends the current peers to the buffer. The caller owns the list, so repeated calls with the
        /// same list do not allocate.
        /// </summary>
        internal void CopyPeers(List<INetPeer> buffer)
        {
            if (_mode == TransportMode.Relay)
            {
                _link.CopyPeers(buffer);
                return;
            }

            if (_manager == null) return;

            // The manager's enumerator is a struct, so walking connected peers does not allocate.
            foreach (NetPeer peer in _manager)
            {
                buffer.Add(PeerWrapper.Of(peer));
            }
        }

        internal void SendTyped<T>(INetPeer peer, in T message, Delivery delivery) where T : struct, INetMessage
        {
            if (_mode == TransportMode.Relay)
            {
                if (!TryRelayTarget(peer, out byte target)) return;

                SendTypedViaRelay(target, message, delivery);
                return;
            }

            if (!TryDirectPeer(peer, out PeerWrapper? wrapper)) return;

            NetDataWriter writer = Dispatcher.BuildTyped(message);
            wrapper!.Native.Send(writer, TypedChannel, Map(delivery));
        }

        internal void SendTypedToAll<T>(in T message, Delivery delivery) where T : struct, INetMessage
        {
            if (_mode == TransportMode.Relay)
            {
                if (!RelayReady()) return;

                SendTypedViaRelay(RelayLink.TargetForAll(Role), message, delivery);
                return;
            }

            if (_manager == null)
            {
                return;
            }
            // Serialised once and fanned out by the manager, unlike a per-peer loop.
            NetDataWriter writer = Dispatcher.BuildTyped(message);
            _manager.SendToAll(writer, TypedChannel, Map(delivery));
        }

        internal void SendRaw(INetPeer peer, byte channel, ReadOnlySpan<byte> data, Delivery delivery)
        {
            if (_mode == TransportMode.Relay)
            {
                if (!TryRelayTarget(peer, out byte target)) return;

                SendRawViaRelay(target, channel, data, delivery);
                return;
            }

            if (!TryDirectPeer(peer, out PeerWrapper? wrapper)) return;

            wrapper!.Native.Send(data, channel, Map(delivery));
        }

        internal void SendRawToAll(byte channel, ReadOnlySpan<byte> data, Delivery delivery)
        {
            if (_mode == TransportMode.Relay)
            {
                if (!RelayReady()) return;

                SendRawViaRelay(RelayLink.TargetForAll(Role), channel, data, delivery);
                return;
            }

            if (_manager == null)
            {
                return;
            }
            DeliveryMethod method = Map(delivery);
            // The manager's enumerator is a struct, so iterating connected peers does not allocate.
            foreach (NetPeer peer in _manager)
            {
                peer.Send(data, channel, method);
            }
        }

        /// <summary>
        /// Backs <see cref="RelayPeer.Disconnect"/>. A host asks the relay to drop the client; a client
        /// asking to drop its host leaves the session instead, since it has no authority over the host.
        /// </summary>
        internal void DisconnectRelayPeer(RelayPeer peer)
        {
            if (!RelayReady()) return;

            if (Role != NetRole.Host || peer.VirtualId == RelayProtocol.HostVirtualId)
            {
                Stop();
                return;
            }

            NetDataWriter control = _link.BuildControl(RelayProtocol.ControlKickPeer, peer.VirtualId);
            _link.Connection!.Send(control, RelayProtocol.ControlChannel, DeliveryMethod.ReliableOrdered);

            // The relay does not report back a kick the host itself asked for, so the event is raised
            // here to keep both roles seeing a departure exactly once.
            RelayPeer? removed = _link.Remove(peer.VirtualId);
            if (removed != null) NetworkEvents.RaisePeerDisconnected(removed, DisconnectReason.LocalClose);
        }

        private void ConnectToRelay(string address, int port, byte role, string? sessionCode, string? password, NetRole localRole)
        {
            Stop();
            _mode = TransportMode.Relay;
            _connectKey = null;
            _link.Prepare(sessionCode, password);
            _manager = NewManager();
            _manager.Start();

            // Derived here rather than in the link, because it has to exist before the connect data does
            // and it is the only half of the password the relay is ever allowed to see. Deliberately not
            // dependent on the session code: a host asking the relay to pick one does not know it yet.
            byte[]? joinToken = string.IsNullOrEmpty(password) ? null : SessionCrypto.DeriveJoinToken(password);

            _manager.Connect(address, port, RelayProtocol.BuildHandshake(role, sessionCode, joinToken));
            Role = localRole;
        }

        private void SendTypedViaRelay<T>(byte target, in T message, Delivery delivery) where T : struct, INetMessage
        {
            // Serialised into the shared writer exactly as a direct send would be, then framed - and
            // sealed, when the session has a key - on its way into the relay's envelope.
            NetDataWriter plaintext = Dispatcher.BuildTyped(message);
            SendFramed(target, TypedChannel, new ReadOnlySpan<byte>(plaintext.Data, 0, plaintext.Length), delivery);
        }

        private void SendRawViaRelay(byte target, byte channel, ReadOnlySpan<byte> data, Delivery delivery)
        {
            SendFramed(target, channel, data, delivery);
        }

        private void SendFramed(byte target, byte channel, ReadOnlySpan<byte> payload, Delivery delivery)
        {
            int length = _link.Frame(target, channel, payload);
            _link.Connection!.Send(_link.FrameBuffer, 0, length, channel, Map(delivery));
        }

        private bool RelayReady()
        {
            return _link.Welcomed && _link.Connection != null;
        }

        private bool TryRelayTarget(INetPeer peer, out byte target)
        {
            target = 0;
            if (!RelayReady()) return false;

            if (peer is not RelayPeer relayPeer)
            {
                MelonLogger.Error("[Networking] a peer from a direct session was used in a relay session");
                return false;
            }

            target = relayPeer.VirtualId;
            return true;
        }

        private bool TryDirectPeer(INetPeer peer, out PeerWrapper? wrapper)
        {
            wrapper = peer as PeerWrapper;
            if (wrapper != null) return true;

            MelonLogger.Error("[Networking] a peer from a relay session was used in a direct session");
            return false;
        }

        private NetManager NewManager()
        {
            return new NetManager(_listener)
            {
                UnsyncedEvents = false,
                AutoRecycle = false,
                ChannelsCount = ChannelCount,

                // How often queued sends are actually flushed. The default is 15 ms, and that delay is
                // added to every message this transport carries. Measured end to end through a relay,
                // where a round trip crosses four such queues, it accounted for most of a 50 ms round trip
                // between two peers running on the same machine.
                //
                // For a game sending player state every frame that wait buys nothing: the packet is
                // complete and held back for a timer. One millisecond spends a little more CPU on smaller,
                // more frequent flushes, which is the right trade for traffic made of small messages that
                // are worthless as soon as the next one supersedes them.
                UpdateTime = 1,
            };
        }

        private void OnConnectionRequest(ConnectionRequest request)
        {
            // Only a direct host listens for requests; in relay mode the relay does the accepting.
            if (_mode == TransportMode.Relay)
            {
                request.Reject();
                return;
            }

            // No key means an open host; otherwise the client must present the matching key.
            if (string.IsNullOrEmpty(_connectKey))
            {
                request.Accept();
            }
            else
            {
                request.AcceptIfKey(_connectKey);
            }
        }

        private void OnPeerConnected(NetPeer peer)
        {
            // In relay mode the connection being up says nothing about the session; the relay's welcome
            // does, so the events wait for it.
            if (_mode == TransportMode.Relay)
            {
                _link.Attach(peer);
                return;
            }

            PeerWrapper wrapper = PeerWrapper.Of(peer);
            // On a client, the first (and only) peer is the host; announce that before the generic
            // peer-connected event so a subscriber can special-case the host.
            if (Role == NetRole.Client)
            {
                NetworkEvents.RaiseConnectedToHost();
            }
            NetworkEvents.RaisePeerConnected(wrapper);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (_mode == TransportMode.Relay)
            {
                OnRelayDisconnected(disconnectInfo);
                return;
            }

            if (disconnectInfo.Reason == LiteNetLib.DisconnectReason.ConnectionRejected)
            {
                NetworkEvents.RaiseConnectionRejected(ConnectionRejection.Unspecified);
            }

            PeerWrapper wrapper = PeerWrapper.Of(peer);
            NetworkEvents.RaisePeerDisconnected(wrapper, Map(disconnectInfo.Reason));
        }

        private void OnRelayDisconnected(DisconnectInfo disconnectInfo)
        {
            byte code = disconnectInfo.AdditionalData.AvailableBytes > 0 ? disconnectInfo.AdditionalData.GetByte() : (byte)0;

            if (disconnectInfo.Reason == LiteNetLib.DisconnectReason.ConnectionRejected)
            {
                NetworkEvents.RaiseConnectionRejected(RelayProtocol.MapRejection(code));
            }

            DisconnectReason reason = code switch
            {
                RelayProtocol.DisconnectSessionClosed => DisconnectReason.SessionClosed,
                RelayProtocol.DisconnectKicked => DisconnectReason.Kicked,
                _ => Map(disconnectInfo.Reason),
            };

            // Losing the one relay connection means losing every player behind it, so each is announced
            // before the session state goes away.
            _link.ForEachPeer(lost => NetworkEvents.RaisePeerDisconnected(lost, reason));
            _link.Reset();
            Role = NetRole.None;
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                if (_mode == TransportMode.Relay)
                {
                    OnRelayReceive(reader, channel);
                    return;
                }

                PeerWrapper wrapper = PeerWrapper.Of(peer);
                if (channel == TypedChannel)
                {
                    Dispatcher.OnTypedReceived(wrapper, reader);
                }
                else
                {
                    Dispatcher.OnRawReceived(wrapper, channel, reader.GetRemainingBytesSpan());
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Networking] receive on channel " + channel + " failed: " + e);
            }
            finally
            {
                // AutoRecycle is off, so return the reader to LiteNetLib's pool by hand.
                reader.Recycle();
            }
        }

        private void OnRelayReceive(NetPacketReader reader, byte channel)
        {
            if (reader.AvailableBytes < 1) return;

            byte sender = reader.GetByte();

            if (channel == RelayProtocol.ControlChannel && sender == RelayProtocol.RelayVirtualId)
            {
                OnRelayControl(reader);
                return;
            }

            // Channels are independent, so a payload can outrun the control message announcing its
            // sender. Whoever the relay stamped is a peer, whether we have been told about them yet.
            RelayPeer relayPeer = _link.EnsurePeer(this, sender, out bool created);
            if (created) NetworkEvents.RaisePeerConnected(relayPeer);

            if (!_link.IsEncrypted)
            {
                if (channel == TypedChannel)
                {
                    Dispatcher.OnTypedReceived(relayPeer, reader);
                }
                else
                {
                    Dispatcher.OnRawReceived(relayPeer, channel, reader.GetRemainingBytesSpan());
                }

                return;
            }

            if (!_link.TryOpen(sender, channel, reader.GetRemainingBytesSpan(), out int length))
            {
                // Either the password differs from the host's or something rewrote the packet. Both are
                // indistinguishable here and both mean the same thing: it is not ours.
                _undecryptable++;
                return;
            }

            if (channel == TypedChannel)
            {
                Dispatcher.OnTypedReceived(relayPeer, _link.PlainReader(length));
            }
            else
            {
                Dispatcher.OnRawReceived(relayPeer, channel, new ReadOnlySpan<byte>(_link.PlainBuffer, 0, length));
            }
        }

        private void OnRelayControl(NetPacketReader reader)
        {
            if (reader.AvailableBytes < 1) return;

            byte opcode = reader.GetByte();

            // The assigned code is the one opcode whose argument is a string, so it has to be split off
            // before anything reads a single argument byte.
            if (opcode == RelayProtocol.ControlSessionCode)
            {
                string assigned = reader.GetString(RelayProtocol.MaxSessionCodeLength);
                if (assigned.Length > 0) _link.AssignSessionCode(assigned);
                return;
            }

            if (reader.AvailableBytes < 1) return;

            byte argument = reader.GetByte();

            switch (opcode)
            {
                case RelayProtocol.ControlWelcome:
                    OnRelayWelcome(argument);
                    break;
                case RelayProtocol.ControlPeerJoined:
                    RelayPeer joined = _link.EnsurePeer(this, argument, out bool created);
                    if (created) NetworkEvents.RaisePeerConnected(joined);
                    break;
                case RelayProtocol.ControlPeerLeft:
                    RelayPeer? left = _link.Remove(argument);
                    if (left != null) NetworkEvents.RaisePeerDisconnected(left, DisconnectReason.RemoteClose);
                    break;
            }
        }

        /// <summary>
        /// Summarizes messages the dispatcher refused for carrying a NaN or an infinity.
        ///
        /// Worth saying out loud rather than dropping in silence: a mod whose messages stop arriving with
        /// no explanation is a bug hunt in the wrong repository, and the honest answer - somebody is
        /// sending numbers no game can use - is only visible here.
        /// </summary>
        private void FlushRefusedLog()
        {
            long now = Environment.TickCount64;
            if (now - _lastRefusedLogMs < DropLogIntervalMs) return;

            long refused = Dispatcher.TakeRefused();
            if (refused == 0) return;

            _lastRefusedLogMs = now;

            MelonLogger.Warning("[Networking] dropped " + refused + " messages carrying a NaN or an infinity; " +
                "a peer is sending numbers no game can use, whether through a bug or on purpose.");
        }

        private void FlushUndecryptableLog()
        {
            if (_undecryptable == 0) return;

            long now = Environment.TickCount64;
            if (now - _lastDropLogMs < DropLogIntervalMs) return;

            // Counters are cleared before the log call, not after: if logging itself fails, the retry would
            // otherwise repeat on every single poll.
            long dropped = _undecryptable;
            _undecryptable = 0;
            _lastDropLogMs = now;

            // Summarised rather than logged per packet: a mismatched password makes every single packet
            // fail, and that must not turn into a log flood on top of a session that already will not work.
            MelonLogger.Error("[Networking] dropped " + dropped + " packets that failed to decrypt; do both sides use the same session password?");
        }

        private void OnRelayWelcome(byte localVirtualId)
        {
            _link.Welcome(localVirtualId);

            if (Role == NetRole.Host)
            {
                NetworkEvents.RaiseHostStarted();
                return;
            }

            NetworkEvents.RaiseConnectedToHost();
            RelayPeer host = _link.EnsurePeer(this, RelayProtocol.HostVirtualId, out bool created);
            if (created) NetworkEvents.RaisePeerConnected(host);
        }

        private void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            NetworkEvents.RaiseNetworkError(endPoint + ": " + socketError);
        }

        private static DeliveryMethod Map(Delivery delivery)
        {
            return delivery switch
            {
                Delivery.Unreliable => DeliveryMethod.Unreliable,
                Delivery.ReliableUnordered => DeliveryMethod.ReliableUnordered,
                Delivery.Sequenced => DeliveryMethod.Sequenced,
                Delivery.ReliableSequenced => DeliveryMethod.ReliableSequenced,
                _ => DeliveryMethod.ReliableOrdered,
            };
        }

        private static DisconnectReason Map(LiteNetLib.DisconnectReason reason)
        {
            return reason switch
            {
                LiteNetLib.DisconnectReason.ConnectionFailed => DisconnectReason.ConnectionFailed,
                LiteNetLib.DisconnectReason.HostUnreachable => DisconnectReason.ConnectionFailed,
                LiteNetLib.DisconnectReason.NetworkUnreachable => DisconnectReason.ConnectionFailed,
                LiteNetLib.DisconnectReason.Timeout => DisconnectReason.Timeout,
                LiteNetLib.DisconnectReason.RemoteConnectionClose => DisconnectReason.RemoteClose,
                LiteNetLib.DisconnectReason.DisconnectPeerCalled => DisconnectReason.LocalClose,
                LiteNetLib.DisconnectReason.ConnectionRejected => DisconnectReason.Rejected,
                _ => DisconnectReason.Unknown,
            };
        }
    }
}
