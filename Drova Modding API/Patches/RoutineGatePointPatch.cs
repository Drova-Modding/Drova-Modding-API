using Drova_Modding_API.Systems.MapEditor;
using Drova_Modding_API.Systems.Routines;
using HarmonyLib;
using Il2CppDrova.AI.Logic;
using Il2CppDrova.Routine;
using Il2CppDrova.Utilities.LazyLoading;
using UnityEngine;
using Object = UnityEngine.Object;
using Il2CppCollections = Il2CppSystem.Collections.Generic;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Runs the gate points of <see cref="RoutineSystem.AddGatePoint"/>. A vanilla <c>TeleportRoutinePoint</c> finds its
    /// target in the baked map database, which knows no mod gates, so these points take theirs from the API.
    /// </summary>
    [HarmonyPatch(typeof(TeleportRoutinePoint))]
    internal static class RoutineGatePointPatch
    {
        private const float FadeSeconds = 1f;
        private const float LookupSeconds = 1f;
        private static float _nextLookup;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportRoutinePoint.GetCommand))]
        private static bool GetCommand(TeleportRoutinePoint __instance, ref IRoutineCommand __result)
        {
            if (!RoutineSystem.GateTargets.TryGetValue(__instance.Pointer, out Vector2 target)) return true;

            Il2CppCollections.List<IRoutineCommand> steps = new();
            steps.Add(new FadeOutRoutineCommand(FadeSeconds, false, null).Cast<IRoutineCommand>());
            steps.Add(new SetPositionRoutineCommand(target).Cast<IRoutineCommand>());
            __result = new SetPositionOnStopRoutineCommand(target, new SequenceRoutineCommands(steps).Cast<IRoutineCommand>()).Cast<IRoutineCommand>();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportRoutinePoint.IsActive))]
        private static bool IsActive(TeleportRoutinePoint __instance, IAIActor entity, ref bool __result)
        {
            if (!RoutineSystem.GateTargets.TryGetValue(__instance.Pointer, out Vector2 target)) return true;

            LazyActor? lazy = entity == null ? PlaceActor(__instance) : null;
            Vector2? position = entity != null ? entity.GetPosition() : lazy != null ? lazy.GetFeetPosition() : null;
            __result = Application.isPlaying && (position == null || MapAreas.At(position.Value) != MapAreas.At(target));
            return false;
        }

        /// <summary>
        /// The NPC of the point's place while it has no AI actor, such as an unloaded NPC of a place set to one actor,
        /// which the map database does not resolve. Found once and kept on the place, as the game does for its own.
        /// </summary>
        private static LazyActor? PlaceActor(TeleportRoutinePoint point)
        {
            BaseRoutinePlace place = point.GetComponentInParent<BaseRoutinePlace>(true);
            if (place == null) return null;
            if (place._runningActor != null) return place._runningActor;

            string? guid = place.GetEntityGuid();
            if (string.IsNullOrEmpty(guid) || Time.unscaledTime < _nextLookup) return null;

            _nextLookup = Time.unscaledTime + LookupSeconds;
            place._runningActor = Object.FindObjectsOfType<LazyActor>().FirstOrDefault(lazy => lazy.GUID == guid);
            return place._runningActor;
        }
    }
}
