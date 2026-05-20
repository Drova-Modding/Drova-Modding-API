using Drova_Modding_API.Systems;
using HarmonyLib;
using Il2CppDrova;


[HarmonyPatch(typeof(AreaName), "OnAreaNameEnter")]
internal class AreaNamePatch
{
    static void Postfix(AreaName __instance)
    {
        AreaNameSystem.Instance?.OnAreaEntered(__instance.AreaKey);
    }
}

[HarmonyPatch(typeof(AreaName), "OnAreaNameExit")]
internal class AreaNamePatchExit
{
    static void Postfix(AreaName __instance)
    {
        AreaNameSystem.Instance?.UnregisterAreaName(__instance.AreaKey);
    }
}