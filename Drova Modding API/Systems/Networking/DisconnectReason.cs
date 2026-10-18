namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// Why a peer left the session. The transport's own richer reason set is folded down into these
    /// cases so the coop mod does not depend on the transport enum.
    /// </summary>
    public enum DisconnectReason
    {
        /// <summary>
        /// The connection attempt never completed (host unreachable, wrong address, rejected key).
        /// </summary>
        ConnectionFailed,

        /// <summary>
        /// The peer stopped responding and timed out.
        /// </summary>
        Timeout,

        /// <summary>
        /// The remote side closed the connection deliberately.
        /// </summary>
        RemoteClose,

        /// <summary>
        /// This side closed the connection deliberately (via <see cref="INetPeer.Disconnect"/> or
        /// <c>Stop</c>).
        /// </summary>
        LocalClose,

        /// <summary>
        /// The host rejected the connection request. The reason, when there is one, arrives separately
        /// through <see cref="Access.NetworkEvents.OnConnectionRejected"/>.
        /// </summary>
        Rejected,

        /// <summary>
        /// Relay sessions only: the host left or the session timed out, so the whole session is gone.
        /// </summary>
        SessionClosed,

        /// <summary>
        /// Relay sessions only: the host had this peer removed from the session.
        /// </summary>
        Kicked,

        /// <summary>
        /// The reason did not map to any of the above.
        /// </summary>
        Unknown,
    }
}
