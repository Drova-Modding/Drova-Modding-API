using Drova_Modding_API.Systems.Networking;

namespace EchoCoopMod
{
    /// <summary>
    /// The one message this sample sends. A struct implementing <see cref="INetMessage"/> is
    /// serialised without boxing and without a per-packet allocation, which is why messages are
    /// structs and not classes. Keep the fields to blittable types for that to hold - adding a
    /// <c>string</c> field would allocate on every receive.
    /// </summary>
    public struct EchoMessage : INetMessage
    {
        /// <summary>
        /// Counts up per send so the log shows which round trip completed.
        /// </summary>
        public int Sequence;

        /// <inheritdoc/>
        public void Write(INetWriter writer)
        {
            writer.Put(Sequence);
        }

        /// <inheritdoc/>
        public void Read(INetReader reader)
        {
            Sequence = reader.GetInt();
        }
    }
}
