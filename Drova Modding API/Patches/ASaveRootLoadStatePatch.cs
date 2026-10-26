using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2CppCysharp.Threading.Tasks;
using Il2CppDrova.Saveables;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(ASaveRoot), nameof(ASaveRoot.LoadState))]
    internal static class ASaveRootLoadStatePatch
    {
        static bool Prefix(ASaveRoot __instance)
        {
            string instanceKey = __instance.GetGameObjectInstanceKey();
            if (NoSaveGuidRegistry.IsRegistered(instanceKey))
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ASaveRoot), nameof(ASaveRoot.LoadStateAsync))]
    internal static class ASaveRootLoadStatePatch2
    {
        static bool Prefix(ASaveRoot __instance, ref UniTask __result)
        {
            string instanceKey = __instance.GetGameObjectInstanceKey();
            if (NoSaveGuidRegistry.IsRegistered(instanceKey))
            {
                __instance._gameObjectData = new SaveData_GenericDictionary();
                __result = UniTask.CompletedTask;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ASaveRoot), nameof(ASaveRoot.GetGameObjectData))]
    internal static class ASaveRootGetGameObjectDataPatch
    {
         static bool Prefix(string instanceKey, ref SaveData_GenericDictionary __result)
         {
            if (NoSaveGuidRegistry.IsRegistered(instanceKey))
            {
                __result = new SaveData_GenericDictionary();
                return false;
            }

            return true;
        }
    }
}