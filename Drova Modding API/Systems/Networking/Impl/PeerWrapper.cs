using LiteNetLib;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// The <see cref="INetPeer"/> the coop mod sees, wrapping a LiteNetLib <see cref="NetPeer"/>. One
    /// wrapper is created per connection and stashed in the peer's <c>Tag</c>, so the receive path
    /// resolves the same instance with a field read instead of allocating a wrapper per packet.
    /// </summary>
    internal sealed class PeerWrapper : INetPeer
    {
        internal NetPeer Native = null!;

        internal static PeerWrapper Of(NetPeer peer)
        {
            // Reuse the wrapper cached on the peer; only the first packet of a connection allocates.
            if (peer.Tag is PeerWrapper existing)
            {
                return existing;
            }
            PeerWrapper wrapper = new() { Native = peer };
            peer.Tag = wrapper;
            return wrapper;
        }

        public int Id => Native.Id;

        public int Ping => Native.Ping;

        public void Disconnect()
        {
            Native.Disconnect();
        }
    }
}
