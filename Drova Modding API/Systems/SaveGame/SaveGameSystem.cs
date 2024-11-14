
using Il2CppDrova.Saveables;

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


        internal List<LazyActorSaveDataManaged> lazyActors = [];

        /**
         * Saves the lazy actors to the savegame
         * @param saveGame The savegame to save the lazy actors to
         */
        public void OnSave(Savegame saveGame)
        {
            
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

        /**
         * Registers a lazy actor to be saved
         * @param lazyActorSaveData The lazy actor to save
         */
        public void RegisterLazyActor(LazyActorSaveDataManaged lazyActorSaveData)
        {
            lazyActors.Add(lazyActorSaveData);
        }

        /**
         * Loads the lazy actors from the savegame
         * @param saveGame The savegame to load the lazy actors from
         */
        public void OnLoad(Savegame saveGame)
        {
            
        }
    }
}
