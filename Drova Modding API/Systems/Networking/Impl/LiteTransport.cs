using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Networking.Diagnostics;
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
    /// LiteNetLib connection. In <see cref="TransportMode.Steam"/> <see cref="SteamLink"/> carries the
    /// traffic. The public surface is the same in both modes.
    /// </summary>
    internal sealed class LiteTransport
    {
        /// <summary>
        /// The channel typed messages travel on; raw channels use 1 and up.
        /// </summary>
        internal const byte TypedChannel = 0;

        /// <summary>
        /// Number of channels the manager multiplexes. Channel 0 is typed, 1..62 are raw, and
        /// <see cref="ControlChannel"/> is reserved.
        /// </summary>
        internal const int ChannelCount = 64;

        /// <summary>
        /// The channel <see cref="SteamLink"/> keeps for its own hello, welcome and goodbye.
        /// </summary>
        internal const byte ControlChannel = 63;

        private const long DropLogIntervalMs = 5_000;

        /// <summary>At LiteNetLib's 500 ms between attempts, two minutes for the joiner's address to reach the host.</summary>
        private const int PunchConnectAttempts = 240;

        private const int MaxFragments = 512;

        private readonly EventBasedNetListener _listener = new();
        private readonly SteamLink _steam;
        private NetManager? _manager;
        private NatLayer? _nat;
        private PortMapping? _portMapping;
        private TransportMode _mode = TransportMode.Direct;

        /// <summary>
        /// A test-only link delay from <c>--net-delay minMs-maxMs</c>: every received packet waits a random time in
        /// that range before it is handed on. Two instances on one machine talk with no delay at all, and a
        /// coop fault that needs two messages to land in one frame never shows there. Ordered and sequenced
        /// traffic keeps its order, as LiteNetLib guarantees it; plain unreliable packets may overtake each other.
        /// </summary>
        private static readonly (int Min, int Max)? _netDelay = ReadNetDelay();
        private readonly List<(long Due, NetPeer Peer, NetPacketReader Reader, byte Channel, DeliveryMethod Method)> _delayed = new();
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        private readonly Random _jitter = new();
        private long _lastOrderedDue;

        private static (int Min, int Max)? ReadNetDelay()
        {
            string? value = Testing.LaunchProfile.ReadArgument("--net-delay");
            if (value == null) return null;

            string[] parts = value.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int min) || !int.TryParse(parts[1], out int max) || min < 0 || max < min)
            {
                MelonLogger.Warning("[Networking] --net-delay wants minMs-maxMs; ignored: " + value);
                return null;
            }

            MelonLogger.Msg($"[Networking] test link delay: every received packet waits {min}-{max} ms.");
            return (min, max);
        }
        private string? _connectKey;
        private long _loggedRefused;
        private long _loggedWithoutSession;
        private long _lastRefusedLogMs;
        private long _lastWithoutSessionLogMs;

        internal NetRole Role { get; private set; } = NetRole.None;

        /// <summary>
        /// How this session reaches the other players. Meaningless outside a session, where it is left at
        /// whatever the last one used.
        /// </summary>
        internal TransportMode Mode => _mode;

        /// <summary>
        /// LiteNetLib's manager, for reading the counters it keeps. Null while no session is running.
        /// </summary>
        internal NetManager? Manager => _manager;

        /// <summary>This direct session's address on the internet, once a STUN server answered.</summary>
        internal IPEndPoint? PublicEndPoint => _nat?.PublicEndPoint;

        /// <summary>Completes once the router closed the last session's port, or at once when none was open.</summary>
        internal Task PortClosed { get; private set; } = Task.CompletedTask;

        internal bool IsConnected => _mode switch
        {
            TransportMode.Steam => _steam.IsConnected,
            _ => _manager != null && _manager.ConnectedPeersCount > 0,
        };

        /// <summary>
        /// How many remote peers are currently known.
        /// </summary>
        internal int PeerCount => _mode switch
        {
            TransportMode.Steam => _steam.PeerCount,
            _ => _manager?.ConnectedPeersCount ?? 0,
        };

        internal LiteTransport()
        {
            _steam = new SteamLink(() => Role = NetRole.None);
            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _listener.NetworkReceiveEvent += OnNetworkReceive;
            _listener.NetworkErrorEvent += OnNetworkError;
            _listener.NetworkReceiveUnconnectedEvent += OnReceiveUnconnected;
        }

        internal void StartHost(int port, string? key)
        {
            Stop();
            // Reset on start rather than in Stop, so an ended session's numbers stay readable.
            NetworkDiagnostics.Reset();
            _mode = TransportMode.Direct;
            _connectKey = key;
            _manager = NewManager();
            // Only a direct host hears LAN discovery.
            _manager.BroadcastReceiveEnabled = true;
            if (!_manager.Start(port))
            {
                Stop();
                NetworkEvents.RaiseNetworkError($"could not listen on UDP port {port}; another program may be using it");
                return;
            }

            if (NetworkAccess.UsePortMapping) _portMapping = new PortMapping(_manager.LocalPort);

            Role = NetRole.Host;
            NetworkEvents.RaiseHostStarted();
        }

        /// <summary>A punching connect keeps trying for two minutes and looks up this end's public address for the host.</summary>
        internal void Connect(string address, int port, string? key, bool punch)
        {
            Stop();
            // Reset on start rather than in Stop, so an ended session's numbers stay readable.
            NetworkDiagnostics.Reset();
            _mode = TransportMode.Direct;
            _connectKey = key;
            _manager = NewManager();
            if (punch)
            {
                _manager.MaxConnectAttempts = PunchConnectAttempts;
                _nat!.FindPublicEndPoint();
            }

            _manager.Start();
            _manager.Connect(address, port, key ?? string.Empty);
            Role = NetRole.Client;
        }

        internal void FindPublicEndPoint()
        {
            if (_manager == null)
            {
                NetworkEvents.RaiseNetworkError("finding the public address needs a running direct session");
                return;
            }

            _nat!.FindPublicEndPoint();
        }

        internal void Punch(string address, int port)
        {
            if (_manager == null)
            {
                NetworkEvents.RaiseNetworkError("punching needs a running direct session");
                return;
            }

            if (!IPAddress.TryParse(address, out IPAddress? ip))
            {
                NetworkEvents.RaiseNetworkError($"cannot punch to '{address}', it is not an IP address");
                return;
            }

            _nat!.Punch(new IPEndPoint(ip, port));
        }

        /// <summary>
        /// Hosts over Steam. <see cref="NetworkEvents.OnHostStarted"/> fires at once. A network error fires instead when Steam
        /// is not running.
        /// </summary>
        internal void StartHostViaSteam()
        {
            Stop();
            NetworkDiagnostics.Reset();
            _mode = TransportMode.Steam;

            if (!_steam.StartHost())
            {
                NetworkEvents.RaiseNetworkError("Steam is not running, or this copy of the game is not the Steam one");
                return;
            }

            Role = NetRole.Host;
            NetworkEvents.RaiseHostStarted();
        }

        /// <summary>Joins a Steam host by its Steam id. <see cref="NetworkEvents.OnConnectedToHost"/> fires on its welcome.</summary>
        internal void ConnectViaSteam(ulong hostId)
        {
            Stop();
            NetworkDiagnostics.Reset();
            _mode = TransportMode.Steam;

            if (!_steam.Connect(hostId))
            {
                NetworkEvents.RaiseNetworkError("Steam is not running, or this copy of the game is not the Steam one");
                return;
            }

            Role = NetRole.Client;
        }

        internal void Poll()
        {
            if (_mode == TransportMode.Steam) _steam.Poll();
            if (_manager != null) _nat?.Poll(_manager);
            _portMapping?.Poll();
            _manager?.PollEvents();
            DeliverDelayed();
            FlushRefusedLog();
            FlushWithoutSessionLog();
        }

        internal void Stop()
        {
            if (_manager != null)
            {
                _manager.Stop();
                _manager = null;
            }

            if (_portMapping != null)
            {
                PortClosed = _portMapping.Close();
                _portMapping = null;
            }

            _nat = null;
            _steam.Stop();
            _mode = TransportMode.Direct;
            Role = NetRole.None;
        }

        /// <summary>
        /// Appends the current peers to the buffer. The caller owns the list, so repeated calls with the
        /// same list do not allocate.
        /// </summary>
        internal void CopyPeers(List<INetPeer> buffer)
        {
            if (_mode == TransportMode.Steam)
            {
                _steam.CopyPeers(buffer);
                return;
            }

            if (_manager == null) return;

            foreach (NetPeer peer in _manager)
            {
                buffer.Add(PeerWrapper.Of(peer));
            }
        }

        internal void SendTyped<T>(INetPeer peer, in T message, Delivery delivery) where T : struct, INetMessage
        {
            if (_mode == TransportMode.Steam)
            {
                if (!TrySteamPeer(peer, out SteamPeer? steamPeer)) return;

                NetDataWriter typed = Dispatcher.BuildTyped(message);
                _steam.Send(steamPeer!, TypedChannel, new ReadOnlySpan<byte>(typed.Data, 0, typed.Length), delivery);
                return;
            }

            if (!TryDirectPeer(peer, out PeerWrapper? wrapper)) return;

            NetDataWriter writer = Dispatcher.BuildTyped(message);
            wrapper!.Native.Send(writer, TypedChannel, Map(delivery));
        }

        internal void SendTypedToAll<T>(in T message, Delivery delivery) where T : struct, INetMessage
        {
            if (_mode == TransportMode.Steam)
            {
                NetDataWriter typed = Dispatcher.BuildTyped(message);
                _steam.SendToAll(TypedChannel, new ReadOnlySpan<byte>(typed.Data, 0, typed.Length), delivery);
                return;
            }

            if (_manager == null)
            {
                NetworkDiagnostics.CountSendWithoutSession();
                return;
            }
            NetDataWriter writer = Dispatcher.BuildTyped(message);
            _manager.SendToAll(writer, TypedChannel, Map(delivery));
        }

        internal void SendRaw(INetPeer peer, byte channel, ReadOnlySpan<byte> data, Delivery delivery)
        {
            if (_mode == TransportMode.Steam)
            {
                if (TrySteamPeer(peer, out SteamPeer? steamPeer)) _steam.Send(steamPeer!, channel, data, delivery);
                return;
            }

            if (!TryDirectPeer(peer, out PeerWrapper? wrapper)) return;

            wrapper!.Native.Send(data, channel, Map(delivery));
        }

        internal void SendRawToAll(byte channel, ReadOnlySpan<byte> data, Delivery delivery)
        {
            if (_mode == TransportMode.Steam)
            {
                _steam.SendToAll(channel, data, delivery);
                return;
            }

            if (_manager == null)
            {
                NetworkDiagnostics.CountSendWithoutSession();
                return;
            }
            DeliveryMethod method = Map(delivery);
            foreach (NetPeer peer in _manager)
            {
                peer.Send(data, channel, method);
            }
        }

        private static bool TrySteamPeer(INetPeer peer, [NotNullWhen(true)] out SteamPeer? steamPeer)
        {
            steamPeer = peer as SteamPeer;
            if (steamPeer != null) return true;

            MelonLogger.Error("[Networking] a peer from another kind of session was used in a Steam session");
            return false;
        }

        private bool TryDirectPeer(INetPeer peer, [NotNullWhen(true)] out PeerWrapper? wrapper)
        {
            wrapper = peer as PeerWrapper;
            if (wrapper == null)
            {
                MelonLogger.Error("[Networking] a peer from a Steam session was used in a direct session");
                return false;
            }

            // A wrapper outlives the session it came from, so a mod holding one from a session that has
            // ended would otherwise send into a manager that is gone.
            if (_manager == null)
            {
                NetworkDiagnostics.CountSendWithoutSession();
                return false;
            }

            return true;
        }

        private NetManager NewManager()
        {
            _nat = new NatLayer();
            return new NetManager(_listener, _nat)
            {
                UnsyncedEvents = false,
                AutoRecycle = false,
                ChannelsCount = ChannelCount,

                // Always on: the build running when coop misbehaves is the one whose numbers are needed.
                EnableStatistics = true,

                // The 15 ms default flush delay is paid twice per round trip, and per-frame state gains
                // nothing from waiting.
                UpdateTime = 1,

                // A story teleport into a big area stalls the process for well over the 5 s default, and both
                // ends would time each other out.
                DisconnectTimeout = 30_000,

                // LiteNetLib allocates the whole fragment table when a fragmented message starts, sized by what the peer
                // claims, and keeps up to 4096 open. At the default 65535 a few dozen tiny packets claim 2 GB.
                // 512 fragments of at least 1 KB carry 512 KB, the most a Steam session can send in one message too.
                MaxFragmentsCount = MaxFragments,
            };
        }

        private void OnConnectionRequest(ConnectionRequest request)
        {
            // A client's only peer is its host. Accepting here would let anyone who finds its port pose as a second host.
            if (Role != NetRole.Host)
            {
                request.Reject();
                return;
            }

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
            PeerWrapper wrapper = PeerWrapper.Of(peer);
            // Before the generic event, so a subscriber can special-case the host.
            if (Role == NetRole.Client)
            {
                NetworkEvents.RaiseConnectedToHost();
            }
            NetworkEvents.RaisePeerConnected(wrapper);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            PeerWrapper wrapper = PeerWrapper.Of(peer);
            NetworkEvents.RaisePeerDisconnected(wrapper, Map(disconnectInfo.Reason));
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            if (_netDelay is not { } delay)
            {
                Deliver(peer, reader, channel, deliveryMethod);
                return;
            }

            long due = _clock.ElapsedMilliseconds + _jitter.Next(delay.Min, delay.Max + 1);
            // Kept in order wherever LiteNetLib keeps it: this delay sits behind the transport, and a sequenced
            // packet reordered here would be one the transport had already dropped as stale.
            if (deliveryMethod != DeliveryMethod.Unreliable && deliveryMethod != DeliveryMethod.ReliableUnordered)
            {
                due = Math.Max(due, _lastOrderedDue);
                _lastOrderedDue = due;
            }

            _delayed.Add((due, peer, reader, channel, deliveryMethod));
        }

        private void DeliverDelayed()
        {
            if (_delayed.Count == 0) return;

            long now = _clock.ElapsedMilliseconds;
            // Stable by due time, so ordered packets with equal times keep their arrival order.
            List<(long Due, NetPeer Peer, NetPacketReader Reader, byte Channel, DeliveryMethod Method)> due =
                _delayed.Where(entry => entry.Due <= now).OrderBy(entry => entry.Due).ToList();
            if (due.Count == 0) return;

            _delayed.RemoveAll(entry => entry.Due <= now);
            foreach ((long _, NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method) in due)
            {
                Deliver(peer, reader, channel, method);
            }
        }

        private void Deliver(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
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
                NetworkDiagnostics.CountReceiveFailure();
                MelonLogger.Error("[Networking] receive on channel " + channel + " failed: " + e);
            }
            finally
            {
                // AutoRecycle is off, so return the reader to LiteNetLib's pool by hand.
                reader.Recycle();
            }
        }

        /// <summary>
        /// Summarizes messages the dispatcher refused for carrying a NaN, an infinity, or a length prefix
        /// the packet could not back. Otherwise a mod's messages would stop arriving with no explanation.
        /// </summary>
        private void FlushRefusedLog()
        {
            long total = NetworkDiagnostics.RefusedMessages;

            // A new session or the stats view's reset moves the counter backwards, so it is re-baselined.
            if (total <= _loggedRefused)
            {
                _loggedRefused = total;
                return;
            }

            long now = Environment.TickCount64;
            if (now - _lastRefusedLogMs < DropLogIntervalMs) return;

            long refused = total - _loggedRefused;
            _loggedRefused = total;
            _lastRefusedLogMs = now;

            MelonLogger.Warning("[Networking] dropped " + refused + " messages carrying a NaN, an infinity, " +
                "or a length no packet could hold; a peer is sending values no game can use, whether through " +
                "a bug or on purpose.");
        }

        /// <summary>
        /// Summarizes sends made while no session was running. They do nothing, and a mod that starts
        /// sending a frame too early has no other way to find out.
        /// </summary>
        private void FlushWithoutSessionLog()
        {
            long total = NetworkDiagnostics.SendsWithoutSession;
            if (total <= _loggedWithoutSession)
            {
                _loggedWithoutSession = total;
                return;
            }

            long now = Environment.TickCount64;
            if (now - _lastWithoutSessionLogMs < DropLogIntervalMs) return;

            long dropped = total - _loggedWithoutSession;
            _loggedWithoutSession = total;
            _lastWithoutSessionLogMs = now;

            MelonLogger.Warning("[Networking] " + dropped + " sends did nothing because no session was " +
                "running; wait for OnHostStarted or OnConnectedToHost before sending.");
        }

        private void OnReceiveUnconnected(IPEndPoint from, NetPacketReader reader, UnconnectedMessageType type)
        {
            try
            {
                if (_mode == TransportMode.Direct && Role == NetRole.Host && _manager != null)
                {
                    LanDiscovery.TryAnswer(_manager, from, reader, type);
                }
            }
            finally
            {
                reader.Recycle();
            }
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
                _ => Unmapped(reason),
            };
        }

        // Logged with LiteNetLib's own name, since Unknown alone explains nothing.
        private static DisconnectReason Unmapped(LiteNetLib.DisconnectReason reason)
        {
            MelonLogger.Warning($"[Networking] a peer left for a reason this API does not name: {reason}.");
            return DisconnectReason.Unknown;
        }
    }
}
