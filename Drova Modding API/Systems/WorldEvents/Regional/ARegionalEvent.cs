namespace Drova_Modding_API.Systems.WorldEvents.Regional
{
    /// <summary>
    /// A regional event that will start when a player enters a region.
    /// </summary>
    /// <param name="regionToTrigger">The region to trigger the event</param>
    public abstract class ARegionalEvent(Region regionToTrigger) : IWorldEvent
    {
        /**
         * If the event is currently running.
         */
        public bool IsRunning { get; private set; }
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

        // Set by the region coordinator when a remote player (a co-op teammate) brought the event in; null for the local player.
        internal Func<UnityEngine.Vector2?>? AnchorPosition { get; set; }
        internal Func<bool>? AnchorBlocked { get; set; }

        /// <summary>Whether the player this event is for is a remote one (a co-op teammate), not the local player.</summary>
        public bool IsAnchorRemote => AnchorPosition != null;

        /// <summary>
        /// Where the event's creatures belong: the local player, or the co-op teammate alone in the region who brought it in.
        /// </summary>
        /// <returns>False when it is not known (no player yet, or the teammate's body is gone).</returns>
        public bool TryGetAnchor(out UnityEngine.Vector2 position)
        {
            if (AnchorPosition != null)
            {
                UnityEngine.Vector2? remote = AnchorPosition();
                position = remote ?? default;
                return remote.HasValue;
            }

            if (Access.PlayerAccess.TryGetPlayer(out Il2CppDrova.Actor? player))
            {
                position = player.transform.position;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>Whether the player this event is for stands in a blocked region (a town, a camp).</summary>
        public bool IsAnchorInBlockedRegion()
        {
            return AnchorBlocked?.Invoke() ?? WorldEventSystemManager.Instance?.IsPlayerInBlockedRegion() == true;
        }

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