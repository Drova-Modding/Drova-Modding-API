using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2Cpp;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(Saveable_StatusEffectHandler), nameof(Saveable_StatusEffectHandler.Start))]
    internal static class SaveableStatusEffectHandlerPatch
    {
        static bool Prefix(Saveable_StatusEffectHandler __instance)
        {
            if (__instance._handler)
            {
                var actor = __instance._handler._owner;
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