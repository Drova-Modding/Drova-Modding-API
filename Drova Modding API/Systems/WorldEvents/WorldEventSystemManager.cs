using UnityEngine;
using MelonLoader;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// Handles the world events in the game.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    public class WorldEventSystemManager(IntPtr ptr) : MonoBehaviour(ptr)
    {

        private IWorldEvent _currentEvent;

        /// <summary>
        /// Start a new world event.
        /// </summary>
        public void StartEvent(IWorldEvent worldEvent)
        {
            if (_currentEvent != null)
            {
                MelonLogger.Warning("Tried to start a new event while another event is running.");
                return;
            }
            if(!worldEvent.CanStartEvent())
            {
                MelonLogger.Warning("Tried to start an event that can't start.");
                return;
            }

            _currentEvent = worldEvent;
            _currentEvent.StartEvent();
        }

        /// <summary>
        /// End the current world event.
        /// </summary>
        public void EndEvent()
        {
            if (_currentEvent == null)
            {
                MelonLogger.Warning("Tried to end an event while no event is running.");
                return;
            }

            _currentEvent.EndEvent();
            _currentEvent = null;
        }
    }
}
