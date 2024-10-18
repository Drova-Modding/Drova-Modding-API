using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppDrova.GUI.Options;

[HarmonyPatch(typeof(GUI_ButtonClick_OpenOptions), "OnClickListener")]
static class GUI_ButtonClick_OpenOptions_Patch
{
    private static void Postfix()
    {
        OptionMenuAccess.OnOptionRoot();
    }
}

