using Drova_Modding_API.Systems.Routines;
using HarmonyLib;
using Il2CppDrova.AI;
using Il2CppDrova.Routine;
using Il2CppNodeCanvas.BehaviourTrees;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Swaps the shared routine tree for the NPC's own one when <see cref="RoutineGraphAccess"/> has an assignment.
    /// Group places are not patched: hooking <c>BaseGroupRoutinePlace.AIGraph</c> crashes the runtime during mod
    /// initialization, so they always run the shared group graph.
    /// </summary>
    [HarmonyPatch(typeof(BaseRoutinePlace), nameof(BaseRoutinePlace.AIGraph), MethodType.Getter)]
    internal static class RoutinePlaceAIGraphPatch
    {
        internal static void Postfix(BaseRoutinePlace __instance, ref BehaviourTree __result)
        {
            if (!RoutineGraphAccess.HasAssignments) return;
            __result = RoutineGraphAccess.Resolve(__instance) ?? __result;
        }
    }

    /// <summary>
    /// Tells <see cref="RoutineGraphAccess"/> which NPC asks for a place's tree. NPCs sharing an <c>EntityInfo</c> share
    /// its places, and the place itself only knows the first of them, so the actor comes from the routine node.
    /// </summary>
    [HarmonyPatch(typeof(SubRoutineGraph), nameof(SubRoutineGraph.OnExecute))]
    internal static class SubRoutineGraphExecutePatch
    {
        // Only the node is kept: its NPC's guid is read in Resolve, which runs when a place's tree is asked for.
        internal static void Prefix(SubRoutineGraph __instance, out SubRoutineGraph? __state)
        {
            __state = RoutineGraphAccess.RunningSubRoutine;
            RoutineGraphAccess.RunningSubRoutine = RoutineGraphAccess.HasAssignments ? __instance : null;
        }

        internal static void Postfix(SubRoutineGraph? __state)
        {
            RoutineGraphAccess.RunningSubRoutine = __state;
        }
    }
}
