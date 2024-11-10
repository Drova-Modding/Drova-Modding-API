using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Drova_Modding_API.Systems.WorldEvents
{

    /// <summary>
    /// A persistent encounter event that will spawn a list of encounters that will not despawn even when the event ends.
    /// If you don't want to persist the encounters, use <see cref="EncounterEvent"/> instead.
    /// </summary>
    /// <param name="persitentEncounters">The list to spawn</param>
    public class PersistentEncounterEvent(List<LazyActorCreator.LazyActorParams> persitentEncounters) : IWorldEvent
    {
        /// <inheritdoc/>
        public void EndEvent()
        {
            // Do nothing
        }

        /// <inheritdoc/>
        public void StartEvent()
        {
            foreach (var encounter in persitentEncounters)
            {
                LazyActorCreator.CreateLazyActorCreature(encounter);
            }
            WorldEventSystemManager.Instance.EndEvent();
        }
    }
}
