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

        // Who is in each region: the local player (LocalKey) and remote players by their id. A region's events start
        // when its first occupant enters and end when its last one leaves, so a co-op teammate alone in a region gets
        // the ambush too, and two players in one region share one.
        private const int LocalKey = -1;
        private readonly Dictionary<Region, List<int>> _occupants = [];
        private readonly Dictionary<int, (HashSet<Region> Regions, Func<Vector2?> Position)> _remotes = [];
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
            Change(LocalKey, region, hasEntered);
        }

        /// <summary>
        /// A remote player (a co-op teammate, on the machine that owns the world) entered or left a region.
        /// </summary>
        /// <param name="player">The teammate's id; anything but -1.</param>
        /// <param name="region">The region.</param>
        /// <param name="hasEntered"><see langword="true"/> when entering; <see langword="false"/> when leaving.</param>
        /// <param name="position">Where the teammate stands now, or null when unknown - what their ambush spawns around.</param>
        public void OnRemoteRegionChanged(int player, Region region, bool hasEntered, Func<Vector2?> position)
        {
            if (player == LocalKey) return;

            if (!_remotes.TryGetValue(player, out (HashSet<Region> Regions, Func<Vector2?> Position) remote))
            {
                remote = ([], position);
                _remotes[player] = remote;
            }

            if (hasEntered) remote.Regions.Add(region);
            else remote.Regions.Remove(region);

            Change(player, region, hasEntered);
        }

        /// <summary>A remote player left: out of every region they were in.</summary>
        /// <param name="player">The teammate's id.</param>
        public void ForgetRemote(int player)
        {
            if (!_remotes.Remove(player, out (HashSet<Region> Regions, Func<Vector2?> Position) remote)) return;

            foreach (Region region in remote.Regions.ToList())
            {
                Change(player, region, false);
            }
        }

        private void Change(int key, Region region, bool hasEntered)
        {
            if (!_occupants.TryGetValue(region, out List<int> inside))
            {
                inside = [];
                _occupants[region] = inside;
            }

            if (hasEntered)
            {
                // The local player re-entering a region nobody else holds is a fresh entry even if no leave was seen
                // (a world reload); anyone else already inside means the region's events are running for the party.
                if (inside.Contains(key))
                {
                    if (key != LocalKey || inside.Count > 1) return;
                }
                else
                {
                    inside.Add(key);
                    if (inside.Count > 1) return;
                }

                Anchor(region, key);
                HandleRegionChange(region, true);
                HandleRunningEvents(region, true);
                return;
            }

            if (!inside.Remove(key)) return;

            // Somebody is still there: the region's events go on, now around them.
            if (inside.Count > 0)
            {
                Anchor(region, inside[0]);
                return;
            }

            HandleRegionChange(region, false);
            HandleRunningEvents(region, false);
        }

        /// <summary>Points the region's events at the occupant they are for.</summary>
        private void Anchor(Region region, int key)
        {
            if (!RegionalEventRegistry.TryGetEvents(region, out List<ARegionalEvent>? events)) return;

            Func<Vector2?>? position = key == LocalKey ? null : _remotes[key].Position;
            Func<bool>? blocked = key == LocalKey ? null : () => _remotes.TryGetValue(key, out (HashSet<Region> Regions, Func<Vector2?> Position) remote) && remote.Regions.Any(_blockedRegions.Contains);

            foreach (ARegionalEvent regionalEvent in events)
            {
                regionalEvent.AnchorPosition = position;
                regionalEvent.AnchorBlocked = blocked;
            }
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
            if (hasEntered && RegionalEventRegistry.TryGetEvents(region, out List<ARegionalEvent>? regionalEvents))
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
                        // Only one that is still running after its start counts: an event may end itself on the spot (a
                        // skip roll, a co-op client that does not own the world), and counting that one put its region on
                        // cooldown and ended it again on leave - stamping the event's own cooldown it had just declined.
                        randomEvent.StartEvent();
                        if (randomEvent.IsRunning)
                        {
                            runningRegionalEvents.Add(randomEvent);
                            startedEvent = true;
                        }
                    }
                }

                for (int index = 0; index < regionalEvents.Count; index++)
                {
                    ARegionalEvent regionalEvent = regionalEvents[index];
                    if (!regionalEvent.IsRunning && regionalEvent.CanRunParallel())
                    {
                        regionalEvent.StartEvent();
                        if (regionalEvent.IsRunning)
                        {
                            runningRegionalEvents.Add(regionalEvent);
                            startedEvent = true;
                        }
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

                    // One that already ended itself (a spawn that could not happen) is not ended twice.
                    if (runningEvent.IsRunning) runningEvent.EndEvent();
                    runningRegionalEvents.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Dispatches enter/leave notifications to all registered regional events for the region.
        /// </summary>
        private static void HandleRegionChange(Region region, bool hasEntered)
        {
            if (hasEntered && RegionalEventRegistry.TryGetEvents(region, out List<ARegionalEvent>? enteredEvents))
            {
                for (int index = 0; index < enteredEvents.Count; index++)
                {
                    ARegionalEvent regionalEvent = enteredEvents[index];
                    regionalEvent.OnRegionEntered();
                }

                return;
            }

            if (!hasEntered && RegionalEventRegistry.TryGetEvents(region, out List<ARegionalEvent>? leftEvents))
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

