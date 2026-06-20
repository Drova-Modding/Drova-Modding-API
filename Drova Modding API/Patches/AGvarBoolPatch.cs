using Drova_Modding_API.Systems.GlobalVars;
using HarmonyLib;
using Il2CppDrova.GlobalVarSystem;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(AGVar<bool>), nameof(AGVar<bool>.SetValue))]
    internal static class AGvarBoolPatch
    {
        internal static void Postfix(bool t, AGVar<bool> __instance)
        {
            GvarBusSystem.RaiseGBoolChanged(__instance.name, t);
        }
    }
}