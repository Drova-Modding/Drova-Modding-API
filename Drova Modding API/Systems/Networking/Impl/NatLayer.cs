using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Drova_Modding_API.Access;
using LiteNetLib;
using LiteNetLib.Layers;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Hole punching on a direct session's own socket, so the address STUN reports is the one the session uses.
    /// A STUN request leaves as an unconnected message that this layer strips to the bare request, and the answer is
    /// taken out before LiteNetLib sees it.
    /// </summary>
    internal sealed class NatLayer : PacketLayerBase
    {
        private const int StunHeaderSize = 20;
        private const ushort BindingRequest = 0x0001;
        private const ushort BindingSuccess = 0x0101;
        private const uint MagicCookie = 0x2112A442;
        private const ushort XorMappedAddress = 0x0020;
        private const byte FamilyIPv4 = 0x01;
        private const long RetryMs = 2_000;

        /// <summary>Routers drop an idle UDP mapping after 30 s at the earliest.</summary>
        private const long KeepAliveMs = 20_000;

        private const long PunchIntervalMs = 500;
        private const long PunchDurationMs = 120_000;

        private static readonly (string Host, int Port)[] StunServers = { ("stun.l.google.com", 19302), ("stun.cloudflare.com", 3478) };

        private static Task<IPEndPoint[]>? _resolvedServers;

        private readonly byte[] _request = new byte[StunHeaderSize];
        private readonly List<(IPEndPoint Target, long UntilMs)> _punches = new();
        private volatile IPEndPoint? _answer;
        private IPEndPoint? _answeredBy;
        private volatile bool _symmetric;
        private bool _warnedSymmetric;
        private bool _finding;
        private long _nextStunMs;
        private long _nextPunchMs;

        internal NatLayer() : base(0)
        {
            BinaryPrimitives.WriteUInt16BigEndian(_request, BindingRequest);
            BinaryPrimitives.WriteUInt32BigEndian(_request.AsSpan(4), MagicCookie);
            Random.Shared.NextBytes(_request.AsSpan(8));
        }

        /// <summary>The socket's address on the internet, once a STUN server answered.</summary>
        internal IPEndPoint? PublicEndPoint { get; private set; }

        /// <summary>Asks the STUN servers until one answers, then every 20 s so the router keeps the mapping.</summary>
        internal void FindPublicEndPoint()
        {
            _finding = true;
        }

        /// <summary>Sends to <paramref name="target"/> for two minutes, so its packets get through this end's router.</summary>
        internal void Punch(IPEndPoint target)
        {
            long now = Environment.TickCount64;
            _punches.RemoveAll(punch => punch.Target.Equals(target));
            _punches.Add((target, now + PunchDurationMs));
            _nextPunchMs = now;
        }

        internal void Poll(NetManager manager)
        {
            long now = Environment.TickCount64;

            IPEndPoint? answer = _answer;
            if (answer != null && !answer.Equals(PublicEndPoint))
            {
                PublicEndPoint = answer;
                MelonLogger.Msg($"[Networking] the internet sees this session at {answer}.");
                NetworkEvents.RaisePublicEndPointFound(answer);
            }

            if (_symmetric && !_warnedSymmetric)
            {
                _warnedSymmetric = true;
                MelonLogger.Warning("[Networking] this router gives every destination a new port, so punching cannot work here. " +
                    "Use a Steam session or forward the port.");
            }

            if (_finding && now >= _nextStunMs) AskStun(manager, now);

            if (_punches.Count > 0 && now >= _nextPunchMs)
            {
                _nextPunchMs = now + PunchIntervalMs;
                _punches.RemoveAll(punch => punch.UntilMs <= now);

                foreach ((IPEndPoint target, long _) in _punches)
                {
                    manager.SendUnconnectedMessage(ReadOnlySpan<byte>.Empty, target);
                }
            }
        }

        public override void ProcessInboundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int length)
        {
            if (length < StunHeaderSize || BinaryPrimitives.ReadUInt16BigEndian(data) != BindingSuccess) return;
            if (!data.AsSpan(4, StunHeaderSize - 4).SequenceEqual(_request.AsSpan(4))) return;

            // The transaction id travels in the clear, so someone on the path could answer in the server's place.
            Task<IPEndPoint[]>? servers = _resolvedServers;
            if (servers == null || !servers.IsCompletedSuccessfully || Array.IndexOf(servers.Result, endPoint) < 0) return;

            int attributesLength = Math.Min(BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(2)), length - StunHeaderSize);
            IPEndPoint? mapped = ReadXorMappedAddress(data.AsSpan(StunHeaderSize, attributesLength));
            length = 0;
            if (mapped == null) return;

            // Two servers seeing two addresses at once means the router picks a new port per destination.
            IPEndPoint? previous = _answer;
            if (previous != null && !previous.Equals(mapped) && !endPoint.Equals(_answeredBy)) _symmetric = true;

            _answeredBy = endPoint;
            _answer = mapped;
        }

        public override void ProcessOutBoundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int offset, ref int length)
        {
            int header = length - StunHeaderSize;
            if (header < 0 || !data.AsSpan(offset + header, StunHeaderSize).SequenceEqual(_request)) return;

            Buffer.BlockCopy(data, offset + header, data, offset, StunHeaderSize);
            length = StunHeaderSize;
        }

        private void AskStun(NetManager manager, long now)
        {
            _resolvedServers ??= Task.Run(ResolveStunServers);
            if (!_resolvedServers.IsCompleted) return;

            _nextStunMs = now + (PublicEndPoint == null ? RetryMs : KeepAliveMs);

            IPEndPoint[] servers = _resolvedServers.Result;
            if (servers.Length == 0)
            {
                _resolvedServers = null;
                return;
            }

            foreach (IPEndPoint server in servers)
            {
                manager.SendUnconnectedMessage(_request, server);
            }
        }

        private static IPEndPoint[] ResolveStunServers()
        {
            List<IPEndPoint> servers = new();

            foreach ((string host, int port) in StunServers)
            {
                try
                {
                    IPAddress? address = Dns.GetHostAddresses(host).FirstOrDefault(candidate => candidate.AddressFamily == AddressFamily.InterNetwork);
                    if (address != null) servers.Add(new IPEndPoint(address, port));
                }
                catch (SocketException e)
                {
                    MelonLogger.Warning($"[Networking] could not look up the STUN server {host}: {e.Message}");
                }
            }

            return servers.ToArray();
        }

        private static IPEndPoint? ReadXorMappedAddress(ReadOnlySpan<byte> attributes)
        {
            while (attributes.Length >= 4)
            {
                ushort type = BinaryPrimitives.ReadUInt16BigEndian(attributes);
                int size = BinaryPrimitives.ReadUInt16BigEndian(attributes.Slice(2));
                if (attributes.Length < 4 + size) return null;

                ReadOnlySpan<byte> value = attributes.Slice(4, size);
                if (type == XorMappedAddress && size >= 8 && value[1] == FamilyIPv4)
                {
                    int port = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(2)) ^ (int)(MagicCookie >> 16);

                    Span<byte> address = stackalloc byte[4];
                    BinaryPrimitives.WriteUInt32BigEndian(address, BinaryPrimitives.ReadUInt32BigEndian(value.Slice(4)) ^ MagicCookie);
                    return new IPEndPoint(new IPAddress(address), port);
                }

                // Attribute values are padded to four bytes.
                attributes = attributes.Slice(Math.Min(attributes.Length, 4 + ((size + 3) & ~3)));
            }

            return null;
        }
    }
}
