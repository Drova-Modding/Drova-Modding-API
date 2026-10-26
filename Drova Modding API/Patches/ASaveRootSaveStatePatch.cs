using HarmonyLib;
using Il2CppDrova.Saveables;
using Drova_Modding_API.Systems.SaveGame;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Skips saving a GameObject and its child saveables when its GUID is in <see cref="NoSaveGuidRegistry"/>,
    /// which is how the DisableSavingMarker component takes effect.
    /// </summary>
    [HarmonyPatch(typeof(ASaveRoot), nameof(ASaveRoot.SaveState))]
    internal static class ASaveRootSaveStatePatch
    {
        private static bool Prefix(ASaveRoot __instance)
        {
            string instanceKey = __instance.GetGameObjectInstanceKey();
            if (NoSaveGuidRegistry.IsRegistered(instanceKey))
            {
                return false;
            }

            return true;
        }
    }
}



