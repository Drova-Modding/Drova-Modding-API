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

    /// <summary>
    /// The concrete channel for one message type. Keeping <see cref="Handle"/> inside a generic
    /// class constrained to <c>struct, INetMessage</c> is what makes decode boxing-free: reading
    /// into a local <typeparamref name="T"/> and calling its <see cref="INetMessage.Read"/> is a
    /// constrained call, and passing the value to the strongly typed handler does not box either.
    /// </summary>
    /// <typeparam name="T">The message struct type this channel decodes.</typeparam>
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
            _handler(peer, message);
        }
    }
}
