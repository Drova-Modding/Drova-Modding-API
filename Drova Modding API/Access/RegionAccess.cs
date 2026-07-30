using Drova_Modding_API.Systems;
using Il2CppDrova;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Where in the world the player is, by region.
    ///
    /// **There was no way to read this at all**, only to be told when it changed, which forces every mod
    /// that cares to keep its own copy, updated from a callback it might have subscribed to late.
    ///
    /// **And a single current region is not a fact about the world.** Areas overlap: a cave mouth inside a
    /// forest inside the overworld puts the player in three at once, and the game resolves that by
    /// priority rather than by order of entry. So the honest surface offers both, the resolved one for
    /// the common question and the whole set for the rare one.
    /// </summary>
    public static class RegionAccess
    {
        // Reused so the common question does not allocate a list every time it is asked.
        private static readonly List<Region> _buffer = new();

        /// <summary>
        /// Raised when the player enters or leaves a region.
        ///
        /// A pass-through of the underlying system's event, offered here so a mod has one place to look
        /// rather than reaching into <c>AreaNameSystem</c>. Subscribing before the world exists is safe,
        /// and so is holding the subscription across a return to the main menu, because the API attaches
        /// to whichever system is current rather than to the one that existed when you subscribed.
        ///
        /// Each subscriber is isolated, so one that throws does not stop the rest being told.
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

        private static RegionChanged? _subscribers;
        private static AreaNameSystem? _attachedTo;

        /// <summary>
        /// The region the player is most specifically in.
        ///
        /// **Resolved rather than picked**, because the answer to "where am I" when standing in three
        /// overlapping areas is the innermost one. A cave inside a forest resolves to the cave.
        /// </summary>
        /// <param name="region">Where the player is.</param>
        /// <returns>False in menus and before the world has placed the player anywhere.</returns>
        public static bool TryGetCurrentRegion(out Region region)
        {
            region = default;

            AreaNameSystem? system = AreaNameSystem.Instance;
            if (system == null) return false;

            _buffer.Clear();
            _buffer.AddRange(system.Regions);

            if (_buffer.Count == 0) return false;

            region = MostSpecific(_buffer);
            return true;
        }

        /// <summary>
        /// Fills the list with every region the player is currently inside, unresolved.
        ///
        /// For the rare caller that genuinely wants all of them, something asking "am I anywhere in the
        /// moor" rather than "where am I". The list is cleared first and reusing one allocates nothing.
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
        /// Resolves a region from an area's key.
        ///
        /// **A <c>Try</c> pair because the underlying lookup cannot fail.** It answers an unrecognized name
        /// with the catch-all region rather than with an error, so a mod with a typo in its configuration
        /// binds silently to a bucket that really exists, and then behaves as though every unmatched area
        /// were that region. Here, an unrecognized name is a false.
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

        /// <summary>
        /// Picks the innermost of the regions the player is standing in.
        ///
        /// Caves win over everything, because a cave is always inside something. Beyond that the last one
        /// entered wins, which matches how the areas are nested in practice: a player walks from the
        /// general into the specific.
        /// </summary>
        private static Region MostSpecific(List<Region> regions)
        {
            for (int index = regions.Count - 1; index >= 0; index--)
            {
                if (regions[index].IsCaveRegion()) return regions[index];
            }

            return regions[^1];
        }

        /// <summary>
        /// Binds the forwarder to a region system. Called by <see cref="AreaNameSystem"/> as it wakes,
        /// which is the only moment the API can know a new one exists.
        ///
        /// A fresh system is built every time the gameplay scene loads, so a mod that subscribed during a
        /// previous session is holding a subscription to a destroyed object. Rebinding here is what keeps
        /// those handlers alive across a return to the main menu.
        /// </summary>
        /// <param name="system">The system that just woke up.</param>
        internal static void Attach(AreaNameSystem? system)
        {
            if (system == null || ReferenceEquals(system, _attachedTo)) return;

            try
            {
                // One delegate per system, never the subscriber list itself. The system would then hold
                // handlers this class could no longer remove one at a time.
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
