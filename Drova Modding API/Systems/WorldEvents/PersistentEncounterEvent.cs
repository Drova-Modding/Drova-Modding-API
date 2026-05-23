using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.SaveGame.Store;
using Drova_Modding_API.Systems.Spawning;

namespace Drova_Modding_API.Systems.WorldEvents
{

    /// <summary>
    /// A persistent encounter event that will spawn a list of encounters that will not despawn even when the event ends.
    /// If you don't want to persist the encounters, use <see cref="EncounterEvent"/> instead.
    /// </summary>
    /// <param name="persistentEncounters">The list to spawn</param>
    public class PersistentEncounterEvent(List<LazyActorCreator.LazyActorParams> persistentEncounters) : IWorldEvent
    {
        /**
         * The store for the lazy actors
         */
        protected IStore<LazyActorSaveData>? LazyActorStore;

        /// <inheritdoc/>
        public virtual void EndEvent()
        {
            // Do nothing
        }

        /// <inheritdoc/>
        public virtual void StartEvent()
        {
            LazyActorStore ??= SaveGameSystem.Instance.GetStore<LazyActorSaveData>();
            for (int i = 0; i < persistentEncounters.Count; i++)
            {
                LazyActorCreator.LazyActorParams encounter = persistentEncounters[i];
                Il2CppDrova.Utilities.LazyLoading.LazyActor lazyActor = LazyActorCreator.CreateLazyActorCreature(encounter);
                LazyActorStore.Add(new LazyActorSaveData(LazyActorCreator.LazyActorName, encounter.AssetReference.AssetGUID, encounter.EntityInfo.AssetGUID, lazyActor._guidstring));
            }
            WorldEventSystemManager.Instance?.EndEvent();
        }
    }
}
