using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.SaveGame.Store;
using Drova_Modding_API.Systems.Spawning;

namespace Drova_Modding_API.Systems.WorldEvents
{

    /// <summary>
    /// A persistent encounter event that will spawn a list of encounters that will not despawn even when the event ends.
    /// If you don't want to persist the encounters, use <see cref="EncounterEvent"/> instead.
    /// </summary>
    /// <param name="persitentEncounters">The list to spawn</param>
    public class PersistentEncounterEvent(List<LazyActorCreator.LazyActorParams> persitentEncounters) : IWorldEvent
    {
        /**
         * The store for the lazy actors
         */
        protected IStore<LazyActorSaveData> lazyActorStore;

        /// <inheritdoc/>
        public virtual void EndEvent()
        {
            // Do nothing
        }

        /// <inheritdoc/>
        public virtual void StartEvent()
        {
            lazyActorStore ??= SaveGameSystem.Instance.GetStore<LazyActorSaveData>();
            for (int i = 0; i < persitentEncounters.Count; i++)
            {
                LazyActorCreator.LazyActorParams encounter = persitentEncounters[i];
                var lazyActor = LazyActorCreator.CreateLazyActorCreature(encounter);
                lazyActorStore.Add(new LazyActorSaveData(LazyActorCreator.LAZY_ACTOR_NAME, encounter.AssetReference.AssetGUID, encounter.EntityInfo.AssetGUID, lazyActor._guidstring));
            }
            WorldEventSystemManager.Instance.EndEvent();
        }
    }
}
