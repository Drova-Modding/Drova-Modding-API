namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// Delivery guarantee for a sent message. Names mirror the transport's delivery methods so the
    /// coop mod can reason about ordering and reliability without seeing the transport type.
    /// </summary>
    public enum Delivery
    {
        /// <summary>
        /// Reliable and ordered. The default; use for anything that must arrive exactly once in
        /// order (state changes, chat, actions).
        /// </summary>
        Reliable,

        /// <summary>
        /// No delivery or ordering guarantee. Cheapest; use for high-frequency data that is
        /// superseded by the next packet anyway (position, input ticks).
        /// </summary>
        Unreliable,

        /// <summary>
        /// Reliable but not ordered. Every message arrives, in any order.
        /// </summary>
        ReliableUnordered,

        /// <summary>
        /// Unreliable, but a message older than the last one received is dropped.
        /// </summary>
        Sequenced,

        /// <summary>
        /// Reliable and sequenced: only the newest message is kept, and it is guaranteed to arrive.
        /// </summary>
        ReliableSequenced,
    }
}
