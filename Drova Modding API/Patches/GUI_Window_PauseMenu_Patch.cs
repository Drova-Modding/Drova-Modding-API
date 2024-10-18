using Il2CppDrova.GUI;
using HarmonyLib;
using Drova_Modding_API.Access;
[HarmonyPatch(typeof(GUI_Window_PauseMenu), "OptionsButtonListener")]
static class GUI_Window_PauseMenu_Patch
{
    private static void Postfix()
    {
        OptionMenuAccess.OnOptionRoot();
    }
}
