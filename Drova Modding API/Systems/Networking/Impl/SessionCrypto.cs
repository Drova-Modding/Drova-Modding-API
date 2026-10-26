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
    /// It does not stop a hostile relay replaying a genuine packet: rejecting a repeated counter would
    /// break unreliable channels. One instance per session, used only from the poll thread.
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

        // The relay sees only the value derived under the admit label, so a join token is no step toward the key.
        // The admit label leaves the session code out because a host may not know its code at handshake time;
        // the key keeps it, so one precomputed table cannot serve every session.
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

            // Virtual ids are reused across rejoins, so the counter alone could repeat a nonce under the same key.
            RandomNumberGenerator.Fill(_noncePrefix);
        }

        /// <summary>
        /// Derives the session key from the password and the session code and builds the cipher. Both
        /// ends derive it themselves, so no key material crosses the relay. Deliberately slow, about a
        /// tenth of a second, to make a short password expensive to grind.
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
        /// Derives what the handshake presents to the relay instead of the password: proof of the
        /// secret that cannot be walked back to the key.
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
