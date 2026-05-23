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
    /// It will kill automatically Events which are running more than 5 times the cooldown.
    /// It will also block when the Player is in a Dialogue, Option menu, or in a blocked region
    /// </summary>
    [RegisterTypeInIl2Cpp]
    public class WorldEventSystemManager(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private static readonly List<IWorldEvent> WorldEvents = [];
        private const int DefaultRegionalCooldownMinutes = 1;
        private readonly List<ARegionalEvent> _runningRegionalEvents = [];
        private readonly WorldEventPicker _worldEventPicker = new();
        private WorldRegionCoordinator? _regionCoordinator;
        private short _skipOnEvents;
        private object? _cooldownCoroutine;
        private bool _cooldownRunning;
        internal AreaNameSystem AreaNameSystem;
        private readonly WaitForSeconds _startEventCooldown = new(1);

        /**
         * The instance of the WorldEventSystemManager.
         */
        [HideFromIl2Cpp]
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
            _cooldownRunning = true;
            _regionCoordinator = new WorldRegionCoordinator(_runningRegionalEvents, GetRegionalCooldownSeconds);
            _cooldownCoroutine = MelonCoroutines.Start(Instance.StartEventCooldown());
            AreaNameSystem.OnRegionChanged += _regionCoordinator.OnRegionChanged;
        }

        internal void OnDestroy()
        {
            _cooldownRunning = false;
            Instance = null;
            if (AreaNameSystem != null && _regionCoordinator != null)
                AreaNameSystem.OnRegionChanged -= _regionCoordinator.OnRegionChanged;
            if(_cooldownCoroutine != null)
                MelonCoroutines.Stop(_cooldownCoroutine);
        }

        [HideFromIl2Cpp]
        private static float GetRegionalCooldownSeconds()
        {
            if (ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.RegionalEventCooldownOptionKey, out int cooldownInMinutes))
            {
                return Mathf.Max(0, cooldownInMinutes) * 60f;
            }

            return DefaultRegionalCooldownMinutes * 60f;
        }

        /// <summary>
        /// Add a region where world events are blocked.
        /// </summary>
        [HideFromIl2Cpp]
        public void AddBlockedRegion(Region region)
        {
            _regionCoordinator?.AddBlockedRegion(region);
        }

        /// <summary>
        /// Remove a region from the blocked list.
        /// </summary>
        [HideFromIl2Cpp]
        public void RemoveBlockedRegion(Region region)
        {
            _regionCoordinator?.RemoveBlockedRegion(region);
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
            while (_cooldownRunning)
            {
                
                if (OptionMenuAccess.Instance.IsMenuOpen || IsPlayerInDialogueOrTeleporting())
                {
                    yield return _startEventCooldown;
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
                    yield return _startEventCooldown;
                }

                if (CurrentEvent == null && WorldEvents.Count > 0 && !IsPlayerInBlockedRegion())
                {
                    _skipOnEvents = 0;
                    IWorldEvent? worldEvent = PickNextWorldEvent();
                    if (worldEvent != null)
                    {
                        StartEvent(worldEvent);
                    }
                }
                else
                {
                    _skipOnEvents++;
                }
                if (_skipOnEvents > 5 && CurrentEvent != null)
                {
                    EndEvent();
                }
            }
        }

        [HideFromIl2Cpp]
        private IWorldEvent? PickNextWorldEvent()
        {
            return _worldEventPicker.Pick(WorldEvents);
        }

        /**
         * Register a world event.
         */
        [HideFromIl2Cpp]
        public static void RegisterWorldEvent(IWorldEvent worldEvent)
        {
            WorldEvents.Add(worldEvent);
        }

        /**
         * Register multiple world events.
         */
        [HideFromIl2Cpp]
        public static void RegisterWorldEvents(IEnumerable<IWorldEvent> worldEvents)
        {
            WorldEvents.AddRange(worldEvents);
        }

        /**
         * Register a regional event.
         */
        [HideFromIl2Cpp]
        public static void RegisterRegionalEvent(ARegionalEvent regionalEvent)
        {
            RegionalEventRegistry.RegisterRegionalEvent(regionalEvent);
        }

        /**
         * Register multiple regional events.
         */
        [HideFromIl2Cpp]
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
        [HideFromIl2Cpp]
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
            return _regionCoordinator?.IsPlayerInBlockedRegion(AreaNameSystem) == true;
        }

    }
}
