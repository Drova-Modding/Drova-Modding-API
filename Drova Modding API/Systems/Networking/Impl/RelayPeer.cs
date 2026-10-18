namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// The <see cref="INetPeer"/> a coop mod sees for a remote player in a relay session. There is only
    /// one real transport connection in relay mode - the one to the relay - so a peer is really a virtual
    /// id, and one wrapper per id is created when the relay first mentions it.
    /// </summary>
    internal sealed class RelayPeer : INetPeer
    {
        private readonly LiteTransport _transport;

        internal RelayPeer(LiteTransport transport, byte virtualId)
        {
            _transport = transport;
            VirtualId = virtualId;
        }

        /// <summary>
        /// The id the relay knows this peer by. Also what <see cref="Id"/> reports, so the coop mod has
        /// one stable identity per player for as long as they are in the session.
        /// </summary>
        internal byte VirtualId { get; }

        /// <inheritdoc/>
        public int Id => VirtualId;

        /// <summary>
        /// One-way latency to the relay, not to this peer: the game side cannot measure the second leg.
        /// Read it as "at least this much".
        /// </summary>
        public int Ping => _transport.RelayPing;

        /// <summary>
        /// Removes this peer from the session. As the host that asks the relay to drop the client; as a
        /// client on the host peer it leaves the session, because a client cannot evict its host.
        /// </summary>
        public void Disconnect()
        {
            _transport.DisconnectRelayPeer(this);
        }
    }
}
