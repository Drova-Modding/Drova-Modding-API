using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.Routine;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Game bug: the range check skips every entity that is not spawned, so a routine dialogue waiting for an absent
    /// NPC still starts (Cengiz greeting Larea at the campfire while she is still in the forest or dead).
    /// An entity that is not spawned counts as out of range and the failure command runs instead.
    /// </summary>
    [HarmonyPatch(typeof(IsEntityInRangeCommand), nameof(IsEntityInRangeCommand.OnExecuteCommand))]
    internal static class IsEntityInRangeCommandPatch
    {
        static bool Prefix(IsEntityInRangeCommand __instance, RoutineCommandArgs args, ref bool __result)
        {
            EntityGameHandler handler = EntityGameHandler.TryGet();
            for (int i = 0; i < __instance._entities.Count; i++)
            {
                EntityInfo info = __instance._entities[i];
                if (info == null || handler.GetFirstEntityByInfo(info) != null) continue;

                __result = __instance._failureCommand == null || __instance._failureCommand.ExecuteCommand(args);
                return false;
            }
            return true;
        }
    }
}
