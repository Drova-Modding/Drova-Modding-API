using HarmonyLib;
using Il2CppDrova.AI.Logic;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Game bug: an NPC that opens a door never unsubscribes from its "interaction ended" event, so once the NPC is
    /// unloaded every use of the door throws in its listener. That listener only clears the NPC's own flag, so it is skipped.
    /// </summary>
    [HarmonyPatch(typeof(ModuleOpenDoorsMovement), "InteractionEndedEventListener")]
    internal static class ModuleOpenDoorsMovementPatch
    {
        static bool Prefix(ModuleOpenDoorsMovement __instance)
        {
            try
            {
                return __instance._aiActor?.GetActor()?.GetObject() != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
