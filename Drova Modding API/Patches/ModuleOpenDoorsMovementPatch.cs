using HarmonyLib;
using Il2CppDrova.AI.Logic;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// A game bug: an NPC that opens a door subscribes to the door's "interaction ended" event and never unsubscribes.
    /// Once that NPC's body is unloaded or destroyed, the next use of the door runs the dead NPC's listener, which asks
    /// its gone actor for itself and throws - one NullReferenceException per such NPC, every time the door is used.
    /// The listener only clears the NPC's own "in a door interaction" flag, so a listener without an actor has nothing
    /// to do and is skipped.
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
