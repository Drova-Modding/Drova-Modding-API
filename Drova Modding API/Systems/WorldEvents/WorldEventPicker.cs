namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// Picks world events without immediate repetition by drawing from a temporary bag of indices.
    /// The bag is rebuilt when empty or when the source event count changes.
    /// </summary>
    internal sealed class WorldEventPicker
    {
        private readonly List<int> _worldEventBag = [];
        private int _worldEventBagVersion = -1;
        private IWorldEvent? _lastWorldEvent;

        /// <summary>
        /// Picks one event from <paramref name="worldEvents"/>.
        /// Returns <see langword="null"/> when no events are available.
        /// </summary>
        /// <param name="worldEvents">The currently eligible events.</param>
        /// <returns>A selected event, or <see langword="null"/> if none exist.</returns>
        public IWorldEvent? Pick(List<IWorldEvent> worldEvents)
        {
            int worldEventCount = worldEvents.Count;
            if (worldEventCount == 0)
            {
                return null;
            }

            if (worldEventCount == 1)
            {
                _lastWorldEvent = worldEvents[0];
                return _lastWorldEvent;
            }

            if (_worldEventBag.Count == 0 || _worldEventBagVersion != worldEventCount)
            {
                RebuildBag(worldEventCount);
            }

            int selectedPosition = UnityEngine.Random.Range(0, _worldEventBag.Count);
            int selectedIndex = _worldEventBag[selectedPosition];
            IWorldEvent selectedEvent = worldEvents[selectedIndex];

            if (_worldEventBag.Count > 1 && ReferenceEquals(selectedEvent, _lastWorldEvent))
            {
                int alternativePosition = UnityEngine.Random.Range(0, _worldEventBag.Count - 1);
                if (alternativePosition >= selectedPosition)
                {
                    alternativePosition++;
                }

                selectedPosition = alternativePosition;
                selectedIndex = _worldEventBag[selectedPosition];
                selectedEvent = worldEvents[selectedIndex];
            }

            _worldEventBag.RemoveAt(selectedPosition);
            _lastWorldEvent = selectedEvent;
            return selectedEvent;
        }

        /// <summary>
        /// Rebuilds the internal index bag to contain each event index exactly once.
        /// </summary>
        /// <param name="worldEventCount">Number of available events in the source list.</param>
        private void RebuildBag(int worldEventCount)
        {
            _worldEventBag.Clear();
            for (int index = 0; index < worldEventCount; index++)
            {
                _worldEventBag.Add(index);
            }

            _worldEventBagVersion = worldEventCount;
        }
    }
}