using MelonLoader;
using System.Collections;
using UnityEngine;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// A timed event that will start and end after a certain amount of time.
    /// </summary>
    /// <remarks>
    /// Creates a new timed event.
    /// </remarks>
    /// <param name="duration">The duration of the Event</param>
    public class TimedEvent(float duration) : IWorldEvent
    {
        /**
         * The duration of the event.
         */
        public float Duration { get; private set; } = duration;
        /**
         * The WaitForSeconds object for the event.
         */
        public WaitForSeconds Wait { get; private set; } = new WaitForSeconds(duration);

        private object _endEvent;

        /// <summary>
        /// Ends the event.
        /// </summary>
        public void EndEvent()
        {
            MelonCoroutines.Stop(_endEvent);
        }

        /// <summary>
        /// Starts the countdown for the event.
        /// </summary>
        public virtual void StartEvent()
        {
             _endEvent = MelonCoroutines.Start(RunEvent());
        }

        /// <summary>
        /// Coroutine that runs the event and ends it after the duration.
        /// </summary>
        public virtual IEnumerator RunEvent()
        {
            yield return Wait;
            EndEvent();
        }
    }
}
