using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems
{
    /**
     * The delegate that is called when the player enters or leaves a region.
     */
    public delegate void RegionChanged(Region region, bool hasEntered);

    /// <summary>
    /// A system that manages the area names.
    /// </summary>
    /// <param name="ptr">Do not instantiate</param>
    [RegisterTypeInIl2Cpp]
    public class AreaNameSystem(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private static AreaNameSystem? _instance;

        /// <summary>
        /// The regions the Player is currently in
        /// </summary>
        internal readonly List<Region> Regions = [];

        /// <summary>
        /// Cached result of <see cref="IsInCave"/>. Recomputed whenever the current Regions change.
        /// </summary>
        private bool _isInCave;


        /**
         * The event that is called when the player enters or leaves a region.
         */
        [HideFromIl2Cpp]
        public event RegionChanged OnRegionChanged;

        /**
         * The instance of the AreaNameSystem.
         */
        [HideFromIl2Cpp]
        public static AreaNameSystem? Instance
        {
            [HideFromIl2Cpp]
            get
            {
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null)
            {
                MelonLogger.Warning("AreaNameSystem already exists, destroying this instance.");
                Destroy(this);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Called when the player enters an area.
        /// </summary>
        /// <param name="areaName">The name of the area</param>
        [HideFromIl2Cpp]
        public void OnAreaEntered(string areaName)
        {
            Region region = RegionExtensions.GetRegionByName(areaName);
            Regions.Add(region);
            _isInCave = Regions.IsARegionInCave();
            OnRegionChanged?.Invoke(region, true);
        }

        /// <summary>
        /// Called when the player leaves an area.
        /// </summary>
        /// <param name="areaName">The name of the area</param>
        [HideFromIl2Cpp]
        public void UnregisterAreaName(string areaName)
        {
            Region region = RegionExtensions.GetRegionByName(areaName);
            if (Regions.Remove(region))
            {
                _isInCave = Regions.IsARegionInCave();
                OnRegionChanged?.Invoke(region, false);
            }
        }

        /// <summary>
        /// Checks if the player is currently in a cave. This is determined by checking if any of the current Regions are caves.
        /// </summary>
        /// <returns></returns>
        [HideFromIl2Cpp]
        public bool IsInCave()
        {
#if DEBUG
            MelonLogger.Msg("Checking if player is in a cave. Current Regions: " + string.Join(", ", Regions.Select(r => r.ToString())));
            MelonLogger.Msg("Is player in cave? " + _isInCave);
#endif
            return _isInCave;
        }
    }
}