namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// A message the coop mod can send and receive. Implement it on a <c>struct</c> so the dispatch
    /// path stays allocation- and boxing-free: the message registry is generic over the concrete
    /// struct type and calls <see cref="Write"/> and <see cref="Read"/> through a constrained
    /// generic, which the runtime resolves without boxing the value.
    ///
    /// Write and read the fields in the same order. Prefer fixed-layout primitive fields; a variable
    /// length field (string, byte array) allocates on the receiving side when it is materialized.
    /// </summary>
    public interface INetMessage
    {
        /// <summary>
        /// Serializes this message's fields into the outgoing packet.
        /// </summary>
        /// <param name="w">The writer for the packet being built.</param>
        void Write(INetWriter w);

        /// <summary>
        /// Deserializes this message's fields from the incoming packet, in the order
        /// <see cref="Write"/> wrote them.
        /// </summary>
        /// <param name="r">The reader for the packet being consumed.</param>
        void Read(INetReader r);
    }
}
