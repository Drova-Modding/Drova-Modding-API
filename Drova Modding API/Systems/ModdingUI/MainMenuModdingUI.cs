using Drova_Modding_API.Access;
using Il2Cpp;
using Il2CppDrova.GUI;
using Il2CppDrova.LoadingScreenHandles;
using MelonLoader;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.ModdingUI
{
    internal static class MainMenuModdingUI
    {
        internal static void Init()
        {
           GameObject button = GameObject.Find("Btn_Continue");
           var devButton = Object.Instantiate(button, button.transform.parent);
           devButton.name = "Btn_ModdingAPI";
           var realButton = devButton.GetComponent<ButtonJ2D>();
           realButton.onClick.AddListener(new Action(PrepareCheatMode));
           
           devButton.transform.SetSiblingIndex(0);
           
           var name = devButton.GetComponentInChildren<LocalizedTextMeshPro>();
           name._localizedString = LocalizationAccess.GetLocalizedString("Modding_API/MainMenu", "DevContinue");
           name.UpdateLocalizedText();
        }

        internal static void PrepareCheatMode()
        {
            MelonCoroutines.Start(PrepareCheatModeCoroutine());
        }
        
        private static IEnumerator PrepareCheatModeCoroutine()
        {
            yield return new WaitForSeconds(5f); // Wait for the loading screen to appear and initialize
            var instance = LoadingScreenHandler.Instance;
            while (!instance.IsWorldReady())
            {
                yield return new WaitForSeconds(2);
            }
            CheatMenuAccess.FireCommand("noclip");
            CheatMenuAccess.FireCommand("camstop");
            CheatMenuAccess.FireCommand("god");
            CheatMenuAccess.FireCommand("invincibility");
        }
    }
}