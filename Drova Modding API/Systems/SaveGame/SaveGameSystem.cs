using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;
using Il2CppDrova.Saveables;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSirenix.Serialization;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;
using static Il2CppDrova.DialogueNew.LookDirectionsOverrides.ActorLookParam;

namespace Drova_Modding_API.Systems.SaveGame
{
    /// <summary>
    /// Handles saving and loading of the game.
    /// </summary>
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


        internal System.Collections.Generic.List<LazyActorSaveDataManaged> lazyActors = [];

        /**
         * Saves the lazy actors to the savegame
         * @param saveGame The savegame to save the lazy actors to
         */
        public void OnSave(Savegame saveGame)
        {
            //var saveData = new ModdingSave();
            //foreach (var lazyActor in lazyActors)
            //{

            //    saveData.LazyActors.Add(lazyActor);
            //}
            MelonLogger.Msg(ClassInjector.IsTypeRegisteredInIl2Cpp<LazyActorSaveData>());
            if (lazyActors.Count > 0)
            {
                LazyActorSaveData data = new LazyActorSaveData()
                {
                    ActorEnitityInfoReferenceString = lazyActors[0].ActorEnitityInfoReferenceString,
                    ActorGuid = lazyActors[0].ActorGuid,
                    ActorName = lazyActors[0].ActorName,
                    ActorReferenceString = lazyActors[0].ActorReferenceString
                };
                saveGame.Data._objectDict.TryInsert(SAVEGAME_KEY_LAZY_ACTORS, data, InsertionBehavior.OverwriteExisting);
                if (saveGame.Data._objectDict.ContainsKey(SAVEGAME_KEY_LAZY_ACTORS))
                {
                    LazyActorSaveData lazy = new LazyActorSaveData();
                    if (saveGame.Data.GetObject(SAVEGAME_KEY_LAZY_ACTORS, ref lazy))
                    {
                        MelonLogger.Msg("Loaded lazy actors");
                        if (!lazy.WasCollected)
                        {
                            MelonLogger.Msg(lazy.ActorName);
                            MelonLogger.Msg(lazy.ActorReferenceString);
                        }
                    }
                    else
                    {
                        MelonLogger.Msg("NOT Loaded lazy actors");
                        if (!lazy.WasCollected)
                        {
                            MelonLogger.Msg(lazy.ActorName);
                            MelonLogger.Msg(lazy.ActorReferenceString);
                        }
                    }
                    MelonLogger.Msg("Saved lazy actors");
                }
            }
            saveGame.Data._objectDict.TryInsert("Test", "Test", InsertionBehavior.OverwriteExisting);



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
            foreach (var test in DefaultSerializationBinder.typeMap)
            {
                var type = test.Value.GetType();
                MelonLogger.Msg(type?.FullName);
                MelonLogger.Msg(test.key);
            }
            //foreach (var test in DefaultSerializationBinder.assemblyNameLookUp)
            //{
            //    MelonLogger.Msg(test.Value.FullName);
            //    MelonLogger.Msg(test.key);
            //}
            if (saveGame.Data._objectDict.ContainsKey(SAVEGAME_KEY_LAZY_ACTORS))
                MelonLogger.Msg(saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS].GetIl2CppType());
            //if (saveGame.Data.GetObject(SAVEGAME_KEY_LAZY_ACTORS, ref lazy))
            //{
            //    MelonLogger.Msg("Loaded lazy actors");
            //}
            //else
            //{
            //    MelonLogger.Msg("NOT Loaded lazy actors");
            //    if (!lazy.WasCollected)
            //    {
            //        MelonLogger.Msg(lazy.ActorName);
            //        MelonLogger.Msg(lazy.ActorReferenceString);
            //    }
            //}

            //if(saveGame.Data._objectDict.ContainsKey("Test"))
            //{
            //    string test = "";
            //    if (saveGame.Data.GetString("Test", ref test))
            //    {
            //        MelonLogger.Msg(test);
            //    }
            //    MelonLogger.Msg("Loaded test");
            //}

            //if(saveGame.Data._objectDict.ContainsKey() {

            //    var keyData = saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS];
            //    var data = saveGame.Data._objectDict[SAVEGAME_KEY_LAZY_ACTORS].TryCast<LazyActorSaveData>();

            //    if (data == null) return;

            //    MelonLogger.Msg("Loaded lazy actors WTF!");
            //}
            //if (saveGame.Data.GetObject(SAVEGAME_KEY_LAZY_ACTORS, ref lazyActors))
            //{
            //    LazyActorCreator.RestoreLazyActor(lazyActors.ToArray().ToList());
            //}
        }
    }

    //public class ModdingSave : Il2CppSystem.Object
    //{
    //    /**
    //    * Constructor from il2cpp side
    //    */
    //    public ModdingSave(IntPtr ptr) : base(ptr) { }

    //    /**
    //     * Constructor from managed side
    //     */
    //    public ModdingSave() : base(ClassInjector.DerivedConstructorPointer<LazyActorSaveData>())
    //    {
    //        ClassInjector.DerivedConstructorBody(this);
    //    }

    //    public Il2CppSystem.Collections.Generic.List<LazyActorSaveData> LazyActors = new();
    //}
}

public class LazyActorSaveDataManaged
{
    public string ActorName = "";
    public string ActorReferenceString = "";
    public string ActorEnitityInfoReferenceString = "";
    public string ActorGuid = "";
}
