using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppDrova.GUI.Options;

[HarmonyPatch(typeof(GUI_ButtonClick_OpenOptions), "OnClickListener")]
static class GUI_ButtonClick_OpenOptions_Patch
{
#pragma warning disable IDE0051 // Remove unused private members
    private static void Postfix()
#pragma warning restore IDE0051 // Remove unused private members
    {
        OptionMenuAccess.OnOptionRoot();
    }
}

