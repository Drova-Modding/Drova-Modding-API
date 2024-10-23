using Il2CppDrova.GUI;
using HarmonyLib;
using Drova_Modding_API.Access;

[HarmonyPatch(typeof(GUI_Window_PauseMenu), "OptionsButtonListener")]
static class GUI_Window_PauseMenu_Patch
{
#pragma warning disable IDE0051 // Remove unused private members
    private static void Postfix()
#pragma warning restore IDE0051 // Remove unused private members
    {
        OptionMenuAccess.OnOptionRoot();
    }
}
