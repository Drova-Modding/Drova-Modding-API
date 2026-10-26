using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2CppDrova.Utilities.LazyLoading;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(LazyActor), nameof(LazyActor.SavePos))]
    internal static class LazyActorPatch
    {
        static bool Prefix(LazyActor __instance)
        {
            return !NoSaveGuidRegistry.IsRegistered(__instance.GUID);
        }
    }
}