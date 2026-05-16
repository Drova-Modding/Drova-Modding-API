using Drova_Modding_API.Access;
using Drova_Modding_API.Extensions;
using Drova_Modding_API.Systems.WorldEvents.Regional;
using Il2CppDrova;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// A System that handles spawn Events for classes which implements <see cref="IWorldEvent"/>, or <see cref="ARegionalEvent"/>.
    /// The system will handle the cooldown and the start of the events.
    /// You can register your Events with <see cref="RegisterWorldEvents(IEnumerable{IWorldEvent})"/> and <see cref="RegisterWorldEvent(IWorldEvent)"/>.
    /// You can register your Regional Events with <see cref="RegisterRegionalEvents(IEnumerable{ARegionalEvent})"/> and <see cref="RegisterRegionalEvent(ARegionalEvent)"/>.
    /// It will kill automatically Events which are running more than 10 times the cooldown.
    /// It will also block when the Player is in a Dialogue, Option menu, or in a blocked region
    /// </summary>
    [RegisterTypeInIl2Cpp]
    public class WorldEventSystemManager(IntPtr ptr) : MonoBehaviour(ptr)
    {

        private static readonly List<IWorldEvent> WorldEvents = [];
        private static readonly Dictionary<Region, List<ARegionalEvent>> RegionEvents = [];
        private readonly List<ARegionalEvent> _runningRegionalEvents = [];
        private short _skipOnEvents = 0;
        private object? _cooldownCoroutine;
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
        internal AreaNameSystem AreaNameSystem;

        /**
         * The instance of the WorldEventSystemManager.
         */
        public static WorldEventSystemManager? Instance { get; private set; }

        /**
         * The current event that is running.
         */
        [HideFromIl2Cpp]
        public IWorldEvent? CurrentEvent { get; private set; }

        /**
         * The regional events that are currently running.
         */
        [HideFromIl2Cpp]
        public IReadOnlyList<ARegionalEvent> RegionalEvents => _runningRegionalEvents;

        /// <summary>
        /// Start a new world event.
        /// </summary>
        [HideFromIl2Cpp]
        public void StartEvent(IWorldEvent worldEvent)
        {
            if (CurrentEvent != null)
            {
                MelonLogger.Warning("Tried to start a new event while another event is running.");
                return;
            }
            if (!worldEvent.CanStartEvent())
            {
                MelonLogger.Warning("Tried to start an event that can't start.");
                return;
            }

            CurrentEvent = worldEvent;
            CurrentEvent.StartEvent();
        }

        internal void Awake()
        {
            Instance = this;
            _cooldownCoroutine = MelonCoroutines.Start(Instance.StartEventCooldown());
            AreaNameSystem.OnRegionChanged += OnRegionListener;
        }

        internal void OnDestroy()
        {
            if (AreaNameSystem != null)
                AreaNameSystem.OnRegionChanged -= OnRegionListener;
            if(_cooldownCoroutine != null)
                MelonCoroutines.Stop(_cooldownCoroutine);
        }

        [HideFromIl2Cpp]
        private void HandleRunningEvents(Region region, bool hasEntered)
        {
            if (hasEntered && RegionEvents.TryGetValue(region, out List<ARegionalEvent>? regionalEvents) && regionalEvents != null)
            {
                if (regionalEvents.Count > 0)
                {
                    ARegionalEvent randomEvent = regionalEvents[UnityEngine.Random.Range(0, regionalEvents.Count)];
                    randomEvent.StartEvent();
                    _runningRegionalEvents.Add(randomEvent);
                }
                for (int index = 0; index < regionalEvents.Count; index++)
                {
                    ARegionalEvent regionalEvent = regionalEvents[index];
                    if (!regionalEvent.IsRunning && regionalEvent.CanRunParallel())
                    {
                        regionalEvent.StartEvent();
                        _runningRegionalEvents.Add(regionalEvent);
                    }
                }

            }
            else if (RegionEvents.ContainsKey(region))
            {
                foreach (ARegionalEvent runningEvent in _runningRegionalEvents)
                {
                    if (runningEvent.Region == region)
                    {
                        runningEvent.EndEvent();
                    }
                }
                _runningRegionalEvents.Clear();
            }
        }

        [HideFromIl2Cpp]
        private static void HandleRegionChange(Region region, bool hasEntered)
        {
            if (hasEntered && RegionEvents.TryGetValue(region, out List<ARegionalEvent>? regionalEvents1) && regionalEvents1 != null)
            {
                for (int index = 0; index < regionalEvents1.Count; index++)
                {
                    ARegionalEvent regionalEvent = regionalEvents1[index];
                    regionalEvent.OnRegionEntered();
                }
            }
            else if (!hasEntered && RegionEvents.TryGetValue(region, out List<ARegionalEvent>? regionalEvents) && regionalEvents != null)
            {
                for (int index = 0; index < regionalEvents.Count; index++)
                {
                    ARegionalEvent regionalEvent = regionalEvents[index];
                    regionalEvent.OnRegionLeft();
                }
            }
        }

        /// <summary>
        /// Add a region where world events are blocked.
        /// </summary>
        [HideFromIl2Cpp]
        public void AddBlockedRegion(Region region)
        {
            _blockedRegions.Add(region);
        }

        /// <summary>
        /// Remove a region from the blocked list.
        /// </summary>
        [HideFromIl2Cpp]
        public void RemoveBlockedRegion(Region region)
        {
            _blockedRegions.Remove(region);
        }

        /// <summary>
        /// Refreshes the Cooldown of the Event timer
        /// </summary>
        [HideFromIl2Cpp]
        internal void RefreshCooldown()
        {
            if (_cooldownCoroutine != null)
            {
                MelonCoroutines.Stop(_cooldownCoroutine);
            }
            _cooldownCoroutine = MelonCoroutines.Start(StartEventCooldown());
        }

        [HideFromIl2Cpp]
        internal IEnumerator StartEventCooldown()
        {
            while (true)
            {
                if (OptionMenuAccess.Instance?.IsMenuOpen == true || IsPlayerInDialogueOrTeleporting())
                {
                    yield return new WaitForSeconds(1);
                    continue;
                }

                bool optionFoundForMin = ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.ModdingUIMinOptionKey, out int min);
                bool optionFoundForMax = ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.ModdingUIMaxOptionKey, out int max);
                if (optionFoundForMin && optionFoundForMax)
                {
#if DEBUG
                    yield return new WaitForSeconds(UnityEngine.Random.Range(min * 30, max * 30));
#else
                    yield return new WaitForSeconds(UnityEngine.Random.Range(min * 60, max * 60));

#endif
                }
                else
                {
                    yield return new WaitForSeconds(1);
                }

                if (CurrentEvent == null && WorldEvents.Count > 0 && !IsPlayerInBlockedRegion())
                {
                    _skipOnEvents = 0;
                    IWorldEvent worldEvent = WorldEvents[UnityEngine.Random.Range(0, WorldEvents.Count)];
                    StartEvent(worldEvent);
                }
                else
                {
                    _skipOnEvents++;
                }
                if (_skipOnEvents > 10 && CurrentEvent != null)
                {
                    EndEvent();
                }
            }
        }

        /**
         * Register a world event.
         */
        public static void RegisterWorldEvent(IWorldEvent worldEvent)
        {
            WorldEvents.Add(worldEvent);
        }

        /**
         * Register multiple world events.
         */
        public static void RegisterWorldEvents(IEnumerable<IWorldEvent> worldEvents)
        {
            WorldEvents.AddRange(worldEvents);
        }

        /**
         * Register a regional event.
         */
        public static void RegisterRegionalEvent(ARegionalEvent regionalEvent)
        {
            if (RegionEvents.TryGetValue(regionalEvent.Region, out List<ARegionalEvent>? events)  && events != null)
            {
                events.Add(regionalEvent);
            }
            else
            {
                RegionEvents[regionalEvent.Region] = [regionalEvent];
            }
        }

        /**
         * Register multiple regional events.
         */
        public static void RegisterRegionalEvents(IEnumerable<ARegionalEvent> regionalEvents)
        {
            foreach (ARegionalEvent regionalEvent in regionalEvents)
            {
                RegisterRegionalEvent(regionalEvent);
            }
        }

        /// <summary>
        /// End the current world event.
        /// </summary>
        public void EndEvent()
        {
            if (CurrentEvent == null)
            {
                MelonLogger.Warning("Tried to end an event while no event is running.");
                return;
            }

            CurrentEvent.EndEvent();
            CurrentEvent = null;
        }

        /// <summary>
        /// Returns true if the player is alive and is currently in a dialogue or teleporting.
        /// </summary>
        /// <returns>True if the player is alive and is in a dialogue or teleporting; otherwise, false.</returns>
        [HideFromIl2Cpp]
        public static bool IsPlayerInDialogueOrTeleporting()
        {
            if (PlayerAccess.TryGetPlayer(out var player) && player.IsAlive())
            {
                return player.IsInDialogue() || player.IsPlayerTeleporting();
            }
            return false;
        }

        /// <summary>
        /// Gives back whether the Player is in one of the blocked regions for spawning world events.
        /// </summary>
        /// <returns>True if the player is in a blocked region</returns>
        [HideFromIl2Cpp]
        public bool IsPlayerInBlockedRegion()
        {
            if (AreaNameSystem == null) return false;
            return AreaNameSystem.Regions.Any(r => _blockedRegions.Contains(r));
        }

        [HideFromIl2Cpp]
        private void OnRegionListener(Region region, bool hasEntered)
        {
            HandleRegionChange(region, hasEntered);
            HandleRunningEvents(region, hasEntered);
        }

    }
}
