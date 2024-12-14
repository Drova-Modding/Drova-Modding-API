using Il2CppDrova;

namespace Drova_Modding_API.Systems.DebugUtils
{
    /// <summary>
    /// Manager for the Debug Build
    /// </summary>
    public static class DebugManager
    {
#if DEBUG
        /// <summary>
        /// Delegate for when an NPC is selected
        /// </summary>
        /// <param name="actor">The Npc</param>
        public delegate void NpcSelected(Actor actor);
        /// <summary>
        /// Event that is triggered when an NPC is selected
        /// </summary>
        public static event NpcSelected OnNpcSelected;

        /// <summary>
        /// The last NPC that was invoked
        /// </summary>
        private static Actor _lastInvoked;

        public static bool AllowNpcSelection { get; set; } = true;

        /// <summary>
        /// Trigger the event for when an NPC is selected
        /// </summary>
        /// <param name="actor">Npc to trigger</param>
        internal static void TriggerNpcSelected(Actor actor)
        {
            if (!AllowNpcSelection)
            {
                return;
            }
            if (_lastInvoked == actor)
            {
                return;
            }
            _lastInvoked = actor;
            OnNpcSelected?.Invoke(actor);
        }
#endif
    }
}
