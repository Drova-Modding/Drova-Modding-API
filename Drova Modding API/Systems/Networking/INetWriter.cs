namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// Writes the fields of an <see cref="INetMessage"/> into an outgoing packet. The concrete
    /// implementation is a single instance reused across sends on the main thread, so a message's
    /// <see cref="INetMessage.Write"/> must not stash the writer for later use.
    /// </summary>
    public interface INetWriter
    {
        /// <summary>
        /// Write a single byte.
        /// </summary>
        void Put(byte value);

        /// <summary>
        /// Write a signed byte.
        /// </summary>
        void Put(sbyte value);

        /// <summary>
        /// Write a 16-bit signed integer.
        /// </summary>
        void Put(short value);

        /// <summary>
        /// Write a 16-bit unsigned integer.
        /// </summary>
        void Put(ushort value);

        /// <summary>
        /// Write a 32-bit signed integer.
        /// </summary>
        void Put(int value);

        /// <summary>
        /// Write a 32-bit unsigned integer.
        /// </summary>
        void Put(uint value);

        /// <summary>
        /// Write a 64-bit signed integer.
        /// </summary>
        void Put(long value);

        /// <summary>
        /// Write a 64-bit unsigned integer.
        /// </summary>
        void Put(ulong value);

        /// <summary>
        /// Write a single-precision float.
        /// </summary>
        void Put(float value);

        /// <summary>
        /// Write a double-precision float.
        /// </summary>
        void Put(double value);

        /// <summary>
        /// Write a boolean.
        /// </summary>
        void Put(bool value);

        /// <summary>
        /// Write a length-prefixed string.
        /// </summary>
        void Put(string value);

        /// <summary>
        /// Write a length-prefixed byte array. The matching read is <see cref="INetReader.GetBytes"/>.
        /// </summary>
        void Put(byte[] value);
    }
}
