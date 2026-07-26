using LiteNetLib;
using LiteNetLib.Utils;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Relay-session state and packet framing, kept apart from <see cref="LiteTransport"/> so the direct
    /// path stays readable. Holds the single connection to the relay, the virtual peers behind it, the
    /// session's <see cref="SessionCrypto"/> when it has one, and the buffers used to frame packets.
    /// </summary>
    internal sealed class RelayLink
    {
        private readonly RelayPeer?[] _peers = new RelayPeer?[256];
        private readonly NetDataWriter _controlWriter = new();
        private readonly NetDataReader _plainReader = new();
        private byte[] _frameBuffer = new byte[1500];
        private byte[] _plainBuffer = new byte[1500];
        private SessionCrypto? _crypto;
        private string? _password;

        /// <summary>
        /// The one transport connection in relay mode, or null while there is no session.
        /// </summary>
        internal NetPeer? Connection { get; private set; }

        /// <summary>
        /// The session code in use. Null before a session, and empty while a host is still waiting for the
        /// relay to pick one for it.
        /// </summary>
        internal string? SessionCode { get; private set; }

        /// <summary>
        /// The virtual id the relay assigned us, valid once <see cref="Welcomed"/> is true.
        /// </summary>
        internal byte LocalVirtualId { get; private set; }

        /// <summary>
        /// Whether the relay has accepted us into a session. Until then the connection exists but the
        /// session does not, so nothing may be sent.
        /// </summary>
        internal bool Welcomed { get; private set; }

        /// <summary>
        /// Whether payloads are sealed end to end. True only for a password-protected session: an open
        /// session has no key material, and pretending otherwise would be worse than saying so.
        /// </summary>
        internal bool IsEncrypted => _crypto != null;

        /// <summary>
        /// How many remote peers are known. On a client this is at most one, the host.
        /// </summary>
        internal int PeerCount { get; private set; }

        /// <summary>
        /// The buffer the last <see cref="Frame"/> filled. Only valid until the next framing call.
        /// </summary>
        internal byte[] FrameBuffer => _frameBuffer;

        /// <summary>
        /// Remembers what the session was started with. The key cannot be derived yet: a host that let the
        /// relay pick its code does not know the code at this point.
        /// </summary>
        internal void Prepare(string? sessionCode, string? password)
        {
            SessionCode = sessionCode ?? string.Empty;
            _password = password;
        }

        /// <summary>
        /// Remembers the relay connection. The session itself only starts at the welcome.
        /// </summary>
        internal void Attach(NetPeer connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Takes the code the relay picked for a host that asked for one.
        /// </summary>
        internal void AssignSessionCode(string sessionCode)
        {
            SessionCode = sessionCode;
        }

        /// <summary>
        /// Records the relay's welcome and derives the session key when there is a password to derive it
        /// from. This is the first moment both ends are guaranteed to know the code, which is half the key
        /// material, so it is where encryption can start.
        /// </summary>
        internal void Welcome(byte localVirtualId)
        {
            LocalVirtualId = localVirtualId;
            Welcomed = true;

            if (string.IsNullOrEmpty(_password) || string.IsNullOrEmpty(SessionCode))
            {
                return;
            }

            _crypto = SessionCrypto.Create(SessionCode, _password);
            // Nothing needs the password once the key exists, so it does not stay reachable.
            _password = null;
        }

        /// <summary>
        /// Forgets everything about the session. Callers raise the disconnect events first, because after
        /// this the peers are no longer reachable.
        /// </summary>
        internal void Reset()
        {
            Array.Clear(_peers, 0, _peers.Length);
            Connection = null;
            SessionCode = null;
            LocalVirtualId = 0;
            Welcomed = false;
            PeerCount = 0;
            _crypto = null;
            _password = null;
        }

        /// <summary>
        /// The wrapper for a virtual id, creating it if this is the first mention.
        /// </summary>
        /// <param name="transport">Owner passed to a newly created wrapper.</param>
        /// <param name="virtualId">The relay-assigned id.</param>
        /// <param name="created">True when the peer was not known before, so the caller announces it.</param>
        internal RelayPeer EnsurePeer(LiteTransport transport, byte virtualId, out bool created)
        {
            RelayPeer? existing = _peers[virtualId];
            if (existing != null)
            {
                created = false;
                return existing;
            }

            RelayPeer peer = new(transport, virtualId);
            _peers[virtualId] = peer;
            PeerCount++;
            created = true;
            return peer;
        }

        /// <summary>
        /// The wrapper for a virtual id, or null when nobody holds it.
        /// </summary>
        internal RelayPeer? Find(byte virtualId)
        {
            return _peers[virtualId];
        }

        /// <summary>
        /// Drops a peer that left, returning the wrapper so the caller can announce it.
        /// </summary>
        internal RelayPeer? Remove(byte virtualId)
        {
            RelayPeer? peer = _peers[virtualId];
            if (peer == null) return null;

            _peers[virtualId] = null;
            PeerCount--;
            return peer;
        }

        /// <summary>
        /// Appends every known peer to the buffer, without clearing it.
        /// </summary>
        internal void CopyPeers(List<INetPeer> buffer)
        {
            int remaining = PeerCount;

            for (int virtualId = 0; virtualId < _peers.Length && remaining > 0; virtualId++)
            {
                RelayPeer? peer = _peers[virtualId];
                if (peer == null) continue;

                remaining--;
                buffer.Add(peer);
            }
        }

        /// <summary>
        /// Calls back for every known peer. Used to tear a session down without allocating an enumerator
        /// while the table is being cleared.
        /// </summary>
        internal void ForEachPeer(Action<RelayPeer> action)
        {
            int remaining = PeerCount;

            for (int virtualId = 0; virtualId < _peers.Length && remaining > 0; virtualId++)
            {
                RelayPeer? peer = _peers[virtualId];
                if (peer == null) continue;

                remaining--;
                action(peer);
            }
        }

        /// <summary>
        /// The target id that means "everyone I can talk to": all clients for a host, the host for a
        /// client.
        /// </summary>
        internal static byte TargetForAll(NetRole role)
        {
            return role == NetRole.Host ? RelayProtocol.BroadcastVirtualId : RelayProtocol.HostVirtualId;
        }

        /// <summary>
        /// Frames a payload for the relay: the target byte, which the relay must be able to read, followed
        /// by the payload either sealed or in the clear. Reports the framed length in
        /// <see cref="FrameBuffer"/>. The buffer grows to the largest payload seen and is then reused, so
        /// steady-state sending does not allocate.
        /// </summary>
        internal int Frame(byte target, byte channel, ReadOnlySpan<byte> payload)
        {
            int overhead = _crypto != null ? SessionCrypto.Overhead : 0;
            int length = payload.Length + RelayProtocol.HeaderSize + overhead;
            if (_frameBuffer.Length < length)
            {
                _frameBuffer = new byte[length];
            }

            _frameBuffer[0] = target;

            if (_crypto == null)
            {
                payload.CopyTo(_frameBuffer.AsSpan(RelayProtocol.HeaderSize));
                return payload.Length + RelayProtocol.HeaderSize;
            }

            return RelayProtocol.HeaderSize + _crypto.Seal(LocalVirtualId, channel, payload, _frameBuffer, RelayProtocol.HeaderSize);
        }

        /// <summary>
        /// Unseals a received payload into the link's own buffer.
        /// </summary>
        /// <param name="senderVirtualId">The id the relay stamped, authenticated as part of the packet.</param>
        /// <param name="channel">The channel it arrived on.</param>
        /// <param name="packet">The bytes after the sender id.</param>
        /// <param name="length">How many plaintext bytes are in <see cref="PlainBuffer"/>.</param>
        /// <returns>False when the packet was not sealed by a peer holding our key.</returns>
        internal bool TryOpen(byte senderVirtualId, byte channel, ReadOnlySpan<byte> packet, out int length)
        {
            length = 0;
            if (_crypto == null) return false;

            int needed = packet.Length;
            if (_plainBuffer.Length < needed)
            {
                _plainBuffer = new byte[needed];
            }

            return _crypto.TryOpen(senderVirtualId, channel, packet, _plainBuffer, out length);
        }

        /// <summary>
        /// The buffer <see cref="TryOpen"/> decrypted into.
        /// </summary>
        internal byte[] PlainBuffer => _plainBuffer;

        /// <summary>
        /// A reader over the decrypted bytes, for handing typed messages to <see cref="Dispatcher"/>. The
        /// instance is reused, like every other reader on this path.
        /// </summary>
        internal NetDataReader PlainReader(int length)
        {
            _plainReader.SetSource(_plainBuffer, 0, length);
            return _plainReader;
        }

        /// <summary>
        /// Builds a control request addressed to the relay itself.
        /// </summary>
        internal NetDataWriter BuildControl(byte opcode, byte argument)
        {
            _controlWriter.Reset();
            _controlWriter.Put(RelayProtocol.RelayVirtualId);
            _controlWriter.Put(opcode);
            _controlWriter.Put(argument);
            return _controlWriter;
        }
    }
}
