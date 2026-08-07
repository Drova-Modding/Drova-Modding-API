namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// A remote peer in the session. Instances are owned by the transport and reused for the life
    /// of the connection, so it is safe to use one as a dictionary key while the peer stays
    /// connected. Do not hold a reference past <see cref="Access.NetworkEvents.OnPeerDisconnected"/>.
    /// </summary>
    public interface INetPeer
    {
        /// <summary>
        /// The transport-assigned id of this peer, stable for the connection's lifetime.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The last measured one-way latency to this peer in milliseconds, which is half the round
        /// trip. Double it before comparing against anything expressed as a round trip.
        /// </summary>
        int Ping { get; }

        /// <summary>
        /// Close the connection to this peer.
        /// </summary>
        void Disconnect();
    }
}
