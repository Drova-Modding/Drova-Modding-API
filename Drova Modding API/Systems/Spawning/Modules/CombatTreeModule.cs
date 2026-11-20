using Drova_Modding_API.Systems.Routines;
using Il2CppDrova.AI.Modules;
using Il2CppNodeCanvas.Framework;
using MelonLoader;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Gives the NPC one of the game's combat trees instead of the one its melee weapon picks, for example a bow tree,
    /// which the default combat tree never chooses. Only trees in the AI factory's graph pools work, because the game
    /// claims every running combat graph from its pool.
    /// </summary>
    /// <param name="treeName">The tree's asset name, e.g. <c>AI_Sub_NPC_Combat_2H_Bow_Talent_01</c>.</param>
    public class CombatTreeModule(string treeName) : INpcModule
    {
        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            AIGraphsBehaviours? graphs = context.GetComponentInChildren<AIGraphsBehaviours>();
            if (graphs == null) return;

            Il2CppSystem.Collections.Generic.Dictionary<string, GraphPool>? pools = RoutineGraphAccess.GetPools();
            if (pools == null || !pools.TryGetValue(treeName, out GraphPool pool) || pool._prefab == null)
            {
                MelonLogger.Warning($"[Spawning] No combat tree '{treeName}' in the AI graph pools.");
                return;
            }

            graphs.ChangeTree(ActorBhvrTrees.Combat, pool._prefab);
        }
    }
}
