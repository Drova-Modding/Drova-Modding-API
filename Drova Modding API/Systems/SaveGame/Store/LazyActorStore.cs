using Drova_Modding_API.Systems.Spawning;
using System.Text.Json;

namespace Drova_Modding_API.Systems.SaveGame.Store
{
    internal class LazyActorStore : IStore<LazyActorSaveData>
    {
        private readonly List<LazyActorSaveData> _lazyActors = [];

        private const string SavegameKeyLazyActors = "DrovaModdingAPI_Lazy_Actors";

        public string SaveGameKey { get => SavegameKeyLazyActors; }

        public void Add(LazyActorSaveData item)
        {
            _lazyActors.Add(item);
        }

        public void Clear()
        {
            _lazyActors.Clear();
        }

        public LazyActorSaveData Get(int index)
        {
            return _lazyActors.ElementAt(index);
        }

        public void Remove(LazyActorSaveData item)
        {
            _lazyActors.Remove(item);
        }

        public void AddRange(LazyActorSaveData[] items)
        {
            _lazyActors.AddRange(items);
        }

        public IEnumerable<LazyActorSaveData> GetAll()
        {
            return _lazyActors;
        }

        public void Load(string loaded)
        {
            _lazyActors.Clear();
            _lazyActors.AddRange(JsonSerializer.Deserialize<List<LazyActorSaveData>>(loaded));
            LazyActorCreator.RestoreLazyActor(_lazyActors);
            ExternalNpcPlacementSystem.HydrationRebuildSpawnedActorsFromStore();
        }

        public string Save()
        {
            return JsonSerializer.Serialize(_lazyActors);
        }
    }
}
