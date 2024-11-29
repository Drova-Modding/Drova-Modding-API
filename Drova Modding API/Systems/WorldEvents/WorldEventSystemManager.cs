using UnityEngine;
using MelonLoader;
using Drova_Modding_API.Access;
using System.Collections;
using Drova_Modding_API.Systems.WorldEvents.Regional;
using Il2CppInterop.Runtime.Attributes;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// A System which handle spawn Events for classes which implements <see cref="IWorldEvent"/>. or <see cref="ARegionalEvent"/>"/>
    /// The system will handle the cooldown and the start of the events.
    /// You can register your Events with <see cref="RegisterWorldEvents(IEnumerable{IWorldEvent})"/> and <see cref="RegisterWorldEvent(IWorldEvent)"/>.
    /// You can register your Regional Events with <see cref="RegisterRegionalEvents(IEnumerable{ARegionalEvent})"/> and <see cref="RegisterRegionalEvent(ARegionalEvent)"/>.
    /// It will kill automatically Events which are running more than 10 times the cooldown.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    public class WorldEventSystemManager(IntPtr ptr) : MonoBehaviour(ptr)
    {

        private static WorldEventSystemManager _instance;
        private static readonly List<IWorldEvent> _worldEvents = [];
        private static readonly Dictionary<Region, List<ARegionalEvent>> _regionEventDic = [];
        private readonly List<ARegionalEvent> _runningRegionalEvents = [];
        private short _skipOnEvents = 0;
        internal AreaNameSystem areaNameSystem;

        /**
         * The instance of the WorldEventSystemManager.
         */
        public static WorldEventSystemManager Instance
        {
            get
            {

                return _instance;
            }
        }

        /**
         * The current event that is running.
         */
        [HideFromIl2Cpp]
        public IWorldEvent CurrentEvent { get; private set; }

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

        private void Awake()
        {
            _instance = this;
            MelonCoroutines.Start(_instance.StartEventCooldown());
            areaNameSystem.OnRegionChanged += OnRegionListener;
        }

        private void OnDestroy()
        {
            if (areaNameSystem != null)
                areaNameSystem.OnRegionChanged -= OnRegionListener;
        }

        private void OnRegionListener(Region region, bool hasEntered)
        {
            HandleRegionChange(region, hasEntered);
            HandleRunningEvents(region, hasEntered);
        }

        private void HandleRunningEvents(Region region, bool hasEntered)
        {
            if (hasEntered && _regionEventDic.ContainsKey(region))
            {
                var regionalEvents = _regionEventDic[region];
                if (regionalEvents.Count > 0)
                {
                    var randomEvent = regionalEvents[UnityEngine.Random.Range(0, regionalEvents.Count)];
                    randomEvent.StartEvent();
                    _runningRegionalEvents.Add(randomEvent);
                }
                foreach (var regionalEvent in regionalEvents)
                {
                    if (!regionalEvent.IsRunning && regionalEvent.CanRunParallel())
                    {
                        regionalEvent.StartEvent();
                        _runningRegionalEvents.Add(regionalEvent);
                    }
                }

            }
            else if (_regionEventDic.ContainsKey(region))
            {
                foreach (var runningEvent in _runningRegionalEvents)
                {
                    if (runningEvent.Region == region)
                    {
                        runningEvent.EndEvent();
                    }
                }
                _runningRegionalEvents.Clear();
            }
        }

        private static void HandleRegionChange(Region region, bool hasEntered)
        {
            if (hasEntered && _regionEventDic.ContainsKey(region))
            {
                var regionalEvents = _regionEventDic[region];
                foreach (var regionalEvent in regionalEvents)
                {
                    regionalEvent.OnRegionEntered();
                }
            }
            else if (!hasEntered && _regionEventDic.ContainsKey(region))
            {
                var regionalEvents = _regionEventDic[region];
                foreach (var regionalEvent in regionalEvents)
                {
                    regionalEvent.OnRegionLeft();
                }
            }
        }

        [HideFromIl2Cpp]
        internal IEnumerator StartEventCooldown()
        {
            while (true)
            {
                var optionFoundForMin = ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.ModdingUIMinOptionKey, out int min);
                var optionFoundForMax = ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.ModdingUIMaxOptionKey, out int max);
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

                if (CurrentEvent == null && _worldEvents.Count > 0)
                {
                    _skipOnEvents = 0;
                    var worldEvent = _worldEvents[UnityEngine.Random.Range(0, _worldEvents.Count)];
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
            _worldEvents.Add(worldEvent);
        }

        /**
         * Register multiple world events.
         */
        public static void RegisterWorldEvents(IEnumerable<IWorldEvent> worldEvents)
        {
            _worldEvents.AddRange(worldEvents);
        }

        /**
         * Register a regional event.
         */
        public static void RegisterRegionalEvent(ARegionalEvent regionalEvent)
        {
            if (_regionEventDic.ContainsKey(regionalEvent.Region))
            {
                _regionEventDic[regionalEvent.Region].Add(regionalEvent);
            }
            else
            {
                _regionEventDic[regionalEvent.Region] = [regionalEvent];
            }
        }

        /**
         * Register multiple regional events.
         */
        public static void RegisterRegionalEvents(IEnumerable<ARegionalEvent> regionalEvents)
        {
            foreach (var regionalEvent in regionalEvents)
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
    }
}
