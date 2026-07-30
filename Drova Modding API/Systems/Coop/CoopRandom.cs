namespace Drova_Modding_API.Systems.Coop
{
    /// <summary>
    /// Turns a session seed and a caller's key into a number every machine works out identically.
    ///
    /// **Keyed derivation, never a sequential stream.** Handing mods a shared <c>Random</c> looks like the
    /// obvious answer and is a trap: two machines only stay in step if they draw from it the same number
    /// of times in the same order, and nothing can enforce that. A player running one extra mod, or a
    /// build with a debug branch that draws once more, offsets the stream permanently, with no symptom at
    /// the point of the offset and a divergence that reproduces on nobody's machine.
    ///
    /// Derivation cannot desync from consumption order because there is no order. It can only differ if
    /// the key differs, and a key is the caller's own composition: it can be logged, compared and fixed.
    /// The cost is that the caller has to name each draw.
    ///
    /// A key should name what is being decided, including anything that makes this draw different from
    /// the next one, so "banditcamp.creature.0" rather than "creature". Two draws with one key are one
    /// draw.
    /// </summary>
    public static class CoopRandom
    {
        /// <summary>
        /// Derives an unsigned number from a seed and a key.
        ///
        /// Uses FNV-1a over the seed's bytes followed by the key's characters. The choice matters less
        /// than the property: it must be pure, cheap, and identical on every machine regardless of runtime
        /// or culture. That rules out anything going through <c>string.GetHashCode</c>, which .NET
        /// randomizes per process, so it would produce a different answer on each machine and fail
        /// exactly the job this exists for.
        /// </summary>
        /// <param name="seed">The session seed.</param>
        /// <param name="key">What is being decided. Null is treated as empty.</param>
        /// <returns>The derived number.</returns>
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
        /// Derives a number in <c>[0, 1)</c>.
        ///
        /// Built from the top 24 bits, which is the width a float can hold exactly. Taking the low bits
        /// instead would quantize the result unevenly.
        /// </summary>
        /// <param name="seed">The session seed.</param>
        /// <param name="key">What is being decided.</param>
        /// <returns>A number from zero up to but not including one.</returns>
        public static float DeriveValue(ulong seed, string? key)
        {
            return (Derive(seed, key) >> 8) / 16777216f;
        }

        /// <summary>
        /// Derives a number in a half-open range.
        /// </summary>
        /// <param name="seed">The session seed.</param>
        /// <param name="key">What is being decided.</param>
        /// <param name="minInclusive">The lowest number that can come out.</param>
        /// <param name="maxExclusive">One past the highest.</param>
        /// <returns>The derived number, or <paramref name="minInclusive"/> when the range is empty.</returns>
        public static int DeriveRange(ulong seed, string? key, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;

            // The span as a long, because a range spanning the whole of int overflows an int.
            long span = (long)maxExclusive - minInclusive;

            return (int)(minInclusive + (long)(Derive(seed, key) % (ulong)span));
        }
    }
}
