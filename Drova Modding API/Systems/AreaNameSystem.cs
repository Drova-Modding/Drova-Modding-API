using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems
{
    /// <summary>
    /// A system that manages the area names.
    /// </summary>
    /// <param name="ptr">Do not instantiate</param>
    [RegisterTypeInIl2Cpp]
    public class AreaNameSystem(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private static AreaNameSystem _instance;

        private readonly List<Region> regions = [];

        /**
         * The delegate that is called when the player enters or leaves a region.
         */
        public delegate void RegionChanged(Region region, bool hasEntered);

        /**
         * The event that is called when the player enters or leaves a region.
         */
        [HideFromIl2Cpp]
        public event RegionChanged OnRegionChanged;

        /**
         * The instance of the AreaNameSystem.
         */
        public static AreaNameSystem? Instance
        {
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
        public void OnAreaEntered(string areaName)
        {
            regions.Add(RegionExtensions.GetRegionByName(areaName));
            OnRegionChanged?.Invoke(RegionExtensions.GetRegionByName(areaName), true);
        }

        /// <summary>
        /// Called when the player leaves an area.
        /// </summary>
        /// <param name="areaName">The name of the area</param>
        public void UnregisterAreaName(string areaName)
        {
            if (regions.Remove(RegionExtensions.GetRegionByName(areaName)))
            {
                OnRegionChanged?.Invoke(RegionExtensions.GetRegionByName(areaName), false);
            }
        }

        /// <summary>
        /// Checks if the player is currently in a cave. This is determined by checking if any of the current regions are caves.
        /// </summary>
        /// <returns></returns>
        public bool IsInCave()
        {
#if DEBUG
            MelonLogger.Msg("Checking if player is in a cave. Current regions: " + string.Join(", ", regions.Select(r => r.ToString())));
            MelonLogger.Msg("Is player in cave? " + regions.IsARegionInCave());
#endif
            return regions.IsARegionInCave();
        }
    }
}