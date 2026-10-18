using Drova_Modding_API.Systems;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Where in the world the player is, by region. Areas overlap, so both the resolved region and the whole set
    /// are offered.
    /// </summary>
    public static class RegionAccess
    {
        // Reused so the common question does not allocate a list every time it is asked.
        private static readonly List<Region> RegionBuffer = new();

        private static RegionChanged? _subscribers;
        private static AreaNameSystem? _attachedTo;

        /// <summary>
        /// Raised when the player enters or leaves a region. Safe to subscribe before the world exists, and the
        /// subscription survives a return to the main menu. A throwing subscriber does not stop the rest.
        /// </summary>
        public static event RegionChanged? OnRegionChanged
        {
            add
            {
                if (value == null) return;

                _subscribers += value;

                Attach(AreaNameSystem.Instance);
            }
            remove
            {
                // Only the API's own forwarder is ever subscribed to the system, so a mod's handler is
                // removed here and nowhere else.
                _subscribers -= value;
            }
        }

        /// <summary>
        /// The region the player is most specifically in: a cave inside a forest resolves to the cave.
        /// </summary>
        /// <param name="region">Where the player is.</param>
        /// <returns>False in menus and before the world has placed the player anywhere.</returns>
        public static bool TryGetCurrentRegion(out Region region)
        {
            region = default;

            AreaNameSystem? system = AreaNameSystem.Instance;
            if (system == null) return false;

            RegionBuffer.Clear();
            RegionBuffer.AddRange(system.Regions);

            if (RegionBuffer.Count == 0) return false;

            region = MostSpecific(RegionBuffer);
            return true;
        }

        /// <summary>
        /// Clears the list and fills it with every region the player is currently inside, unresolved.
        /// </summary>
        /// <param name="buffer">Filled with the current regions.</param>
        public static void GetCurrentRegions(List<Region> buffer)
        {
            if (buffer == null) return;

            buffer.Clear();

            AreaNameSystem? system = AreaNameSystem.Instance;
            if (system == null) return;

            buffer.AddRange(system.Regions);
        }

        /// <summary>
        /// Whether the player is inside a given region, whether or not it is the most specific one.
        /// </summary>
        /// <param name="region">Which region.</param>
        /// <returns>True when the player is inside it.</returns>
        public static bool IsInRegion(Region region)
        {
            AreaNameSystem? system = AreaNameSystem.Instance;
            if (system == null) return false;

            return system.Regions.Contains(region);
        }

        /// <summary>
        /// Whether the player is underground.
        /// </summary>
        /// <returns>False in menus and outdoors.</returns>
        public static bool IsInCave()
        {
            AreaNameSystem? system = AreaNameSystem.Instance;

            return system != null && system.IsInCave();
        }

        /// <summary>
        /// Resolves a region from an area's key. Unlike <c>RegionExtensions.GetRegionByName</c>, which answers an
        /// unknown name with the catch-all region, an unknown name returns false.
        /// </summary>
        /// <param name="areaKey">The area's key, as the game names it.</param>
        /// <param name="region">The region it names.</param>
        /// <returns>False when the name is empty or not one this build knows.</returns>
        public static bool TryGetRegionByName(string? areaKey, out Region region)
        {
            region = default;

            if (string.IsNullOrEmpty(areaKey)) return false;

            Region resolved = RegionExtensions.GetRegionByName(areaKey);

            // The catch-all is a real region, so it cannot simply be rejected. It is only suspicious when
            // the caller did not ask for it by name.
            if (resolved == Region.Overworld_Or_Cave
                && !string.Equals(areaKey, nameof(Region.Overworld_Or_Cave), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            region = resolved;
            return true;
        }

        // Caves win, since a cave is always inside something. Beyond that the last one entered wins.
        private static Region MostSpecific(List<Region> regions)
        {
            for (int index = regions.Count - 1; index >= 0; index--)
            {
                if (regions[index].IsCaveRegion()) return regions[index];
            }

            return regions[^1];
        }

        /// <summary>
        /// Binds the forwarder to a region system. Called by <see cref="AreaNameSystem"/> as it wakes, since a fresh
        /// one is built on every gameplay load.
        /// </summary>
        /// <param name="system">The system that just woke up.</param>
        internal static void Attach(AreaNameSystem? system)
        {
            if (system == null || ReferenceEquals(system, _attachedTo)) return;

            try
            {
                // One forwarder per system, so handlers can still be removed one at a time.
                system.OnRegionChanged += Forward;
                _attachedTo = system;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Region] could not subscribe to region changes: " + e);
            }
        }

        private static void Forward(Region region, bool hasEntered)
        {
            RegionChanged? subscribers = _subscribers;
            if (subscribers == null) return;

            foreach (Delegate subscriber in subscribers.GetInvocationList())
            {
                try
                {
                    ((RegionChanged)subscriber)(region, hasEntered);
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[Region] a subscriber to OnRegionChanged failed: " + e);
                }
            }
        }
    }
}
