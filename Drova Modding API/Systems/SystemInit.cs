using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Editor;
using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.SaveGame.Store;
using Drova_Modding_API.Systems.WorldEvents;
using Il2CppDrova.Saveables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drova_Modding_API.Systems
{
    internal static class SystemInit
    {
        internal static void Init()
        {
            if (ProviderAccess.TryGetGameManager(out Il2Cpp.GameManager gameManager))
            {
                GameObject moddingAPISystemRoot = new("ModdingAPI");
                moddingAPISystemRoot.SetActive(false);
                AreaNameSystem areaNameSystem = moddingAPISystemRoot.AddComponent<AreaNameSystem>();
                WorldEventSystemManager worldEventSystem = moddingAPISystemRoot.AddComponent<WorldEventSystemManager>();
                worldEventSystem.areaNameSystem = areaNameSystem;
                moddingAPISystemRoot.SetActive(true);
                // TODO Fix me, it doesn't move the object to the scene
                SceneManager.MoveGameObjectToScene(moddingAPISystemRoot, gameManager.gameObject.scene);
                SaveGameSystem.Instance.OnLoad(Savegame.Current);
            }
#if DEBUG
            EditorUI.Init();
#endif
        }

        internal static void RegisterStores()
        {
            SaveGameSystem.Instance.AddStore(new LazyActorStore());
        }
    }
}
