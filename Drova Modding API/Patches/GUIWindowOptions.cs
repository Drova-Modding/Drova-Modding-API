using Drova_Modding_API.Register;
using HarmonyLib;
using Il2CppDrova.GUI;

[HarmonyPatch(typeof(GUI_Window_Options), nameof(GUI_Window_Options.CloseWindow))]
static class GUIWindowOptionsCloseWindowPatch
{
    static bool Prefix(GUI_Window_Options __instance)
    {
        // If currently mapping input keys, prevent window from closing
        if (InputActionRegister.isMappingCurrently)
        {
            return false; // Skip the original method
        }
        return true; // Allow the original method to execute
    }
}


