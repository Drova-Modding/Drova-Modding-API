using System.Text;
using LiteNetLib.Utils;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Adapts an <see cref="INetReader"/> onto the packet reader LiteNetLib hands to the receive
    /// callback. A single instance is reused for every receive on the main thread; <see cref="Bind"/>
    /// points it at the current packet before dispatch.
    /// </summary>
    internal sealed class LiteReader : INetReader
    {
        private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        private NetDataReader _packet = null!;

        /// <summary>
        /// Whether this packet carried something no game can use, and is therefore not to be handed to
        /// anybody. Two things set it: a number that is a NaN or an infinity, and a length prefix that
        /// names more bytes than the packet holds or than the field allows. A flag rather than an exception,
        /// since any peer can reach this path and a stack trace per packet would flood the log.
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

            // Zero only keeps the rest of the message decoding; the poisoned message is dropped before any handler.
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
            return GetString(int.MaxValue);
        }

        public string GetString(int maxBytes)
        {
            if (!TryTakeBytes(maxBytes, out int offset, out int count) || count == 0) return string.Empty;

            return Utf8.GetString(_packet.RawData, offset, count);
        }

        public byte[] GetBytes()
        {
            return GetBytes(int.MaxValue);
        }

        public byte[] GetBytes(int maxLength)
        {
            if (_packet.AvailableBytes < sizeof(int))
            {
                Poisoned = true;
                return Array.Empty<byte>();
            }

            int length = _packet.GetInt();

            // Never allocate for a length the packet cannot back.
            if (length < 0 || length > _packet.AvailableBytes)
            {
                Poisoned = true;
                return Array.Empty<byte>();
            }

            if (length > maxLength)
            {
                _packet.SkipBytes(length);
                Poisoned = true;
                return Array.Empty<byte>();
            }

            if (length == 0) return Array.Empty<byte>();

            byte[] buffer = new byte[length];
            _packet.GetBytes(buffer, length);
            return buffer;
        }

        /// <summary>
        /// Takes a length-prefixed run of bytes without copying it, poisoning the read when the prefix is
        /// unusable. LiteNetLib's own capped string read cannot be used for this: it caps in characters
        /// and returns an empty string when the value is over the cap, so an over-long field arrives as
        /// no field at all with nothing to distinguish it from one that was genuinely empty.
        /// </summary>
        /// <param name="maxBytes">Longest value to accept.</param>
        /// <param name="offset">Where the value starts in <see cref="NetDataReader.RawData"/>.</param>
        /// <param name="count">How many bytes it holds, zero when there is nothing to read.</param>
        /// <returns>False when the prefix was truncated, lied, or named more than the cap allows.</returns>
        private bool TryTakeBytes(int maxBytes, out int offset, out int count)
        {
            offset = 0;
            count = 0;

            if (_packet.AvailableBytes < sizeof(ushort))
            {
                Poisoned = true;
                return false;
            }

            // The writer stores the byte count plus one so that zero can mean absent rather than one byte.
            ushort size = _packet.GetUShort();
            if (size == 0) return true;

            int bytes = size - 1;
            if (bytes > _packet.AvailableBytes)
            {
                Poisoned = true;
                return false;
            }

            if (bytes > maxBytes)
            {
                // Skipped so the fields after it still line up.
                _packet.SkipBytes(bytes);
                Poisoned = true;
                return false;
            }

            offset = _packet.Position;
            count = bytes;
            _packet.SkipBytes(bytes);
            return true;
        }
    }
}
