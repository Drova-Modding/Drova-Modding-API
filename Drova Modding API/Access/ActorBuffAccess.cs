using Il2Cpp;
using Il2CppDrova;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// A runtime buff for one actor. Factors multiply, armor and magic resistance are added as flat points.
    /// </summary>
    /// <param name="HealthFactor">Multiplies the maximum health. 2 doubles it.</param>
    /// <param name="DamageFactor">Multiplies every physical, magical and pure hit the actor deals.</param>
    /// <param name="Armor">Flat physical resistance. Against a non-player 10 points block about a quarter of a hit.</param>
    /// <param name="MagicResistance">Flat magical resistance, on the same scale as <paramref name="Armor"/>.</param>
    public readonly record struct ActorBuff(float HealthFactor = 1f, float DamageFactor = 1f, int Armor = 0, int MagicResistance = 0);

    /// <summary>
    /// Buffs an actor's health, damage and resistances through the game's own stat modifiers, the same ones
    /// equipment and abilities use. Nothing here is saved: the buff is gone after a save load or once the actor
    /// is unloaded, and a buff applied twice stacks.
    /// </summary>
    public static class ActorBuffAccess
    {
        /// <summary>
        /// Applies <paramref name="buff"/> to <paramref name="actor"/> and refills its health to the new maximum.
        /// </summary>
        /// <returns>False when the actor is dead, not initialized yet, or the gameplay settings are not loaded.</returns>
        public static bool TryApply(Actor actor, ActorBuff buff)
        {
            if (actor == null || !actor.IsInitialized)
            {
                return false;
            }

            Health health = actor.GetHealth();
            AttributeStats stats = actor.GetStats();
            if (health == null || stats == null || health.IsDead
                || !DamageAccess.TryGetPhysicalDamageType(out DamageStatDesc? physical)
                || !DamageAccess.TryGetMagicalDamageType(out DamageStatDesc? magical))
            {
                return false;
            }

            try
            {
                // Runtime max health is the unsaved part, so the authored base value stays untouched.
                health.AddRuntimeMaxHealth(Mathf.RoundToInt(health.MaxHealth * (buff.HealthFactor - 1f)));
                health.SetHealthPercent(1f);

                ScaleDamage(stats, physical, buff.DamageFactor);
                ScaleDamage(stats, magical, buff.DamageFactor);
                if (DamageAccess.TryGetPureDamageType(out DamageStatDesc? pure))
                {
                    ScaleDamage(stats, pure, buff.DamageFactor);
                }

                AddResistance(stats, physical, buff.Armor);
                AddResistance(stats, magical, buff.MagicResistance);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[ActorBuffAccess] applying a buff failed: " + e);
                return false;
            }
        }

        private static void ScaleDamage(AttributeStats stats, DamageStatDesc damageType, float factor)
        {
            if (Mathf.Approximately(factor, 1f) || damageType.RelatedDamageFactor == null)
            {
                return;
            }

            GenericStat stat = stats.GetGenericStat(damageType.RelatedDamageFactor);
            float before = stat.GetFloatValue();
            stat.CreateModifier(factor, ModifiableFloat.Mode.Mult);

            // The stat clamps to its descriptor's range, which would cap a large factor silently.
            float after = stat.GetFloatValue();
            if (!Mathf.Approximately(after, before * factor))
            {
                MelonLogger.Warning($"[ActorBuffAccess] {damageType.RelatedDamageFactor.name} is capped at {after}, wanted {before * factor}.");
            }
        }

        private static void AddResistance(AttributeStats stats, DamageStatDesc damageType, int points)
        {
            if (points != 0 && damageType.RelatedResistanceType != null)
            {
                stats.GetGenericStat(damageType.RelatedResistanceType).CreateModifier(points, ModifiableFloat.Mode.Add);
            }
        }
    }
}
