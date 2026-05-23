using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Editor;
using Drova_Modding_API.Systems.Routines;
#if DEBUG
using Drova_Modding_API.Systems.GlobalVars;
#endif
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
            SaveGameSystem.Instance.OnLoad(Savegame.Current);
            NpcCreator.CacheAlignments();

#if DEBUG
            EditorUI.Init();
#endif
        }

        internal static void RegisterStores()
        {
            SaveGameSystem.Instance.AddStore(new LazyActorStore());
        }

        internal static void AiLogicInit(Scene scene)
        {
            GameObject moddingAPIAiLogicSystemRoot = new("ModdingAPI_AILogic");
            SceneManager.MoveGameObjectToScene(moddingAPIAiLogicSystemRoot, scene);
            RoutineSystem.RoutineRoot = moddingAPIAiLogicSystemRoot;
        }
    }
}