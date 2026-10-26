using System.Diagnostics.CodeAnalysis;
using Il2CppDrova;
using Il2CppDrova.Alignment;
using Il2CppDrova.Stats;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Finds actors that are hostile to a player, with the same alignment test the game's aim assist uses.
    /// </summary>
    public static class EnemyAccess
    {
        /// <summary>
        /// The closest living actor within <paramref name="radius"/> that is hostile to <paramref name="player"/>.
        /// Brawl opponents and knocked out actors do not count.
        /// </summary>
        /// <param name="player">Whose enemies to look for.</param>
        /// <param name="radius">Search radius in world units.</param>
        /// <param name="filter">Further condition a candidate must pass, or null.</param>
        /// <param name="enemy">The closest match.</param>
        public static bool TryFindNearest(Actor player, float radius, Func<Actor, bool>? filter, [NotNullWhen(true)] out Actor? enemy)
        {
            enemy = null;
            EntityGameHandler? handler = ProviderAccess.GetEntityGameHandler();
            if (handler == null || player == null)
            {
                return false;
            }

            IAlignmentArgs args = new AlignmentArgs(player).Cast<IAlignmentArgs>();
            Vector2 origin = player.transform.position;
            float best = float.MaxValue;

            foreach (Entity entity in handler.GetDynamicEntitiesInCircle(origin, Vector2.zero, 0f, radius, null))
            {
                Actor? actor = entity != null ? entity.TryCast<Actor>() : null;
                if (actor == null || actor.Pointer == player.Pointer || !actor.IsInitialized || handler.CheckIsPlayer(actor))
                {
                    continue;
                }

                float distance = ((Vector2)actor.transform.position - origin).sqrMagnitude;
                if (distance >= best || !IsHostileTarget(actor, args))
                {
                    continue;
                }

                if (filter != null && !filter(actor))
                {
                    continue;
                }

                best = distance;
                enemy = actor;
            }

            return enemy != null;
        }

        private static bool IsHostileTarget(Actor actor, IAlignmentArgs args)
        {
            Health health = actor.GetHealth();
            if (health == null || health.IsDead)
            {
                return false;
            }

            // A brawl makes the opponent read as an enemy for its duration only.
            ICustomStats stats = actor.CustomStats;
            if (stats != null && (stats.IsInBrawl || stats.IsKnockout))
            {
                return false;
            }

            IAlignment alignment = actor.Alignment;
            return alignment != null && alignment.CheckAlignment(args, AlignmentState.Enemy);
        }
    }
}
