using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Editor;
using Drova_Modding_API.Systems.Routines;
using Drova_Modding_API.Systems.Routines.Store;
#if DEBUG
using Drova_Modding_API.Systems.GlobalVars;
#endif
using Drova_Modding_API.Systems.MapEditor;
using Drova_Modding_API.Systems.Quests;
using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.SaveGame.Store;
using Drova_Modding_API.Systems.Spawning;
using Drova_Modding_API.Systems.Talents;
using Drova_Modding_API.Systems.WorldEvents;
using Il2CppDrova.Saveables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drova_Modding_API.Systems
{
    internal static class SystemInit
    {
        private const string ActorSpawnRootName = "ModdingAPI_Actors";

        internal static readonly LazyActorStore LazyActors = new();

        internal static void GameplayInit()
        {
            TalentContainerDatabase.InitializeDatabase();
            GameObject moddingAPISystemRoot = new("ModdingAPI");
            moddingAPISystemRoot.SetActive(false);
            AreaNameSystem areaNameSystem = moddingAPISystemRoot.AddComponent<AreaNameSystem>();
            WorldEventSystemManager worldEventSystem = moddingAPISystemRoot.AddComponent<WorldEventSystemManager>();
            worldEventSystem.AreaNameSystem = areaNameSystem;
#if DEBUG
            moddingAPISystemRoot.AddComponent<GlobalVarInspectorSystem>();
#endif
            moddingAPISystemRoot.AddComponent<DialogueAudioDistanceManager>();

            moddingAPISystemRoot.SetActive(true);

#if DEBUG
            LocalizationEntriesEditorSystem.Initialize();
            NpcWizardSystem.Initialize();
            EditorUI.Init();
#endif
            TalentContainerDatabase.Init();
#if DEBUG
            Debugging.NearbyDumper.Register();
#endif
            PlayerAccess.StartWaitForPlayerCoroutine();
            SaveGameSystem.AfterSaveGameLoaded -= OnSaveGameSystemOnAfterSaveGameLoaded;
            SaveGameSystem.AfterSaveGameLoaded += OnSaveGameSystemOnAfterSaveGameLoaded;
        }
        private static void OnSaveGameSystemOnAfterSaveGameLoaded(Savegame _)
        {
            PlayerAccess.StartWaitForPlayerCoroutine();
        }

        internal static void RegisterStores()
        {
            SaveGameSystem.Instance.AddStore(LazyActors);
        }

        internal static void AiLogicInit(Scene scene)
        {
            GameObject moddingAPIAiLogicSystemRoot = new("ModdingAPI_AILogic");
            SceneManager.MoveGameObjectToScene(moddingAPIAiLogicSystemRoot, scene);
            RoutineSystem.RoutineRoot = moddingAPIAiLogicSystemRoot;
            RoutineSystem.GateTargets.Clear();
            BehaviourTreeStore.PatchAllSavedBehaviourTrees();
            RoutineGraphAccess.OnWorldLoaded();
            RoutineOverrideStore.ApplyPlaces();
        }

        internal static void ActorInit(Scene scene)
        {
            GameObject actorSpawnRoot = GameObject.Find(ActorSpawnRootName);
            if (actorSpawnRoot == null)
                actorSpawnRoot = new GameObject(ActorSpawnRootName);

            SceneManager.MoveGameObjectToScene(actorSpawnRoot, scene);
            ActorSpawnAccess.PruneDestroyed();
            GVarConditions.BeginWorldLoad();
            NpcCreator.SetSpawnRoot(actorSpawnRoot.transform);
            NpcCreator.CacheAlignments();
            ExternalNpcPlacementSystem.SpawnConfiguredNpcs();
            ExternalCreaturePlacementSystem.SpawnConfiguredCreatures();
            ExternalItemPlacementSystem.SpawnConfiguredItems(scene);
            QuestAreas.Spawn(scene);
            MapLayoutSystem.BeginWorld(scene);
        }
    }
}
