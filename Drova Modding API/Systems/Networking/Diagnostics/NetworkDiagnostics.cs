using Drova_Modding_API.Systems.Networking.Impl;

namespace Drova_Modding_API.Systems.Networking.Diagnostics
{
    /// <summary>
    /// Counts what only the transport can know about its own traffic: packets it threw away before
    /// anybody saw them, and how the typed-message budget is spent per message id. LiteNetLib's
    /// <c>NetStatistics</c> already counts packets, bytes and reliable loss, so nothing here repeats it.
    ///
    /// Counting restarts when a session starts and survives it ending. Every write happens on the main
    /// thread the transport is polled from, so none of this synchronizes.
    /// </summary>
    internal static class NetworkDiagnostics
    {
        private static readonly long[] SentMessagesById = new long[Dispatcher.MaxMessageTypes];
        private static readonly long[] SentBytesById = new long[Dispatcher.MaxMessageTypes];
        private static readonly long[] ReceivedMessagesById = new long[Dispatcher.MaxMessageTypes];
        private static readonly long[] ReceivedBytesById = new long[Dispatcher.MaxMessageTypes];

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
        /// Sends dropped for being larger than a relay will forward. A relay drops these itself and counts
        /// the sender as abusive, so catching them here is the only way the sender learns.
        /// </summary>
        internal static long OversizedSends { get; private set; }

        /// <summary>
        /// Sends made before a session existed, which do nothing. A steady count is a mod sending before
        /// <see cref="Access.NetworkEvents.OnHostStarted"/> or
        /// <see cref="Access.NetworkEvents.OnConnectedToHost"/> told it the session was up.
        /// </summary>
        internal static long SendsWithoutSession { get; private set; }

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
            Array.Clear(SentMessagesById, 0, SentMessagesById.Length);
            Array.Clear(SentBytesById, 0, SentBytesById.Length);
            Array.Clear(ReceivedMessagesById, 0, ReceivedMessagesById.Length);
            Array.Clear(ReceivedBytesById, 0, ReceivedBytesById.Length);

            UndecryptablePackets = 0;
            RefusedMessages = 0;
            UnknownMessageIds = 0;
            MalformedPackets = 0;
            ReceiveFailures = 0;
            OversizedSends = 0;
            SendsWithoutSession = 0;
            CountingSinceMs = Environment.TickCount64;
        }

        internal static void CountUndecryptablePacket() => UndecryptablePackets++;

        internal static void CountRefusedMessage() => RefusedMessages++;

        internal static void CountUnknownMessageId() => UnknownMessageIds++;

        internal static void CountMalformedPacket() => MalformedPackets++;

        internal static void CountReceiveFailure() => ReceiveFailures++;

        internal static void CountOversizedSend() => OversizedSends++;

        internal static void CountSendWithoutSession() => SendsWithoutSession++;

        /// <summary>
        /// Records a typed message on its way out, sized as it serialized including its id. A send to
        /// every peer counts once, because that is how often it was built - what the fan-out costs on the
        /// wire shows up in LiteNetLib's byte counters instead.
        /// </summary>
        internal static void RecordSent(ushort id, int bytes)
        {
            if (id >= Dispatcher.MaxMessageTypes) return;

            SentMessagesById[id]++;
            SentBytesById[id] += bytes;
        }

        /// <summary>
        /// Records a typed message that arrived, sized as it occupied the packet including its id.
        /// Counted before delivery, so a message refused for a NaN is counted here and again under
        /// <see cref="RefusedMessages"/>.
        /// </summary>
        internal static void RecordReceived(ushort id, int bytes)
        {
            if (id >= Dispatcher.MaxMessageTypes) return;

            ReceivedMessagesById[id]++;
            ReceivedBytesById[id] += bytes;
        }

        internal static long SentMessages(int id) => SentMessagesById[id];

        internal static long SentBytes(int id) => SentBytesById[id];

        internal static long ReceivedMessages(int id) => ReceivedMessagesById[id];

        internal static long ReceivedBytes(int id) => ReceivedBytesById[id];
    }
}
