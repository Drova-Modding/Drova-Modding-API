using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova.Saveables;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Runtime.Serialization;
using Il2CppSystem.Runtime.Serialization.Formatters.Binary;
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

        internal Il2CppSystem.Collections.Generic.List<LazyActorSaveData> lazyActors = new();

        /**
         * Saves the lazy actors to the savegame
         * @param saveGame The savegame to save the lazy actors to
         */
        public void OnSave(Savegame saveGame)
        {
            saveGame.Data.SetObject(SAVEGAME_KEY_LAZY_ACTORS, lazyActors);
            if (saveGame.Data._objectDict.ContainsKey(SAVEGAME_KEY_LAZY_ACTORS))
            {
                MelonLogger.Msg("Loading lazy actors");
                var data = saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS].Cast<Il2CppSystem.Collections.Generic.List<LazyActorSaveData>>();
                foreach (var lazyActor in data)
                {
                    MelonLogger.Msg($"Loading lazy actor {lazyActor.ActorGuid}");
                }
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
            if (saveGame.Data._objectDict.ContainsKey(SAVEGAME_KEY_LAZY_ACTORS))
            {
                if (saveGame.Data.GetObject(SAVEGAME_KEY_LAZY_ACTORS, ref lazyActors))
                {
                    LazyActorCreator.RestoreLazyActor(lazyActors.ToArray().ToList());
                }
                MelonLogger.Msg("Loading lazy actors");
                var keyData = saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS];
                var data = saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS].TryCast<Il2CppSystem.Collections.Generic.List<LazyActorSaveData>>();
                var test = (object)keyData;
                var test2 = (List<LazyActorSaveData>)test;
                foreach(var tesos in test2)
                {
                    MelonLogger.Msg(tesos.ActorGuid);
                }
                
                if (test == null)
                {
                    MelonLogger.Msg("help");
                }


                if (data == null) return;

                foreach (var lazyActor in data)
                {
                    MelonLogger.Msg($"Loading lazy actor {lazyActor.ActorGuid}");
                    lazyActors.Add(lazyActor);
                }

            }
            if (saveGame.Data.GetObject(SAVEGAME_KEY_LAZY_ACTORS, ref lazyActors))
            {
                LazyActorCreator.RestoreLazyActor(lazyActors.ToArray().ToList());
            }
        }

    }

    /**
    * The data for a lazy actor
    */
    [Serializable]
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
