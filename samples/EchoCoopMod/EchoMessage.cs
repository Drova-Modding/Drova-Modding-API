using Drova_Modding_API.Systems.Networking;

namespace EchoCoopMod
{
    /// <summary>
    /// The one message this sample sends. Messages are structs so they serialize without boxing or a
    /// per-packet allocation; a <c>string</c> field would allocate on every receive.
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
