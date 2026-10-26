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
        /// Read a length-prefixed string. A prefix claiming more bytes than the packet holds yields an
        /// empty string and drops the whole message, so a hostile length can never make the reader
        /// allocate past the packet it came in.
        /// </summary>
        string GetString();

        /// <summary>
        /// Read a length-prefixed string, refusing one longer than <paramref name="maxBytes"/> instead of
        /// decoding it. Use this for any field with a known bound: the packet size is the only limit the
        /// unbounded overload can apply, and a fragmented reliable packet is allowed to be large. A value
        /// over the cap is not truncated: it yields an empty string and the message is dropped.
        /// </summary>
        /// <param name="maxBytes">Longest value to accept, in UTF-8 bytes.</param>
        string GetString(int maxBytes);

        /// <summary>
        /// Read a length-prefixed byte array written by <see cref="INetWriter.Put(byte[])"/>. This
        /// allocates the returned array. A prefix naming more than the packet holds yields an empty array
        /// and drops the message.
        /// </summary>
        byte[] GetBytes();

        /// <summary>
        /// Read a length-prefixed byte array, refusing one longer than <paramref name="maxLength"/>.
        /// Same refusal rule as <see cref="GetString(int)"/>: over the cap is an empty array and a
        /// dropped message, never a truncated one.
        /// </summary>
        /// <param name="maxLength">Longest array to accept, in bytes.</param>
        byte[] GetBytes(int maxLength);
    }
}
