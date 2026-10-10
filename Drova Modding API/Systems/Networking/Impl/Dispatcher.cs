using Drova_Modding_API.Systems.Networking.Diagnostics;
using LiteNetLib.Utils;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Routes typed and raw messages with no per-message allocation on the send or receive path.
    /// The single reused <see cref="LiteWriter"/> and <see cref="LiteReader"/> are safe without
    /// locking because the whole transport is pumped from the Unity main thread. Typed messages are
    /// looked up by a 1-byte id in a flat array; the id for a type is cached in
    /// <see cref="MessageType{T}"/> so a send never hashes on the type.
    /// </summary>
    internal static class Dispatcher
    {
        /// <summary>
        /// How many typed message ids exist - every id fits the one byte it travels in. Also the width of every per-id
        /// table in <see cref="NetworkDiagnostics"/>, so the two cannot drift apart.
        /// </summary>
        internal const int MaxMessageTypes = 256;

        private static readonly MessageChannel?[] TypedChannels = new MessageChannel[MaxMessageTypes];
        private static readonly RawHandler?[] RawHandlers = new RawHandler[LiteTransport.ChannelCount];
        private static readonly LiteWriter SharedWriter = new();
        private static readonly LiteReader SharedReader = new();

        internal static void RegisterWithId<T>(ushort id, Action<INetPeer, T> handler) where T : struct, INetMessage
        {
            if (id >= MaxMessageTypes)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "message id must be below " + MaxMessageTypes);
            }
            if (TypedChannels[id] != null)
            {
                throw new InvalidOperationException("message id " + id + " is already registered");
            }
            MessageType<T>.Id = id;
            MessageType<T>.Registered = true;
            TypedChannels[id] = new MessageChannel<T>(handler);
        }

        internal static void Unregister<T>() where T : struct, INetMessage
        {
            if (!MessageType<T>.Registered)
            {
                return;
            }
            TypedChannels[MessageType<T>.Id] = null;
            MessageType<T>.Registered = false;
        }

        internal static NetDataWriter BuildTyped<T>(in T message) where T : struct, INetMessage
        {
            RequireRegistered<T>();
            LiteWriter writer = SharedWriter;
            writer.Reset();
            // One byte: ids are below MaxMessageTypes. Two cost a byte on every message, about 30 a second each way.
            writer.Put((byte)MessageType<T>.Id);
            message.Write(writer);
            NetworkDiagnostics.RecordSent(MessageType<T>.Id, writer.Native.Length);
            return writer.Native;
        }

        private static void RequireRegistered<T>() where T : struct, INetMessage
        {
            if (!MessageType<T>.Registered)
            {
                throw new InvalidOperationException("message type " + typeof(T).Name + " was not registered with RegisterWithId before sending");
            }
        }

        internal static void OnTypedReceived(INetPeer peer, NetDataReader packet)
        {
            // A typed packet too short to hold its own id is not a message. Without this the read runs off
            // the end of the buffer, and the receive path's catch turns every one of them into a logged
            // stack trace - a flood anyone with the port can start.
            if (packet.AvailableBytes < sizeof(byte))
            {
                NetworkDiagnostics.CountMalformedPacket();
                return;
            }

            // Measured before the id is read, so the tally is the message as it sat on the wire.
            int size = packet.AvailableBytes;
            SharedReader.Bind(packet);
            ushort id = SharedReader.GetByte();
            MessageChannel? channel = id < MaxMessageTypes ? TypedChannels[id] : null;
            if (channel == null)
            {
                NetworkDiagnostics.CountUnknownMessageId();
                return;
            }

            NetworkDiagnostics.RecordReceived(id, size);
            channel.Handle(peer, SharedReader);
        }

        internal static void RegisterRawChannel(byte channel, RawHandler handler)
        {
            if (channel == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), "channel 0 is reserved for typed messages");
            }
            // The top channel is reserved in both modes, so a mod's channel numbers keep working when a
            // session moves from direct to relay.
            if (channel >= RelayProtocol.ControlChannel)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), "channel must be below " + RelayProtocol.ControlChannel);
            }
            RawHandlers[channel] = handler;
        }

        internal static void UnregisterRawChannel(byte channel)
        {
            if (channel >= RawHandlers.Length)
            {
                return;
            }
            RawHandlers[channel] = null;
        }

        internal static void OnRawReceived(INetPeer peer, byte channel, ReadOnlySpan<byte> data)
        {
            RawHandler? handler = channel < RawHandlers.Length ? RawHandlers[channel] : null;
            handler?.Invoke(peer, data);
        }

        internal static void Reset()
        {
            Array.Clear(TypedChannels, 0, TypedChannels.Length);
            Array.Clear(RawHandlers, 0, RawHandlers.Length);
        }
    }

    /// <summary>
    /// Per-type cache of the id a message was registered under. A generic static field gives an
    /// O(1) field read on the send path instead of a dictionary lookup keyed by <c>Type</c>.
    /// </summary>
    /// <typeparam name="T">The message struct type.</typeparam>
    internal static class MessageType<T> where T : struct, INetMessage
    {
        internal static ushort Id;
        internal static bool Registered;
    }
}
