using Il2CppDrova.GUI;
using HarmonyLib;
using Drova_Modding_API.Access;

[HarmonyPatch(typeof(GUI_Window), "CloseWindow")]
static class GUI_Window_Patch_Close
{
#pragma warning disable IDE0051 // Remove unused private members
    private static void Postfix()
#pragma warning restore IDE0051 // Remove unused private members
    {
        OptionMenuAccess.OnOptionClose();
    }
}

[HarmonyPatch(typeof(GUI_Window), "ShowWindow")]
static class GUI_Window_Patch_Open
{
#pragma warning disable IDE0051 // Remove unused private members
    private static void Postfix()
#pragma warning restore IDE0051 // Remove unused private members
    {
        OptionMenuAccess.OnOptionOpen();
    }
}

