namespace Drova_Modding_API.Systems.WorldEvents.Regional
{
    /// <summary>
    /// A regional event that will start when a player enters a region.
    /// </summary>
    /// <param name="regionToTrigger">The region to trigger the event</param>
    public abstract class ARegionalEvent(Region regionToTrigger): IWorldEvent
    {
        /**
         * If the event is currently running.
         */
        public bool IsRunning { get; private set; } = false;
        /**
         * The region to trigger the event.
         */
        public Region Region { get; private set; } = regionToTrigger;

        /**
         * Called when the player enters the region.
         */
        public abstract void OnRegionEntered();

        /**
         * Called when the player leaves the region.
         */
        public abstract void OnRegionLeft();

        /**
         * Called when the event starts in the region, override this method to allow your event to run in parallel.
         */
        public virtual bool CanRunParallel() => false;

        /// <inheritdoc/>
        public virtual void StartEvent()
        {
            this.IsRunning = true;
        }
        
        /// <inheritdoc/>
        public virtual void EndEvent()
        {
            this.IsRunning = false;
        }
    }
}