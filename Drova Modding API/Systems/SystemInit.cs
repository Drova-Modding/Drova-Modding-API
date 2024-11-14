using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.WorldEvents;
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
                //SaveGameSystem.Instance.OnLoad(ProviderAccess.GetSaveGameHandler().CurrentSavegame);
                worldEventSystem.areaNameSystem = areaNameSystem;

                moddingAPISystemRoot.SetActive(true);
                // TODO Fix me, it doesn't move the object to the scene
                SceneManager.MoveGameObjectToScene(moddingAPISystemRoot, gameManager.gameObject.scene);
                
            }
        }
    }
}
