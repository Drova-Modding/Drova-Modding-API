using System.Security.Cryptography;
using System.Text;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// End-to-end encryption for a relay session: AES-GCM between host and clients, keyed from the
    /// session password, so a relay only ever sees ciphertext. This closes both halves of the relay's
    /// trust problem - it cannot read the traffic, and because the sender id and channel are
    /// authenticated as associated data it cannot forge a packet as another peer either, which the host's
    /// authority over gameplay could never have detected on its own.
    ///
    /// The reading half only actually closed in protocol version 2. Before it, the password was both the
    /// key material and the credential the handshake showed the relay in the clear, so the relay held
    /// everything it needed to derive the key and the encryption stopped everyone except the one party
    /// it existed to stop. <see cref="DeriveJoinToken"/> is what separates the two.
    ///
    /// What it does not do is stop a hostile relay replaying a genuine packet it forwarded earlier.
    /// Rejecting a repeated counter would break unreliable channels, whose whole point is that packets
    /// arrive late, out of order or not at all, so that bound is left to the game's own state handling.
    ///
    /// One instance per session, used only from the poll thread, which is what lets the buffers and the
    /// counter be plain fields.
    /// </summary>
    internal sealed class SessionCrypto
    {
        /// <summary>
        /// Bytes every encrypted packet costs on top of its payload: a 12-byte nonce and a 16-byte tag.
        /// A game side sizing messages against the relay's payload cap has to subtract this.
        /// </summary>
        internal const int Overhead = NonceSize + TagSize;

        /// <summary>
        /// Size of the value handed to the relay in place of the password. Mirrors the relay's
        /// <c>RelayProtocol.JoinTokenBytes</c>.
        /// </summary>
        internal const int JoinTokenBytes = 32;

        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int KeySize = 32;
        private const int NoncePrefixSize = 4;
        private const int Iterations = 100_000;

        // Two labels, one password. The relay is given only the value derived under the first, and the
        // key is derived under the second, so holding a join token is no closer to holding the key than
        // holding nothing is: both sides of the split cost a full PBKDF2 run per password guess.
        //
        // The admit label deliberately leaves the session code out. A host that asks the relay to pick
        // its code has to present a token before it knows what that code will be, so anything the token
        // depends on has to be knowable at handshake time. The key keeps the code, which is what stops
        // one precomputed table serving every session.
        private const string AdmitSaltLabel = "drova-coop-relay-v2:admit";
        private const string KeySaltLabel = "drova-coop-relay-v2:key:";

        private readonly AesGcm _cipher;
        private readonly byte[] _noncePrefix = new byte[NoncePrefixSize];
        private readonly byte[] _nonce = new byte[NonceSize];
        private readonly byte[] _associatedData = new byte[2];
        private ulong _counter;

        private SessionCrypto(byte[] key)
        {
            _cipher = new AesGcm(key);

            // The counter alone is not enough: virtual ids are reused when a client leaves and another
            // joins, so a fresh peer could otherwise repeat a nonce that was already used under the same
            // key - the one mistake AES-GCM does not survive. A per-connection random prefix makes the
            // nonce unique across rejoins as well as across packets.
            RandomNumberGenerator.Fill(_noncePrefix);
        }

        /// <summary>
        /// Derives the session key from the password and the session code and builds the cipher. Both
        /// ends run this with the same two inputs and never exchange key material, so there is nothing
        /// for a relay to sit in the middle of; the strength of the session is the strength of the
        /// password.
        ///
        /// Costs about a tenth of a second on purpose - it runs once when a session starts, and it is
        /// what makes a short password expensive to grind.
        /// </summary>
        /// <param name="sessionCode">The code in use, upper-cased so the relay's case-insensitive matching cannot split the two ends.</param>
        /// <param name="password">The session password. Must not be empty; an open session has no key material and stays unencrypted.</param>
        internal static SessionCrypto Create(string sessionCode, string password)
        {
            byte[] salt = SHA256.HashData(Encoding.UTF8.GetBytes(KeySaltLabel + sessionCode.ToUpperInvariant()));
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return new SessionCrypto(key);
        }

        /// <summary>
        /// Derives what the handshake presents to the relay instead of the password.
        ///
        /// This is the whole point of the split: the relay has to be able to tell whether a joiner knows
        /// the session secret, and version 1 let it do that by simply handing it the password — which
        /// also handed it both inputs to the key, so the encryption protected the traffic from everyone
        /// except the party actually carrying it. The relay now gets a value it cannot walk back.
        ///
        /// Costs a second tenth of a second on top of <see cref="Create"/>, once per session. Both run
        /// on the calling thread, so a session start is around a fifth of a second of work.
        /// </summary>
        /// <param name="password">The session password. Must not be empty.</param>
        internal static byte[] DeriveJoinToken(string password)
        {
            byte[] salt = SHA256.HashData(Encoding.UTF8.GetBytes(AdmitSaltLabel));
            return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, JoinTokenBytes);
        }

        /// <summary>
        /// Encrypts a payload into <paramref name="destination"/> as nonce, ciphertext and tag.
        /// </summary>
        /// <param name="senderVirtualId">Our own virtual id, authenticated so it cannot be rewritten.</param>
        /// <param name="channel">The channel, authenticated for the same reason.</param>
        /// <param name="plaintext">The payload to protect.</param>
        /// <param name="destination">Buffer with room for <paramref name="plaintext"/> plus <see cref="Overhead"/>.</param>
        /// <param name="offset">Where in the buffer to start writing.</param>
        /// <returns>How many bytes were written.</returns>
        internal int Seal(byte senderVirtualId, byte channel, ReadOnlySpan<byte> plaintext, byte[] destination, int offset)
        {
            NextNonce();
            BuildAssociatedData(senderVirtualId, channel);

            Span<byte> target = destination.AsSpan(offset);
            _nonce.CopyTo(target);
            Span<byte> ciphertext = target.Slice(NonceSize, plaintext.Length);
            Span<byte> tag = target.Slice(NonceSize + plaintext.Length, TagSize);

            _cipher.Encrypt(_nonce, plaintext, ciphertext, tag, _associatedData);
            return NonceSize + plaintext.Length + TagSize;
        }

        /// <summary>
        /// Reverses <see cref="Seal"/>.
        /// </summary>
        /// <param name="senderVirtualId">The id the relay stamped on the packet. A forged one fails the tag check.</param>
        /// <param name="channel">The channel it arrived on.</param>
        /// <param name="packet">Nonce, ciphertext and tag as received.</param>
        /// <param name="destination">Buffer with room for the plaintext.</param>
        /// <param name="length">How many plaintext bytes were written.</param>
        /// <returns>False when the packet was truncated, tampered with, or sealed under another key.</returns>
        internal bool TryOpen(byte senderVirtualId, byte channel, ReadOnlySpan<byte> packet, byte[] destination, out int length)
        {
            length = 0;
            if (packet.Length < Overhead) return false;

            int plaintextLength = packet.Length - Overhead;
            if (plaintextLength > destination.Length) return false;

            BuildAssociatedData(senderVirtualId, channel);

            ReadOnlySpan<byte> nonce = packet.Slice(0, NonceSize);
            ReadOnlySpan<byte> ciphertext = packet.Slice(NonceSize, plaintextLength);
            ReadOnlySpan<byte> tag = packet.Slice(NonceSize + plaintextLength, TagSize);

            try
            {
                _cipher.Decrypt(nonce, ciphertext, tag, destination.AsSpan(0, plaintextLength), _associatedData);
            }
            catch (CryptographicException)
            {
                // The only thing a failed tag tells us is that this packet is not ours: a wrong password,
                // a stale key, or something on the path that rewrote it. All three mean drop it.
                return false;
            }

            length = plaintextLength;
            return true;
        }

        private void NextNonce()
        {
            _counter++;
            _noncePrefix.CopyTo(_nonce, 0);

            ulong counter = _counter;
            for (int index = NonceSize - 1; index >= NoncePrefixSize; index--)
            {
                _nonce[index] = (byte)counter;
                counter >>= 8;
            }
        }

        private void BuildAssociatedData(byte senderVirtualId, byte channel)
        {
            _associatedData[0] = senderVirtualId;
            _associatedData[1] = channel;
        }
    }
}
