using Drova_Modding_API.Systems.WorldEvents.Regional;
using UnityEngine;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// Coordinates region-based world events, including enter/leave callbacks,
    /// runtime lifecycle, blocked regions, and per-region cooldowns.
    /// </summary>
    /// <param name="runningRegionalEvents">Shared collection of currently running regional events.</param>
    /// <param name="getRegionalCooldownSeconds">Provider for cooldown duration applied after starting regional events.</param>
    internal sealed class WorldRegionCoordinator(List<ARegionalEvent> runningRegionalEvents, Func<float> getRegionalCooldownSeconds)
    {
        private readonly Dictionary<Region, float> _regionCooldowns = [];
        private readonly HashSet<Region> _blockedRegions =
        [
            Region.Nemeton,
            Region.EntryNemeton,
            Region.RedTower,
            Region.Tavern,
            Region.Academy,
            Region.WoodCamp,
            Region.Magecamp,
            Region.Ruinexplorer,
            Region.RuinSchmuggler,
            Region.RuinsCamp
        ];

        /// <summary>
        /// Handles player transitions for a region and updates event lifecycle accordingly.
        /// </summary>
        /// <param name="region">The region that changed state.</param>
        /// <param name="hasEntered"><see langword="true"/> when entering; <see langword="false"/> when leaving.</param>
        public void OnRegionChanged(Region region, bool hasEntered)
        {
            HandleRegionChange(region, hasEntered);
            HandleRunningEvents(region, hasEntered);
        }

        /// <summary>
        /// Adds a region to the blocked list used by <see cref="IsPlayerInBlockedRegion"/>.
        /// </summary>
        /// <param name="region">Region to block.</param>
        public void AddBlockedRegion(Region region)
        {
            _blockedRegions.Add(region);
        }

        /// <summary>
        /// Removes a region from the blocked list.
        /// </summary>
        /// <param name="region">Region to unblock.</param>
        public void RemoveBlockedRegion(Region region)
        {
            _blockedRegions.Remove(region);
        }

        /// <summary>
        /// Checks whether the player is currently in any blocked region.
        /// </summary>
        /// <param name="areaNameSystem">Area system used to read active player regions.</param>
        /// <returns><see langword="true"/> when any current region is blocked; otherwise <see langword="false"/>.</returns>
        public bool IsPlayerInBlockedRegion(AreaNameSystem? areaNameSystem)
        {
            if (areaNameSystem == null)
            {
                return false;
            }

            return areaNameSystem.Regions.Any(region => _blockedRegions.Contains(region));
        }

        /// <summary>
        /// Starts and stops region-scoped runtime events based on enter/leave transitions.
        /// Applies a cooldown to regions where at least one event started.
        /// </summary>
        private void HandleRunningEvents(Region region, bool hasEntered)
        {
            if (hasEntered && RegionalEventRegistry.TryGetEvents(region, out List<ARegionalEvent>? regionalEvents) && regionalEvents != null)
            {
                if (IsRegionOnCooldown(region))
                {
                    return;
                }

                bool startedEvent = false;
                if (regionalEvents.Count > 0)
                {
                    ARegionalEvent randomEvent = regionalEvents[UnityEngine.Random.Range(0, regionalEvents.Count)];
                    if (!randomEvent.IsRunning)
                    {
                        randomEvent.StartEvent();
                        runningRegionalEvents.Add(randomEvent);
                        startedEvent = true;
                    }
                }

                for (int index = 0; index < regionalEvents.Count; index++)
                {
                    ARegionalEvent regionalEvent = regionalEvents[index];
                    if (!regionalEvent.IsRunning && regionalEvent.CanRunParallel())
                    {
                        regionalEvent.StartEvent();
                        runningRegionalEvents.Add(regionalEvent);
                        startedEvent = true;
                    }
                }

                if (startedEvent)
                {
                    float regionalCooldownSeconds = getRegionalCooldownSeconds();
                    if (regionalCooldownSeconds > 0f)
                    {
                        SetRegionCooldown(region, regionalCooldownSeconds);
                    }
                }
            }
            else if (RegionalEventRegistry.ContainsRegion(region))
            {
                for (int index = runningRegionalEvents.Count - 1; index >= 0; index--)
                {
                    ARegionalEvent runningEvent = runningRegionalEvents[index];
                    if (runningEvent.Region != region)
                    {
                        continue;
                    }

                    runningEvent.EndEvent();
                    runningRegionalEvents.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Dispatches enter/leave notifications to all registered regional events for the region.
        /// </summary>
        private static void HandleRegionChange(Region region, bool hasEntered)
        {
            if (hasEntered && RegionalEventRegistry.TryGetEvents(region, out List<ARegionalEvent>? enteredEvents) && enteredEvents != null)
            {
                for (int index = 0; index < enteredEvents.Count; index++)
                {
                    ARegionalEvent regionalEvent = enteredEvents[index];
                    regionalEvent.OnRegionEntered();
                }

                return;
            }

            if (!hasEntered && RegionalEventRegistry.TryGetEvents(region, out List<ARegionalEvent>? leftEvents) && leftEvents != null)
            {
                for (int index = 0; index < leftEvents.Count; index++)
                {
                    ARegionalEvent regionalEvent = leftEvents[index];
                    regionalEvent.OnRegionLeft();
                }
            }
        }

        /// <summary>
        /// Returns whether the region is still inside its cooldown window.
        /// </summary>
        private bool IsRegionOnCooldown(Region region)
        {
            return _regionCooldowns.TryGetValue(region, out float cooldownEnd) && Time.realtimeSinceStartup < cooldownEnd;
        }

        /// <summary>
        /// Sets or refreshes the cooldown end timestamp for a region.
        /// </summary>
        private void SetRegionCooldown(Region region, float cooldownInSeconds)
        {
            _regionCooldowns[region] = Time.realtimeSinceStartup + cooldownInSeconds;
        }
    }
}

