using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2CppDrova.AI.Graphs;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(SaveBBParameterToSavegameIndex), nameof(SaveBBParameterToSavegameIndex.UpdateSaveData))]
    internal static class SaveBBParametersToSavegameIndexPatch
    {
        static bool Prefix(SaveBBParameterToSavegameIndex __instance)
        {
            var guid = __instance._actor.GetValue().GetActor().GetGuidString();
            if (NoSaveGuidRegistry.IsRegistered(guid))
            {
                return false;
            }
            return true;
        }
    }
}