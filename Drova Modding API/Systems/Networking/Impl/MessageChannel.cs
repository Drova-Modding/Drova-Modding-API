using Drova_Modding_API.Systems.Networking.Diagnostics;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// A registered typed-message id, type-erased so <see cref="Dispatcher"/> can hold an array of
    /// them indexed by id without knowing each message type at that level.
    /// </summary>
    internal abstract class MessageChannel
    {
        internal abstract void Handle(INetPeer peer, LiteReader reader);
    }

    /// <summary>The concrete channel for one message type, generic so decoding does not box.</summary>
    internal sealed class MessageChannel<T> : MessageChannel where T : struct, INetMessage
    {
        private readonly Action<INetPeer, T> _handler;

        internal MessageChannel(Action<INetPeer, T> handler)
        {
            _handler = handler;
        }

        internal override void Handle(INetPeer peer, LiteReader reader)
        {
            T message = default;
            message.Read(reader);

            // Dropped rather than repaired: a NaN written into a transform feeds every later frame, and a
            // mod cannot be expected to check every float it reads.
            if (reader.Poisoned)
            {
                NetworkDiagnostics.CountRefusedMessage();
                return;
            }

            _handler(peer, message);
        }
    }
}
