using Drova_Modding_API.Register;
using HarmonyLib;
using Il2CppDrova.GUI;

[HarmonyPatch(typeof(GUI_Window_Options), nameof(GUI_Window_Options.CloseWindow))]
static class GUIWindowOptionsCloseWindowPatch
{
    static bool Prefix(GUI_Window_Options __instance)
    {
        if (InputActionRegister.isMappingCurrently)
        {
            return false;
        }
        return true;
    }
}


