namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// Reads the fields of an <see cref="INetMessage"/> back out of an incoming packet, in the same
    /// order they were written. The concrete implementation is a single instance reused across
    /// receives on the main thread, so a message's <see cref="INetMessage.Read"/> must not stash the
    /// reader for later use.
    /// </summary>
    public interface INetReader
    {
        /// <summary>
        /// Read a single byte.
        /// </summary>
        byte GetByte();

        /// <summary>
        /// Read a signed byte.
        /// </summary>
        sbyte GetSByte();

        /// <summary>
        /// Read a 16-bit signed integer.
        /// </summary>
        short GetShort();

        /// <summary>
        /// Read a 16-bit unsigned integer.
        /// </summary>
        ushort GetUShort();

        /// <summary>
        /// Read a 32-bit signed integer.
        /// </summary>
        int GetInt();

        /// <summary>
        /// Read a 32-bit unsigned integer.
        /// </summary>
        uint GetUInt();

        /// <summary>
        /// Read a 64-bit signed integer.
        /// </summary>
        long GetLong();

        /// <summary>
        /// Read a 64-bit unsigned integer.
        /// </summary>
        ulong GetULong();

        /// <summary>
        /// Read a single-precision float.
        /// </summary>
        float GetFloat();

        /// <summary>
        /// Read a double-precision float.
        /// </summary>
        double GetDouble();

        /// <summary>
        /// Read a boolean.
        /// </summary>
        bool GetBool();

        /// <summary>
        /// Read a length-prefixed string.
        /// </summary>
        string GetString();

        /// <summary>
        /// Read a length-prefixed byte array written by <see cref="INetWriter.Put(byte[])"/>. This
        /// allocates the returned array; fixed-layout messages that avoid it stay allocation-free.
        /// </summary>
        byte[] GetBytes();
    }
}
