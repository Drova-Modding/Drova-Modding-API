using HarmonyLib;
using Il2CppDrova.Saveables;
using Drova_Modding_API.Systems.SaveGame;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Harmony prefix patch for <c>ASaveRoot.SaveState()</c> that skips saving when the
    /// actor's instance key (GUID) is registered in <see cref="NoSaveGuidRegistry"/>.
    /// 
    /// This allows modders to prevent an entire GameObject and its child saveables from
    /// being saved by simply adding the DisableSavingMarker component to the root, or by
    /// registering the actor's GUID in <see cref="NoSaveGuidRegistry"/>.
    /// </summary>
    [HarmonyPatch(typeof(ASaveRoot), nameof(ASaveRoot.SaveState))]
    internal static class ASaveRootSaveStatePatch
    {
        private static bool Prefix(ASaveRoot __instance)
        {
            // Check if the instance key (GUID) is registered in the no-save registry
            string instanceKey = __instance.GetGameObjectInstanceKey();
            if (NoSaveGuidRegistry.IsRegistered(instanceKey))
            {
                return false;
            }

            return true;
        }
    }
}



