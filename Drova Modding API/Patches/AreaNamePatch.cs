using Drova_Modding_API.Systems;
using HarmonyLib;
using Il2CppDrova;
using MelonLoader;

#pragma warning disable IDE0051 // Remove unused private members

[HarmonyPatch(typeof(AreaName), "OnAreaNameEnter")]
internal class AreaNamePatch
{
    private static void Postfix(AreaName __instance)
    {
        AreaNameSystem.Instance?.OnAreaEntered(__instance.AreaKey);
    }
}

[HarmonyPatch(typeof(AreaName), "OnAreaNameExit")]
internal class AreaNamePatchExit
{
    private static void Postfix(AreaName __instance)
    {
        AreaNameSystem.Instance?.UnregisterAreaName(__instance.AreaKey);
    }
}
#pragma warning restore IDE0051 // Remove unused private members

