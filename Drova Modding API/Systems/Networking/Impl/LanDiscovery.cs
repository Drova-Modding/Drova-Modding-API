using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Finds direct hosts on the local network by UDP broadcast. The searching side runs its own manager,
    /// so it works outside a session. The answering side is the direct host's manager, see <see cref="TryAnswer"/>.
    /// </summary>
    internal static class LanDiscovery
    {
        private const long RebroadcastMs = 2_000;

        // Three missed broadcasts.
        private const long ForgetAfterMs = 6_500;

        // Bounds what a noisy network can make this machine hold.
        private const int MaxHosts = 64;

        private static readonly byte[] Request = "DRV?"u8.ToArray();
        private static readonly byte[] AnswerTag = "DRV!"u8.ToArray();
        private static readonly EventBasedNetListener Listener = new();
        private static readonly Dictionary<IPEndPoint, (DiscoveredHost Host, long SeenMs)> Found = new();
        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

        private static NetManager? _manager;
        private static int _port;
        private static long _lastBroadcastMs;

        static LanDiscovery()
        {
            Listener.NetworkReceiveUnconnectedEvent += OnAnswer;
        }

        /// <summary>What a direct host answers with. Null means it does not answer.</summary>
        internal static Func<byte[]>? Provider { get; set; }

        internal static bool IsRunning => _manager != null;

        internal static void Start(int port)
        {
            Stop();

            // IPv4 only: a broadcast is IPv4, and one host answering over both families would be listed twice.
            _manager = new NetManager(Listener) { UnconnectedMessagesEnabled = true, IPv6Enabled = false, AutoRecycle = true };
            if (!_manager.Start())
            {
                MelonLogger.Error("[Networking] LAN discovery could not open a socket.");
                _manager = null;
                return;
            }

            _port = port;
            Broadcast();
        }

        internal static void Stop()
        {
            _manager?.Stop();
            _manager = null;
            Found.Clear();
        }

        internal static void Poll()
        {
            if (_manager == null) return;

            _manager.PollEvents();

            long now = Clock.ElapsedMilliseconds;
            if (now - _lastBroadcastMs >= RebroadcastMs) Broadcast();

            foreach (KeyValuePair<IPEndPoint, (DiscoveredHost Host, long SeenMs)> entry in Found)
            {
                if (now - entry.Value.SeenMs > ForgetAfterMs) Found.Remove(entry.Key);
            }
        }

        internal static void CopyHosts(List<DiscoveredHost> buffer)
        {
            foreach ((DiscoveredHost host, long _) in Found.Values) buffer.Add(host);
        }

        /// <summary>
        /// Answers a discovery broadcast that reached a direct host's manager. Only senders on a private network
        /// get an answer, so a host reachable from the internet does not describe itself to it.
        /// </summary>
        internal static void TryAnswer(NetManager manager, IPEndPoint from, NetPacketReader reader, UnconnectedMessageType type)
        {
            Func<byte[]>? provider = Provider;
            if (provider == null || type != UnconnectedMessageType.Broadcast) return;
            if (!reader.GetRemainingBytesSpan().SequenceEqual(Request) || !IsPrivate(from.Address)) return;

            byte[] payload;
            try
            {
                payload = provider();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Networking] the discovery info provider failed: " + e);
                return;
            }

            manager.SendUnconnectedMessage([.. AnswerTag, .. payload], from);
        }

        private static void Broadcast()
        {
            _lastBroadcastMs = Clock.ElapsedMilliseconds;
            _manager?.SendBroadcast(Request, _port);
        }

        private static void OnAnswer(IPEndPoint from, NetPacketReader reader, UnconnectedMessageType type)
        {
            ReadOnlySpan<byte> data = reader.GetRemainingBytesSpan();
            if (type != UnconnectedMessageType.BasicMessage || !data.StartsWith(AnswerTag)) return;
            if (!Found.ContainsKey(from) && Found.Count >= MaxHosts) return;

            DiscoveredHost host = new(from.Address.ToString(), from.Port, data[AnswerTag.Length..].ToArray(), DateTime.UtcNow);
            Found[from] = (host, Clock.ElapsedMilliseconds);
        }

        private static bool IsPrivate(IPAddress address)
        {
            if (IPAddress.IsLoopback(address)) return true;
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            if (address.AddressFamily == AddressFamily.InterNetworkV6) return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;

            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254);
        }
    }
}
