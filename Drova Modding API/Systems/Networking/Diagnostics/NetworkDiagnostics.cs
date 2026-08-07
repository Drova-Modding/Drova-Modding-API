using Drova_Modding_API.Systems.Networking.Impl;

namespace Drova_Modding_API.Systems.Networking.Diagnostics
{
    /// <summary>
    /// Counts what only the transport can know about its own traffic: packets it threw away before
    /// anybody saw them, and how the typed-message budget is spent per message id. LiteNetLib's
    /// <c>NetStatistics</c> already counts packets, bytes and reliable loss, so nothing here repeats it.
    ///
    /// Counting restarts whenever a session does, so a number read here belongs to the session in front
    /// of the reader rather than to a run of them; the counters outlive the session ending, which is
    /// usually the moment somebody wants to look at them. Every write happens on the Unity main thread
    /// the transport is polled from, so none of this synchronizes.
    /// </summary>
    internal static class NetworkDiagnostics
    {
        private static readonly long[] _sentMessages = new long[Dispatcher.MaxMessageTypes];
        private static readonly long[] _sentBytes = new long[Dispatcher.MaxMessageTypes];
        private static readonly long[] _receivedMessages = new long[Dispatcher.MaxMessageTypes];
        private static readonly long[] _receivedBytes = new long[Dispatcher.MaxMessageTypes];

        /// <summary>
        /// Relay packets that failed to decrypt. Either a password differing from the host's or a packet
        /// somebody rewrote; the two are indistinguishable from this side.
        /// </summary>
        internal static long UndecryptablePackets { get; private set; }

        /// <summary>
        /// Messages decoded but refused for carrying a NaN or an infinity, so no handler saw them.
        /// </summary>
        internal static long RefusedMessages { get; private set; }

        /// <summary>
        /// Typed messages whose id nobody registered a handler for. A steady count is a version
        /// mismatch between the two ends rather than a network fault.
        /// </summary>
        internal static long UnknownMessageIds { get; private set; }

        /// <summary>
        /// Typed packets too short to hold even a message id.
        /// </summary>
        internal static long MalformedPackets { get; private set; }

        /// <summary>
        /// Receives that threw. The stack trace is in the log; this is how often it has happened.
        /// </summary>
        internal static long ReceiveFailures { get; private set; }

        /// <summary>
        /// <see cref="Environment.TickCount64"/> at the last <see cref="Reset"/>, which is the point every
        /// counter above is measured from.
        /// </summary>
        internal static long CountingSinceMs { get; private set; }

        /// <summary>
        /// Clears every counter and restarts the window they are measured over. The transport calls this
        /// when a session starts; the stats view offers it as a button, so one action can be measured on
        /// its own instead of against a whole session.
        /// </summary>
        internal static void Reset()
        {
            Array.Clear(_sentMessages, 0, _sentMessages.Length);
            Array.Clear(_sentBytes, 0, _sentBytes.Length);
            Array.Clear(_receivedMessages, 0, _receivedMessages.Length);
            Array.Clear(_receivedBytes, 0, _receivedBytes.Length);

            UndecryptablePackets = 0;
            RefusedMessages = 0;
            UnknownMessageIds = 0;
            MalformedPackets = 0;
            ReceiveFailures = 0;
            CountingSinceMs = Environment.TickCount64;
        }

        internal static void CountUndecryptablePacket() => UndecryptablePackets++;

        internal static void CountRefusedMessage() => RefusedMessages++;

        internal static void CountUnknownMessageId() => UnknownMessageIds++;

        internal static void CountMalformedPacket() => MalformedPackets++;

        internal static void CountReceiveFailure() => ReceiveFailures++;

        /// <summary>
        /// Records a typed message on its way out, sized as it serialized including its id. A send to
        /// every peer counts once, because that is how often it was built - what the fan-out costs on the
        /// wire shows up in LiteNetLib's byte counters instead.
        /// </summary>
        /// <param name="id">The registered message id.</param>
        /// <param name="bytes">Serialized length of the message.</param>
        internal static void RecordSent(ushort id, int bytes)
        {
            if (id >= Dispatcher.MaxMessageTypes) return;

            _sentMessages[id]++;
            _sentBytes[id] += bytes;
        }

        /// <summary>
        /// Records a typed message that arrived, sized as it occupied the packet including its id.
        /// Counted before delivery, so a message refused for a NaN is counted here and again under
        /// <see cref="RefusedMessages"/>.
        /// </summary>
        /// <param name="id">The registered message id.</param>
        /// <param name="bytes">Length of the message in the packet.</param>
        internal static void RecordReceived(ushort id, int bytes)
        {
            if (id >= Dispatcher.MaxMessageTypes) return;

            _receivedMessages[id]++;
            _receivedBytes[id] += bytes;
        }

        internal static long SentMessages(int id) => _sentMessages[id];

        internal static long SentBytes(int id) => _sentBytes[id];

        internal static long ReceivedMessages(int id) => _receivedMessages[id];

        internal static long ReceivedBytes(int id) => _receivedBytes[id];
    }
}
