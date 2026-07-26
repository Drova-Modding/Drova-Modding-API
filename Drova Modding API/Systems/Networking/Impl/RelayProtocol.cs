using LiteNetLib.Utils;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// The game side's half of the relay wire contract. Every value here mirrors the
    /// <c>Drova-Coop-Relay</c> server's <c>RelayProtocol</c>; the two must be changed together and a
    /// change means bumping <see cref="Version"/>. The relay's <c>docs/protocol.md</c> is the spec.
    /// </summary>
    internal static class RelayProtocol
    {
        /// <summary>
        /// Protocol version announced in the handshake. A relay speaking anything else rejects us.
        ///
        /// Version 2 replaced the plaintext password in the handshake with a derived join token. The
        /// field kept its shape, so only the bump tells a version 1 relay apart from one that will
        /// actually understand us - without it the mismatch would surface as a wrong password.
        /// </summary>
        internal const ushort Version = 2;

        internal const byte RoleHost = 0;
        internal const byte RoleClient = 1;

        /// <summary>
        /// The host's virtual id, and the only target a client may address.
        /// </summary>
        internal const byte HostVirtualId = 0;

        /// <summary>
        /// As a target: every client in the session. As a sender: the relay itself.
        /// </summary>
        internal const byte BroadcastVirtualId = 255;

        /// <summary>
        /// Same value as <see cref="BroadcastVirtualId"/>, spelled differently where it means "the
        /// relay" rather than "everyone".
        /// </summary>
        internal const byte RelayVirtualId = 255;

        /// <summary>
        /// The one byte every relayed packet carries in front: the target id going out, the sender id
        /// coming back. It stays in the clear even in an encrypted session, because the relay has to read
        /// it to route at all.
        /// </summary>
        internal const int HeaderSize = 1;

        /// <summary>
        /// The channel the relay keeps for its own traffic. <see cref="Dispatcher"/> refuses to hand it
        /// out as a raw channel, in direct mode too, so the rule stays the same everywhere.
        /// </summary>
        internal const byte ControlChannel = 63;

        /// <summary>
        /// Relay to game: a peer's virtual id follows.
        /// </summary>
        internal const byte ControlWelcome = 1;

        /// <summary>
        /// Relay to host: the virtual id of a client that joined follows.
        /// </summary>
        internal const byte ControlPeerJoined = 2;

        /// <summary>
        /// Relay to host: the virtual id of a client that left follows.
        /// </summary>
        internal const byte ControlPeerLeft = 3;

        /// <summary>
        /// Relay to host: the code the relay picked follows, as a length-prefixed string rather than the
        /// single byte every other opcode carries. Arrives before <see cref="ControlWelcome"/>.
        /// </summary>
        internal const byte ControlSessionCode = 4;

        /// <summary>
        /// Longest session code the relay will ever send, and the cap the reader is given so a hostile
        /// length prefix cannot make us allocate.
        /// </summary>
        internal const int MaxSessionCodeLength = 16;

        /// <summary>
        /// Host to relay: drop the client whose virtual id follows.
        /// </summary>
        internal const byte ControlKickPeer = 1;

        /// <summary>
        /// Disconnect payload: the host left or the session timed out.
        /// </summary>
        internal const byte DisconnectSessionClosed = 1;

        /// <summary>
        /// Disconnect payload: the host had this peer removed.
        /// </summary>
        internal const byte DisconnectKicked = 2;

        /// <summary>
        /// Builds the connect data the relay expects. Allocates, which is fine: this runs once per
        /// session, not per packet.
        /// </summary>
        /// <param name="role">Either <see cref="RoleHost"/> or <see cref="RoleClient"/>.</param>
        /// <param name="sessionCode">The code that identifies the session. Empty from a host asks the relay to pick one.</param>
        /// <param name="joinToken">
        /// The value derived from the password by <c>SessionCrypto.DeriveJoinToken</c>, or null for an
        /// open session. Never the password itself: the relay is not a party this session trusts.
        /// </param>
        internal static NetDataWriter BuildHandshake(byte role, string? sessionCode, byte[]? joinToken)
        {
            NetDataWriter writer = new();
            writer.Put(Version);
            writer.Put(role);
            writer.Put(sessionCode ?? string.Empty);
            PutLengthPrefixed(writer, joinToken);
            return writer;
        }

        // The token field is read with the same length convention the string field it replaced used -
        // a ushort holding the byte count plus one, so that zero can still mean "absent" rather than
        // "one byte". Writing it by hand keeps that explicit; NetDataWriter's own byte-array helper
        // prefixes an int instead and would silently desynchronise the two ends.
        private static void PutLengthPrefixed(NetDataWriter writer, byte[]? value)
        {
            if (value == null || value.Length == 0)
            {
                writer.Put((ushort)0);
                return;
            }

            writer.Put((ushort)(value.Length + 1));
            writer.Put(value);
        }

        /// <summary>
        /// Folds a relay reject byte into the public enum. An unknown byte means the relay is newer
        /// than we are, which is worth reporting as unspecified rather than guessing.
        /// </summary>
        internal static ConnectionRejection MapRejection(byte reason)
        {
            return reason switch
            {
                1 => ConnectionRejection.BadHandshake,
                2 => ConnectionRejection.BadSessionCode,
                3 => ConnectionRejection.SessionNotFound,
                4 => ConnectionRejection.SessionFull,
                5 => ConnectionRejection.DuplicateHost,
                6 => ConnectionRejection.WrongPassword,
                7 => ConnectionRejection.RelayFull,
                8 => ConnectionRejection.RateLimited,
                9 => ConnectionRejection.TooManySessions,
                10 => ConnectionRejection.TooManyAttempts,
                _ => ConnectionRejection.Unspecified,
            };
        }
    }
}
