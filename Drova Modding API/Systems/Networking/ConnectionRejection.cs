namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// Why a host or relay refused a connection attempt. Reported through
    /// <see cref="Access.NetworkEvents.OnConnectionRejected"/> so a coop mod can tell the player what
    /// went wrong instead of showing a bare connection failure.
    /// </summary>
    public enum ConnectionRejection
    {
        /// <summary>
        /// The other side gave no reason. This is what a direct host does when the connection key does
        /// not match.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The relay could not read the handshake, or it speaks a different protocol version. Usually
        /// means the coop mod and the relay are different versions.
        /// </summary>
        BadHandshake,

        /// <summary>
        /// The session code was malformed.
        /// </summary>
        BadSessionCode,

        /// <summary>
        /// No host holds that session code. Either it was mistyped or the host has left.
        /// </summary>
        SessionNotFound,

        /// <summary>
        /// The session already holds as many players as it allows.
        /// </summary>
        SessionFull,

        /// <summary>
        /// Another host already owns that session code, so this one cannot host under it.
        /// </summary>
        DuplicateHost,

        /// <summary>
        /// The session is password protected and the password did not match.
        /// </summary>
        WrongPassword,

        /// <summary>
        /// The relay is carrying as many sessions as it allows. Not the player's fault; retrying later
        /// or using another relay is the fix.
        /// </summary>
        RelayFull,

        /// <summary>
        /// Too many connection attempts from this address in a short time.
        /// </summary>
        RateLimited,

        /// <summary>
        /// This address already hosts as many sessions on that relay as it is allowed to.
        /// </summary>
        TooManySessions,

        /// <summary>
        /// The session stopped accepting joins after repeated wrong passwords. Trying again immediately
        /// will not help; the session itself is holding the door shut.
        /// </summary>
        TooManyAttempts,
    }
}
