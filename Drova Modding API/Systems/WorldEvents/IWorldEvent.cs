namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// Base interface for all world events.
    /// </summary>
    public interface IWorldEvent
    {
        /**
         * Called when the event starts.
         */
        void StartEvent();
        /**
         * Called when the event ends.
         */
        void EndEvent();

        /**
         * Check if the event can start.
         */
        bool CanStartEvent() => true;
    }
}
