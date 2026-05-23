using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.Saveables;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(Saveable_Health), nameof(Saveable_Health.Start))]
    internal static class SaveableHealthPatchMaxHealth
    {
        static bool Prefix(Saveable_Health __instance)
        {
            if (__instance._health)
            {
                var actor = __instance._health.OwnerEntity.TryCast<Actor>();
                if (actor && NoSaveGuidRegistry.IsRegistered(actor._guidComponent._guidString))
                {
                    // We can't let them subscribe on a non-saveable actor
                    return false;
                }
            }
            return true;
        }
    }

}