using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2CppDrova.AI.Graphs;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(SetSavegameRoutineIndex),  nameof(SetSavegameRoutineIndex.OnExecute))]
    internal static class SetSaveGameRouteIndexPatch
    {
        static bool Prefix(SetSavegameRoutineIndex __instance)
        {
            var guid = __instance._actor.GetValue().GetActor().GetGuidString();
            if (NoSaveGuidRegistry.IsRegistered(guid))
            {
                __instance.EndAction(true);
                return false;
            }
            return true;
        }
    }
}