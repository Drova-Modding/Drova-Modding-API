using HarmonyLib;
using Il2CppCysharp.Threading.Tasks;
using Il2CppDrova.Routine;

[HarmonyPatch(typeof(ActorRoutineModule), nameof(ActorRoutineModule.DelayedInit))]
internal static class ActorRoutineModulePatch
{
    static bool Prefix(ActorRoutineModule __instance, ref UniTask __result)
    {

        if (__instance._mapHandler == null)
        {
            return true;
        }

        __result = UniTask.CompletedTask;
        return false;
    }
}
