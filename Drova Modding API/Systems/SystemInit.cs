using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.DebugUtils;
using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.SaveGame.Store;
using Drova_Modding_API.Systems.WorldEvents;
using Drova_Modding_API.UI;
using Il2CppDrova.Saveables;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drova_Modding_API.Systems
{
    internal static class SystemInit
    {
        internal static void Init()
        {
            if (ProviderAccess.TryGetGameManager(out var gameManager))
            {
                GameObject moddingAPISystemRoot = new("ModdingAPI");
                moddingAPISystemRoot.SetActive(false);
                var areaNameSystem   = moddingAPISystemRoot.AddComponent<AreaNameSystem>();
                var worldEventSystem = moddingAPISystemRoot.AddComponent<WorldEventSystemManager>();
                worldEventSystem.areaNameSystem = areaNameSystem;
                moddingAPISystemRoot.SetActive(true);
                // TODO Fix me, it doesn't move the object to the scene
                SceneManager.MoveGameObjectToScene(moddingAPISystemRoot, gameManager.gameObject.scene);
                SaveGameSystem.Instance.OnLoad(Savegame.Current);
            }
#if DEBUG
            DebugUI.Init();
#endif
        }

        internal static void RegisterStores()
        {
            SaveGameSystem.Instance.AddStore(new LazyActorStore());
        }
    }
}
