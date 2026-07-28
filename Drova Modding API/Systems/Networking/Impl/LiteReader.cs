using LiteNetLib.Utils;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Adapts an <see cref="INetReader"/> onto the packet reader LiteNetLib hands to the receive
    /// callback. A single instance is reused for every receive on the main thread; <see cref="Bind"/>
    /// points it at the current packet before dispatch. Being a class, passing it as
    /// <see cref="INetReader"/> does not box.
    /// </summary>
    internal sealed class LiteReader : INetReader
    {
        private NetDataReader _packet = null!;

        /// <summary>
        /// Whether this packet carried a number no game can use - a NaN or an infinity - and is therefore
        /// not to be handed to anybody.
        ///
        /// A flag rather than an exception, because this path is reachable by any peer: throwing turns one
        /// hostile packet into one logged stack trace, and a stream of them into a way of filling somebody
        /// else's disk. The message is still decoded to the end so the reader stays in step with the
        /// packet; <see cref="MessageChannel{T}"/> is what declines to deliver it.
        /// </summary>
        internal bool Poisoned { get; private set; }

        internal void Bind(NetDataReader packet)
        {
            _packet = packet;
            Poisoned = false;
        }

        public byte GetByte()
        {
            return _packet.GetByte();
        }

        public sbyte GetSByte()
        {
            return _packet.GetSByte();
        }

        public short GetShort()
        {
            return _packet.GetShort();
        }

        public ushort GetUShort()
        {
            return _packet.GetUShort();
        }

        public int GetInt()
        {
            return _packet.GetInt();
        }

        public uint GetUInt()
        {
            return _packet.GetUInt();
        }

        public long GetLong()
        {
            return _packet.GetLong();
        }

        public ulong GetULong()
        {
            return _packet.GetULong();
        }

        public float GetFloat()
        {
            float value = _packet.GetFloat();

            // NaN and the infinities are never legitimate game state, and letting one through is not a
            // cosmetic problem: a NaN position assigned to a transform is read back as the input to the
            // next frame's interpolation, so every frame after it is NaN too and the object never
            // recovers. Zero is substituted only so the rest of the message decodes; nothing acts on it,
            // because the message is dropped before it reaches a handler.
            if (!float.IsFinite(value))
            {
                Poisoned = true;
                return 0f;
            }

            return value;
        }

        public double GetDouble()
        {
            double value = _packet.GetDouble();

            if (!double.IsFinite(value))
            {
                Poisoned = true;
                return 0d;
            }

            return value;
        }

        public bool GetBool()
        {
            return _packet.GetBool();
        }

        public string GetString()
        {
            return _packet.GetString();
        }

        public byte[] GetBytes()
        {
            int length = _packet.GetInt();
            byte[] buffer = new byte[length];
            _packet.GetBytes(buffer, length);
            return buffer;
        }
    }
}
