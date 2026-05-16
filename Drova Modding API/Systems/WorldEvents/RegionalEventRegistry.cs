using Drova_Modding_API.Systems.WorldEvents.Regional;

namespace Drova_Modding_API.Systems.WorldEvents
{
    internal static class RegionalEventRegistry
    {
        private static readonly Dictionary<Region, List<ARegionalEvent>> RegionEvents = [];

        public static bool TryGetEvents(Region region, out List<ARegionalEvent>? regionalEvents)
        {
            return RegionEvents.TryGetValue(region, out regionalEvents);
        }

        public static bool ContainsRegion(Region region)
        {
            return RegionEvents.ContainsKey(region);
        }

        public static void RegisterRegionalEvent(ARegionalEvent regionalEvent)
        {
            if (RegionEvents.TryGetValue(regionalEvent.Region, out List<ARegionalEvent>? events) && events != null)
            {
                events.Add(regionalEvent);
                return;
            }

            RegionEvents[regionalEvent.Region] = [regionalEvent];
        }
    }
}

