using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Il2CppDrova;
using Il2CppDrova.AI.Logic;
using Il2CppDrova.AI.Modules;
using Il2CppDrova.Alignment;
using Il2CppDrova.Cognition;
using Il2CppDrova.Stats;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Finds actors that are hostile to a player, with the same alignment test the game's aim assist uses,
    /// and makes enemies notice and chase their targets from further away.
    /// </summary>
    public static class EnemyAccess
    {
        private const float GroupLoadSeconds = 20f;
        private const float ChaseSeconds = 300f;
        private static readonly WaitForSeconds RecheckWait = new(1f);

        /// <summary>
        /// Makes <paramref name="actor"/> notice its enemies from <paramref name="range"/> away and chase them that far
        /// before it gives up, the way the game's aggro trigger zones do (<c>HitReceiveBhvr_SetAggroToActor</c>).
        /// Territorial actors do not stop at their border, and teasing ones attack without posturing first.
        /// </summary>
        /// <param name="actor">An initialized actor. In co-op, call it where the actor's AI runs (the world authority).</param>
        /// <param name="range">Aggro and chase range in world units. The game's own default is 100 to 125.</param>
        public static void MakeAggressive(Actor actor, float range)
        {
            Component? ai = actor._aiModule != null ? actor._aiModule.GetAi() : null;
            AI_TargetBhvr? target = ai != null ? ai.GetComponentInChildren<AI_TargetBhvr>() : null;
            if (target != null)
            {
                // SetAggroRange leaves out the soft chase circle, which would end the chase at the prefab's 250 to 300.
                target.SetAggroRange(range);
                target._looseActorRange = range;
            }

            CognitionSeeingAroundBhvr seeing = actor.GetComponentInChildren<CognitionSeeingAroundBhvr>();
            if (seeing != null)
            {
                seeing.SetNewRange(range);
            }

            AI_Perks perks = actor.GetComponentInChildren<AI_Perks>();
            if (perks != null)
            {
                perks._territorial = false;
                perks._preAggroPerk = PreAggroPerk.Aggressive;
            }
        }

        /// <summary>
        /// Once their bodies have loaded, puts <paramref name="members"/> in one AI group, so the first to fight pulls
        /// the rest in, and applies <see cref="MakeAggressive"/> to each. Members not loaded within 20 seconds are left out.
        /// </summary>
        /// <param name="members">Lazy actors spawned together, for example by <c>BanditCreator</c>.</param>
        /// <param name="range">Aggro and chase range in world units, see <see cref="MakeAggressive"/>.</param>
        public static void MakeAggressiveGroup(IReadOnlyList<LazyActor> members, float range)
        {
            MelonCoroutines.Start(GroupWhenLoaded(members, range, null));
        }

        /// <summary>
        /// Does <see cref="MakeAggressiveGroup(IReadOnlyList{LazyActor}, float)"/> and sends the group at
        /// <paramref name="target"/> at once, as the game's aggro zones do. Without it a group spawned past its
        /// perception radius never notices anyone and stands at its spawn point.
        /// </summary>
        /// <param name="members">Lazy actors spawned together, for example by <c>BanditCreator</c>.</param>
        /// <param name="range">Aggro and chase range in world units, see <see cref="MakeAggressive"/>.</param>
        /// <param name="target">Whom they attack, a player or a co-op teammate's body.</param>
        public static void MakeAggressiveGroup(IReadOnlyList<LazyActor> members, float range, Actor? target)
        {
            MelonCoroutines.Start(GroupWhenLoaded(members, range, target));
        }

        private static IEnumerator GroupWhenLoaded(IReadOnlyList<LazyActor> members, float range, Actor? target)
        {
            float deadline = Time.time + GroupLoadSeconds;
            while (Time.time < deadline && members.Any(member => member != null && (member.Actor == null || !member.Actor.IsInitialized)))
            {
                yield return null;
            }

            List<Actor> actors = members.Where(member => member != null && member.Actor != null && member.Actor.IsInitialized)
                .Select(member => member.Actor).ToList();
            IAIActor? targetActor = target != null && target.AIModule != null ? target.AIModule.GetIAIFactory().GetActor() : null;
            foreach (Actor actor in actors)
            {
                // Human NPCs carry no AI_GroupBhvr, so SetInCombat shares targets through this group module instead.
                foreach (Actor other in actors.Where(other => other != actor))
                {
                    actor._aiModule.AddActorInGroup(other._aiModule.GetIAIFactory().GetActor());
                }
                MakeAggressive(actor, range);

                if (targetActor != null) SendAt(actor, targetActor);
            }

            if (targetActor == null) yield break;

            // A body that streams out and back in is a new actor without any of this, so it is sent again.
            // ponytail: polls for ChaseSeconds, a load event per handle if many groups ever run at once.
            HashSet<IntPtr> sent = actors.Select(actor => actor.Pointer).ToHashSet();
            float chaseEnd = Time.time + ChaseSeconds;
            while (Time.time < chaseEnd && target != null)
            {
                yield return RecheckWait;
                foreach (LazyActor member in members)
                {
                    Actor? body = member != null ? member.Actor : null;
                    if (body == null || !body.IsInitialized || !sent.Add(body.Pointer) || body.GetHealth()?.IsDead == true) continue;

                    MakeAggressive(body, range);
                    SendAt(body, targetActor);
#if DEBUG
                    MelonLogger.Msg($"[EnemyAccess] {body.name} streamed in again and was sent at {target.name}.");
#endif
                }
            }
        }

        private static void SendAt(Actor actor, IAIActor target)
        {
            actor.GetHealth()?.SetReceivedDamageLastFrame();
            actor._aiModule.AddTargetInCombat(target);
        }

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
