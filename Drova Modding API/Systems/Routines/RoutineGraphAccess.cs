using Drova_Modding_API.Systems.Routines.Store;
using Il2CppCustomFramework;
using Il2CppDrova;
using Il2CppDrova.AI;
using Il2CppDrova.Routine;
using Il2CppNodeCanvas.BehaviourTrees;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Routines
{
    /// <summary>
    /// Gives an NPC its own routine graph instead of the shared <see cref="DefaultRoutineGraph"/>. Keys are an actor
    /// GUID or an <see cref="EntityInfo"/> GUID, and an actor assignment wins. Group places always run the shared
    /// group graph. Assignments last until the game closes; persist them with <see cref="RoutineOverrideStore"/>.
    /// </summary>
    public static class RoutineGraphAccess
    {
        private static readonly Dictionary<string, string> TreeIdByGuid = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, BehaviourTree> LoadedTrees = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Trees that have a pool in the current pool table. Cleared on every world load, which builds a new table.
        /// </summary>
        private static readonly HashSet<string> PooledTreeIds = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The shared tree every routine place runs unless its NPC has an assignment.
        /// </summary>
        public static BehaviourTree? DefaultRoutineGraph => AResourceScriptableSingleton<AIDatabase>.Instance?.RoutineGraph;

        /// <summary>
        /// The shared tree every group routine place runs unless its NPC has an assignment.
        /// </summary>
        public static BehaviourTree? DefaultGroupGraph => AResourceScriptableSingleton<AIDatabase>.Instance?.GroupGraph;

        /// <summary>
        /// Whether any NPC has an assignment. The routine getters run every tick, so they check this before
        /// touching the place.
        /// </summary>
        internal static bool HasAssignments => TreeIdByGuid.Count > 0;

        /// <summary>
        /// The routine node executing right now, set by <c>SubRoutineGraphExecutePatch</c>; its NPC is the asking actor.
        /// </summary>
        internal static SubRoutineGraph? RunningSubRoutine { get; set; }

        /// <summary>
        /// Assigns a stored tree to an NPC until the game closes. Takes effect when the NPC next enters a routine place;
        /// call <see cref="RefreshRunningRoutines"/> to apply it to NPCs that are already running one.
        /// </summary>
        /// <param name="guid">The actor's GUID, or the GUID of its <see cref="EntityInfo"/></param>
        /// <param name="treeId">The id the tree was saved under in <see cref="BehaviourTreeStore"/>; null or empty removes the assignment</param>
        public static void Assign(string guid, string? treeId)
        {
            if (string.IsNullOrWhiteSpace(guid)) return;

            if (string.IsNullOrWhiteSpace(treeId))
            {
                TreeIdByGuid.Remove(guid.Trim());
                return;
            }

            TreeIdByGuid[guid.Trim()] = treeId!.Trim();
        }

        /// <summary>
        /// The tree id assigned to an actor or entity GUID.
        /// </summary>
        /// <param name="guid">The actor's GUID, or the GUID of its <see cref="EntityInfo"/></param>
        /// <param name="treeId">The assigned tree id</param>
        /// <returns>Whether the GUID has an assignment</returns>
        public static bool TryGetAssigned(string guid, out string treeId)
        {
            treeId = string.Empty;
            return !string.IsNullOrWhiteSpace(guid) && TreeIdByGuid.TryGetValue(guid.Trim(), out treeId!);
        }

        /// <summary>
        /// Drops the cached copy of a stored tree and restarts every running routine that uses it, so the stored
        /// version is used from now on.
        /// </summary>
        public static void Invalidate(string treeId)
        {
            DropLoadedTree(treeId);
            RefreshRunningRoutines(treeId);
        }

        /// <summary>
        /// Restarts the running routines that run one of <paramref name="changedTreeIds"/> or whose NPC got a different
        /// assignment. Pooled and running copies of the changed trees are discarded.
        /// </summary>
        /// <param name="changedTreeIds">Names of trees whose content changed</param>
        public static void RefreshRunningRoutines(params string[] changedTreeIds)
        {
            HashSet<string> changed = new(changedTreeIds, StringComparer.OrdinalIgnoreCase);
            Il2CppSystem.Collections.Generic.Dictionary<string, GraphPool>? pools = GetPools();
            foreach (string treeId in changed)
            {
                if (pools != null && pools.TryGetValue(treeId, out GraphPool pool)) pool.Clear();
            }

            foreach (BehaviourTree tree in Resources.FindObjectsOfTypeAll<BehaviourTree>())
            {
                if (tree.parentGraph != null && changed.Contains(tree.name))
                {
                    tree.name += "#stale";
                }
            }

            Restart(subRoutine =>
            {
                string running = subRoutine.subGraph.name;
                string? expected = GetExpectedTreeName(subRoutine);
                return changed.Contains(running) || (expected != null && expected != running);
            });
        }

        /// <summary>
        /// Restarts the routines of the NPCs currently in one of the given places, so a changed place (moved, added or
        /// removed points) is picked up. The NPC re-enters the place and continues at the point it had saved for it.
        /// </summary>
        /// <param name="placeKeys">Keys of the changed places (<c>IRoutinePlace.Key()</c>)</param>
        public static void RestartRoutinesAt(IEnumerable<string> placeKeys)
        {
            HashSet<string> keys = new(placeKeys, StringComparer.OrdinalIgnoreCase);
            Restart(subRoutine => keys.Contains(subRoutine._currentPlaceKey));
        }

        /// <summary>
        /// Every running routine's place and tree, as <c>place key -&gt; running tree name</c>, by actor GUID.
        /// </summary>
        public static Dictionary<string, (string PlaceKey, string TreeName)> GetRunningRoutines()
        {
            Dictionary<string, (string, string)> running = new(StringComparer.OrdinalIgnoreCase);
            ForEachSubRoutine(subRoutine =>
            {
                string? actorGuid = GetActorGuid(subRoutine);
                if (actorGuid != null) running[actorGuid] = (subRoutine._currentPlaceKey, subRoutine.subGraph.name);
            });
            return running;
        }

        /// <summary>
        /// The GUID of the NPC a routine node runs for, or null before the node has an actor.
        /// </summary>
        internal static string? GetActorGuid(SubRoutineGraph subRoutine)
        {
            string? actorGuid = subRoutine._actorFactory?.GetValue()?.GetActor()?.GetGuidString();
            return string.IsNullOrEmpty(actorGuid) ? null : actorGuid;
        }

        /// <summary>
        /// The assigned tree for a routine place, or null when its NPC has none. The NPC is the one whose routine node is
        /// executing; outside of that, the actor the place is bound to, then the place's <see cref="EntityInfo"/>.
        /// </summary>
        /// <param name="place">The place asking for its tree</param>
        internal static BehaviourTree? Resolve(BaseRoutinePlace place)
        {
            string? treeId = null;
            string? actorGuid = (RunningSubRoutine != null ? GetActorGuid(RunningSubRoutine) : null) ?? place.GetEntityGuid();
            if (string.IsNullOrEmpty(actorGuid) || !TreeIdByGuid.TryGetValue(actorGuid!, out treeId))
            {
                string? entityGuid = place.GetEntityInfo()?.GUID;
                if (string.IsNullOrEmpty(entityGuid) || !TreeIdByGuid.TryGetValue(entityGuid!, out treeId)) return null;
            }

            if (!LoadedTrees.TryGetValue(treeId!, out BehaviourTree? tree) || tree == null)
            {
                if (!BehaviourTreeStore.TryLoadBehaviourTree(treeId!, out tree))
                {
                    RemoveAssignmentsOf(treeId!);
                    return null;
                }

                tree.name = treeId!;
                tree.hideFlags = HideFlags.DontUnloadUnusedAsset;
                LoadedTrees[treeId!] = tree;
                PooledTreeIds.Remove(treeId!);
            }

            if (!PooledTreeIds.Contains(treeId!)) RegisterPool(treeId!, tree);
            return tree;
        }

        /// <summary>
        /// Forgets which trees have a pool. Called on every world load, which builds a new pool table.
        /// </summary>
        internal static void OnWorldLoaded()
        {
            PooledTreeIds.Clear();
        }

        /// <summary>
        /// Drops every cached tree, so the stored files are read again when an NPC next needs them.
        /// </summary>
        internal static void ClearLoadedTrees()
        {
            foreach (string treeId in LoadedTrees.Keys.ToList())
            {
                DropLoadedTree(treeId);
            }
        }

        /// <summary>
        /// The tree a running routine node should run now, by name, with the node's own NPC as the asking actor.
        /// </summary>
        private static string? GetExpectedTreeName(SubRoutineGraph subRoutine)
        {
            SubRoutineGraph? previous = RunningSubRoutine;
            RunningSubRoutine = subRoutine;
            try
            {
                return subRoutine._currentPlace?.AIGraph?.name;
            }
            finally
            {
                RunningSubRoutine = previous;
            }
        }

        private static void RemoveAssignmentsOf(string treeId)
        {
            List<string> guids = TreeIdByGuid.Where(pair => string.Equals(pair.Value, treeId, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key).ToList();
            foreach (string guid in guids)
            {
                TreeIdByGuid.Remove(guid);
            }

            MelonLogger.Warning($"RoutineGraphAccess: routine graph '{treeId}' could not be loaded from '{BehaviourTreeStore.GetFilePath(treeId)}'. "
                + $"The NPCs assigned to it ({string.Join(", ", guids)}) use the shared routine graph. Check the GraphId in their file under '{RoutineOverrideStore.GetFolderPath()}'.");
        }

        private static void DropLoadedTree(string treeId)
        {
            PooledTreeIds.Remove(treeId);
            if (!LoadedTrees.TryGetValue(treeId, out BehaviourTree? tree)) return;

            LoadedTrees.Remove(treeId);
            if (tree != null) Object.Destroy(tree);
        }

        /// <summary>
        /// Makes the matching routines pick their place and tree again on their next tick: the NPC re-enters its
        /// current place with whatever tree the place now resolves to.
        /// </summary>
        private static void Restart(Func<SubRoutineGraph, bool> predicate)
        {
            ForEachSubRoutine(subRoutine =>
            {
                if (!predicate(subRoutine)) return;
                subRoutine._currentPlaceKey = string.Empty;
            });
        }

        /// <summary>
        /// Visits every routine node that currently runs a tree. A started node's <c>subGraph</c> is a clone that keeps
        /// the source tree's name, so trees are compared by name.
        /// </summary>
        private static void ForEachSubRoutine(Action<SubRoutineGraph> visit)
        {
            foreach (BehaviourTree tree in Resources.FindObjectsOfTypeAll<BehaviourTree>())
            {
                foreach (Node node in tree.allNodes)
                {
                    SubRoutineGraph? subRoutine = node?.TryCast<SubRoutineGraph>();
                    if (subRoutine?.subGraph != null) visit(subRoutine);
                }
            }
        }

        /// <summary>
        /// Gives an own tree a graph pool. The game claims routine instances from a pool keyed by tree name and logs an
        /// error when none exists.
        /// </summary>
        private static void RegisterPool(string treeId, BehaviourTree tree)
        {
            Il2CppSystem.Collections.Generic.Dictionary<string, GraphPool>? pools = GetPools();
            if (pools == null) return;

            string poolKey = tree.name;
            if (pools.TryGetValue(poolKey, out GraphPool existing))
            {
                if (existing._prefab != null && existing._prefab.Pointer == tree.Pointer)
                {
                    PooledTreeIds.Add(treeId);
                    return;
                }
                existing.Clear();
            }
            pools[poolKey] = new GraphPool(tree, poolKey);
            PooledTreeIds.Add(treeId);
        }

        private static Il2CppSystem.Collections.Generic.Dictionary<string, GraphPool>? GetPools()
        {
            return FactoryAIGameHandler.TryGet(out FactoryAIGameHandler handler) && handler != null
                ? handler._graphPool?._poolDict
                : null;
        }
    }
}
