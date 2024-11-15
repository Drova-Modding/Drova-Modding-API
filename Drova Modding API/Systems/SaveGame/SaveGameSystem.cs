
using Il2CppDrova.Saveables;
using Drova_Modding_API.Systems.SaveGame.Store;
using System.Text.Json;

namespace Drova_Modding_API.Systems.SaveGame
{
    /// <summary>
    /// Handles saving and loading of the game.
    /// </summary>
    public class SaveGameSystem
    {
        /**
         * The key for the lazy actors in the savegame
         */
        public const string SAVEGAME_KEY_LAZY_ACTORS = "DrovaModdingAPI_Lazy_Actors";
        /**
         * Delegate for savegame events
         */
        public delegate void SaveGameDelegate(Savegame saveGame);
        /**
         * Triggered before a savegame is loaded
         */
        public static event SaveGameDelegate BeforeSaveGameLoaded;
        /**
         * Triggered after a savegame is loaded
         */
        public static event SaveGameDelegate AfterSaveGameLoaded;
        /**
         * Triggered before a savegame is saved
         */
        public static event SaveGameDelegate BeforeSaveGameSaving;

        /**
         * The instance of the save game system
         */
        public static SaveGameSystem Instance { get; private set; } = new SaveGameSystem();

        internal List<IStorable> stores = [];

        private SaveGameSystem()
        {
            BeforeSaveGameSaving += OnSave;
        }

        private void OnSave(Savegame saveGame)
        {

            for (int i = 0; i < stores.Count; i++)
            {
                IStorable store = stores[i];
                string saved = store.Save();
                saveGame.Data.SetString(store.SaveGameKey, saved);
            }
        }

        internal static void TriggerBeforeSaveGameSaving(Savegame saveGame)
        {
            BeforeSaveGameSaving?.Invoke(saveGame);
        }

        internal static void TriggerBeforeSaveGameLoaded(Savegame saveGame)
        {
            BeforeSaveGameLoaded?.Invoke(saveGame);
        }

        internal static void TriggerAfterSaveGameLoaded(Savegame saveGame)
        {
            AfterSaveGameLoaded?.Invoke(saveGame);
        }

        internal void OnLoad(Savegame saveGame)
        {
            for (int i = 0; i < stores.Count; i++)
            {
                IStorable store = stores[i];
                string loaded = "";
                if(saveGame.Data.GetString(store.SaveGameKey, ref loaded))
                {
                    store.Load(loaded);
                }
            }
        }

        /// <summary>
        /// Register a new store to the save game system
        /// </summary>
        /// <param name="store">The store</param>
        public void AddStore(IStorable store)
        {
            stores.Add(store);
        }

        /// <summary>
        /// Get a store by type from the save game system
        /// </summary>
        /// <typeparam name="T">The store to get</typeparam>
        /// <returns>Your store or default</returns>
        public IStore<T> GetStore<T>()
        {
            return stores.OfType<IStore<T>>().FirstOrDefault();
        }

        /// <summary>
        /// Get a store by save game key from the save game system
        /// </summary>
        /// <param name="saveGameKey">The key of the Store</param>
        /// <returns></returns>
        public IStorable GetStore(string saveGameKey)
        {
            return stores.FirstOrDefault(x => x.SaveGameKey == saveGameKey);
        }
    }
}
