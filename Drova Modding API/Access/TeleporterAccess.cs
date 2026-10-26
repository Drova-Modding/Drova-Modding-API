using Il2CppInterop.Runtime;
using Il2CppOpenWorldSystem;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Access to Drova's area-transition gates. Every transition is an <see cref="OW_Teleporter"/> whose
    /// <c>_targetPos</c> and <c>_targetMoveDir</c> are an arrival anchor beside its partner gate, and vanilla
    /// links are bidirectional pairs. Overrides are enforced on existing gates, when a gate streams in, and
    /// when the world becomes ready. Rewire whole pairs, never single directions, or the player can be stranded.
    /// </summary>
    public static class TeleporterAccess
    {
        /// <summary>The destination a gate teleports to.</summary>
        public struct GateDestination
        {
            /// <summary>World position the player arrives at.</summary>
            public Vector2 TargetPos;
            /// <summary>Facing/move direction applied on arrival.</summary>
            public Vector2 TargetMoveDir;
        }

        private static readonly Dictionary<string, GateDestination> Overrides = new(StringComparer.Ordinal);
        private static bool _hooked;

        /// <summary>
        /// Every gate in memory, including ones in inactive chunks. A heap sweep, so not for every frame or scene load.
        /// </summary>
        /// <returns>All scene gates.</returns>
        public static List<OW_Teleporter> GetAllGates()
        {
            List<OW_Teleporter> gates = [];
            UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(Il2CppType.Of<OW_Teleporter>());
            foreach (UnityEngine.Object obj in all)
            {
                OW_Teleporter? teleporter = obj == null ? null : obj.TryCast<OW_Teleporter>();
                if (teleporter != null && teleporter.gameObject.scene.IsValid())
                {
                    gates.Add(teleporter);
                }
            }
            return gates;
        }

        /// <summary>
        /// Read a gate's current destination.
        /// </summary>
        /// <param name="gate">The gate.</param>
        /// <returns>Its current arrival anchor.</returns>
        public static GateDestination GetDestination(OW_Teleporter gate)
        {
            if (gate == null)
            {
                throw new ArgumentNullException(nameof(gate));
            }
            return new GateDestination
            {
                TargetPos = gate._targetPos,
                TargetMoveDir = gate._targetMoveDir,
            };
        }

        /// <summary>
        /// Persistently override where the gate named <paramref name="gateName"/> leads. Applied
        /// to every existing instance immediately and re-applied whenever the gate streams in.
        /// </summary>
        /// <param name="gateName">The gate GameObject's name.</param>
        /// <param name="destination">The arrival anchor to enforce.</param>
        public static void SetDestinationOverride(string gateName, GateDestination destination)
        {
            HookOnce();
            Overrides[gateName] = destination;
            ApplyOverrides();
        }

        /// <summary>
        /// Register several overrides at once. Prefer this over calling
        /// <see cref="SetDestinationOverride"/> in a loop: <see cref="ApplyOverrides"/> runs a
        /// heap sweep, so registering one at a time sweeps once per entry.
        /// </summary>
        /// <param name="destinations">Gate name to arrival anchor.</param>
        public static void SetDestinationOverrides(IEnumerable<KeyValuePair<string, GateDestination>> destinations)
        {
            HookOnce();
            foreach (KeyValuePair<string, GateDestination> destination in destinations)
            {
                Overrides[destination.Key] = destination.Value;
            }
            ApplyOverrides();
        }

        /// <summary>
        /// Stop enforcing an override. The gate keeps whatever destination it currently has until
        /// its scene reloads with the authored value; write the vanilla anchor back via
        /// <see cref="SetDestinationOverride"/> if you need an immediate restore.
        /// </summary>
        /// <param name="gateName">The gate GameObject's name.</param>
        public static void RemoveDestinationOverride(string gateName)
        {
            Overrides.Remove(gateName);
        }

        /// <summary>
        /// Drop all overrides (see <see cref="RemoveDestinationOverride"/> for restore caveats).
        /// </summary>
        public static void ClearDestinationOverrides()
        {
            Overrides.Clear();
        }

        /// <summary>
        /// Re-apply every override to every existing gate. Called automatically when overrides
        /// change and when the player is found (world ready); call it yourself after events you
        /// know invalidate gates.
        /// </summary>
        public static void ApplyOverrides()
        {
            if (Overrides.Count == 0)
            {
                return;
            }
            try
            {
                UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(Il2CppType.Of<OW_Teleporter>());
                foreach (UnityEngine.Object obj in all)
                {
                    OW_Teleporter? teleporter = obj == null ? null : obj.TryCast<OW_Teleporter>();
                    // A prefab asset has no valid scene, and mutating it leaks the override to every future instance.
                    if (teleporter != null && teleporter.gameObject.scene.IsValid())
                    {
                        Apply(teleporter);
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[TeleporterAccess] ApplyOverrides failed: " + e);
            }
        }

        private static bool Apply(OW_Teleporter teleporter)
        {
            if (!Overrides.TryGetValue(teleporter.name, out GateDestination destination))
            {
                return false;
            }
            teleporter._targetPos = destination.TargetPos;
            teleporter._targetMoveDir = destination.TargetMoveDir;
            return true;
        }

        /// <summary>
        /// Lazy: the OnEnable patch and the world-ready re-sweep only exist once the first
        /// override is registered, so mods that never touch teleporters pay nothing.
        /// </summary>
        private static void HookOnce()
        {
            if (_hooked)
            {
                return;
            }
            _hooked = true;
            Hooking.TryPostfix(Core.SharedHarmony, typeof(OW_Teleporter), "OnEnable",
                typeof(TeleporterAccess), nameof(OnEnablePostfix));
            PlayerAccess.OnPlayerFound += _ => ApplyOverrides();
        }

        private static void OnEnablePostfix(OW_Teleporter __instance)
        {
            try
            {
                Apply(__instance);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[TeleporterAccess] OnEnable patch failed: " + e);
            }
        }
    }
}
