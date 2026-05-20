using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppDrova.GUI;

[HarmonyPatch(typeof(GUI_Window_Options), "CloseWindow")]
static class GUIWindowPatchClose
{
    private const string WindowName = "GUI_Window_Options(Clone)";
    
    private static void Postfix(GUI_Window __instance)
    {
        if (__instance.name == WindowName)
            OptionMenuAccess.OnOptionClose();
    }
}

[HarmonyPatch(typeof(GUI_Window), "ShowWindow")]
static class GUIWindowPatchOpen
{
    private const string WindowName = "GUI_Window_Options(Clone)";
    
    private static void Postfix(GUI_Window __instance)
    {
        if (__instance.name == WindowName)
            OptionMenuAccess.OnOptionOpen();
    }
}


