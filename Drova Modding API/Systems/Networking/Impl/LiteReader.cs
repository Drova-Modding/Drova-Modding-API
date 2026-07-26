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

        internal void Bind(NetDataReader packet)
        {
            _packet = packet;
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
            return _packet.GetFloat();
        }

        public double GetDouble()
        {
            return _packet.GetDouble();
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
