using HarmonyLib;
using Il2CppDrova.GUI;
using Drova_Modding_API.Register;

[HarmonyPatch(typeof(GUI_GameMenu_APanel), nameof(GUI_GameMenu_APanel.OnTryClosePanel))]
static class GUI_GameMenu_APanelPatch
{
    static void Postfix(ref bool __result)
    {
       if(InputActionRegister.isMappingCurrently)
        {
            __result = false;
        }
    }
}

