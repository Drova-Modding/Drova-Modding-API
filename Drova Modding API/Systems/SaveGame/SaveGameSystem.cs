using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova.Saveables;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSirenix.Serialization;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.SaveGame
{
    /// <summary>
    /// Handles saving and loading of the game.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    public class SaveGameSystem(IntPtr ptr) : MonoBehaviour(ptr)
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
        public static SaveGameSystem Instance { get; private set; }

        internal System.Collections.Generic.List<LazyActorSaveData> lazyActors = [];

        /**
         * Saves the lazy actors to the savegame
         * @param saveGame The savegame to save the lazy actors to
         */
        public void OnSave(Savegame saveGame)
        {
            var saveData = new ModdingSave();
            foreach (var lazyActor in lazyActors)
            {

                saveData.LazyActors.Add(lazyActor);
            }
            saveGame.Data.SetObject(SAVEGAME_KEY_LAZY_ACTORS, lazyActors[0]);

            if (saveGame.Data._objectDict.ContainsKey(SAVEGAME_KEY_LAZY_ACTORS))
            {
                MelonLogger.Msg("Saved lazy actors");
            }
        }

        private void Awake()
        {
            BeforeSaveGameSaving -= OnSave;
            AfterSaveGameLoaded -= OnLoad;
            BeforeSaveGameSaving += OnSave;
            AfterSaveGameLoaded += OnLoad;
            OnLoad(ProviderAccess.GetSaveGameHandler().CurrentSavegame);
            Instance = this;
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
        public void RegisterLazyActor(LazyActorSaveData lazyActorSaveData)
        {
            lazyActors.Add(lazyActorSaveData);
        }

        /**
         * Loads the lazy actors from the savegame
         * @param saveGame The savegame to load the lazy actors from
         */
        public void OnLoad(Savegame saveGame)
        {
            if (lazyActors.Count > 0)
                lazyActors.Clear();
            foreach(var test in DefaultSerializationBinder.typeMap)
            {
                var type = test.Value.GetType();
                MelonLogger.Msg(type?.FullName);
                MelonLogger.Msg(test.key);
            }
            if (saveGame.Data._objectDict.ContainsKey(SAVEGAME_KEY_LAZY_ACTORS))
            {
                
                var keyData = saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS];
                var data = saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS].TryCast<LazyActorSaveData>();

                if (data == null) return;

               

            }
            //if (saveGame.Data.GetObject(SAVEGAME_KEY_LAZY_ACTORS, ref lazyActors))
            //{
            //    LazyActorCreator.RestoreLazyActor(lazyActors.ToArray().ToList());
            //}
        }
    }

    [RegisterTypeInIl2Cpp]
    public class ModdingSave : Il2CppSystem.Object
    {
        /**
        * Constructor from il2cpp side
        */
        public ModdingSave(IntPtr ptr) : base(ptr) { }

        /**
         * Constructor from managed side
         */
        public ModdingSave() : base(ClassInjector.DerivedConstructorPointer<LazyActorSaveData>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public Il2CppSystem.Collections.Generic.List<LazyActorSaveData> LazyActors = new();
    }

    /**
    * The data for a lazy actor
    */
    [RegisterTypeInIl2Cpp]
    public class LazyActorSaveData : Il2CppSystem.Object
    {

        /**
        * Constructor from il2cpp side
        */
        public LazyActorSaveData(IntPtr ptr) : base(ptr) { }

        /**
         * Constructor from managed side
         */
        public LazyActorSaveData() : base(ClassInjector.DerivedConstructorPointer<LazyActorSaveData>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
        /**
         * The name of the actor
         */
        public string ActorName = "";
        /**
         * The reference string for the actor
         */
        public string ActorReferenceString = "";
        /**
         * The reference string for the entity info of the actor
         */
        public string ActorEnitityInfoReferenceString = "";
        /**
         * The guid of the actor
         */
        public string ActorGuid = "";
    }
}
