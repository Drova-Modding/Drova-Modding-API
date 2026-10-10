using Drova_Modding_API.Systems;
using HarmonyLib;
using Il2CppDrova;


[HarmonyPatch(typeof(AreaName), "OnAreaNameEnter")]
internal class AreaNamePatch
{
    static void Postfix(AreaName __instance)
    {
        AreaNameSystem? system = AreaNameSystem.Instance;
        if (system != null) system.OnAreaEntered(__instance.AreaKey);
    }
}

[HarmonyPatch(typeof(AreaName), "OnAreaNameExit")]
internal class AreaNamePatchExit
{
    static void Postfix(AreaName __instance)
    {
        AreaNameSystem? system = AreaNameSystem.Instance;
        if (system != null) system.UnregisterAreaName(__instance.AreaKey);
    }
}