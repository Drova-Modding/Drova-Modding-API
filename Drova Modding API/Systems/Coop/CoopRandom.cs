namespace Drova_Modding_API.Systems.Coop
{
    /// <summary>
    /// Turns a session seed and a caller's key into a number every machine works out identically.
    /// Keyed rather than a shared stream, which desyncs as soon as one machine draws once more.
    /// Name each draw specifically ("banditcamp.creature.0", not "creature"): two draws with one key are one draw.
    /// </summary>
    public static class CoopRandom
    {
        /// <summary>
        /// Derives an unsigned number from a seed and a key with FNV-1a over the seed's bytes and the key's characters.
        /// </summary>
        /// <param name="seed">The session seed.</param>
        /// <param name="key">What is being decided. Null is treated as empty.</param>
        public static uint Derive(ulong seed, string? key)
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;

            uint hash = offset;

            for (int shift = 0; shift < 64; shift += 8)
            {
                hash = (hash ^ (byte)(seed >> shift)) * prime;
            }

            if (key != null)
            {
                foreach (char character in key)
                {
                    hash = (hash ^ (byte)character) * prime;
                    hash = (hash ^ (byte)(character >> 8)) * prime;
                }
            }

            return hash;
        }

        /// <summary>
        /// Derives a number in <c>[0, 1)</c> from the top 24 bits, the width a float holds exactly.
        /// </summary>
        public static float DeriveValue(ulong seed, string? key)
        {
            return (Derive(seed, key) >> 8) / 16777216f;
        }

        /// <summary>
        /// Derives a number in a half-open range.
        /// </summary>
        /// <returns>The derived number, or <paramref name="minInclusive"/> when the range is empty.</returns>
        public static int DeriveRange(ulong seed, string? key, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;

            long span = (long)maxExclusive - minInclusive;

            return (int)(minInclusive + (long)(Derive(seed, key) % (ulong)span));
        }
    }
}
