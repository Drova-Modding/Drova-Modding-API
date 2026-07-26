using LiteNetLib.Utils;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Adapts an <see cref="INetWriter"/> onto LiteNetLib's <see cref="NetDataWriter"/> so a
    /// message never sees the transport type. A single instance is reused for every send on the main
    /// thread; call <see cref="Reset"/> before building each packet. Being a class, passing it as
    /// <see cref="INetWriter"/> does not box.
    /// </summary>
    internal sealed class LiteWriter : INetWriter
    {
        internal readonly NetDataWriter Native = new();

        internal void Reset()
        {
            Native.Reset();
        }

        public void Put(byte value)
        {
            Native.Put(value);
        }

        public void Put(sbyte value)
        {
            Native.Put(value);
        }

        public void Put(short value)
        {
            Native.Put(value);
        }

        public void Put(ushort value)
        {
            Native.Put(value);
        }

        public void Put(int value)
        {
            Native.Put(value);
        }

        public void Put(uint value)
        {
            Native.Put(value);
        }

        public void Put(long value)
        {
            Native.Put(value);
        }

        public void Put(ulong value)
        {
            Native.Put(value);
        }

        public void Put(float value)
        {
            Native.Put(value);
        }

        public void Put(double value)
        {
            Native.Put(value);
        }

        public void Put(bool value)
        {
            Native.Put(value);
        }

        public void Put(string value)
        {
            Native.Put(value);
        }

        public void Put(byte[] value)
        {
            // Length-prefixed so the reader knows how many bytes to pull back; the matching read is
            // INetReader.GetBytes.
            Native.Put(value.Length);
            Native.Put(value);
        }
    }
}
