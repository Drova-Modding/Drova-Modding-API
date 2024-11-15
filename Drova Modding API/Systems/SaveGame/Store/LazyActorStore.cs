using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova.Saveables;
using System.Text.Json;

namespace Drova_Modding_API.Systems.SaveGame.Store
{
    internal class LazyActorStore : IStore<LazyActorSaveData>
    {
        private readonly List<LazyActorSaveData> lazyActors = [];

        private const string SAVEGAME_KEY_LAZY_ACTORS = "DrovaModdingAPI_Lazy_Actors";

        public string SaveGameKey { get => SAVEGAME_KEY_LAZY_ACTORS; }

        public void Add(LazyActorSaveData item)
        {
            lazyActors.Add(item);
        }

        public void Clear()
        {
            lazyActors.Clear();
        }

        public LazyActorSaveData Get(int index)
        {
            return lazyActors.ElementAt(index);
        }

        public void Remove(LazyActorSaveData item)
        {
            lazyActors.Remove(item);
        }

        public void AddRange(LazyActorSaveData[] items)
        {
            lazyActors.AddRange(items);
        }

        public IEnumerable<LazyActorSaveData> GetAll()
        {
            return lazyActors;
        }

        public void Load(string loaded)
        {
            lazyActors.Clear();
            lazyActors.AddRange(JsonSerializer.Deserialize<List<LazyActorSaveData>>(loaded));
            LazyActorCreator.RestoreLazyActor(lazyActors);
        }

        public string Save()
        {
            return JsonSerializer.Serialize(lazyActors);
        }
    }
}
