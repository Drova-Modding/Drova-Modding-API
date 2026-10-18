using System.Diagnostics.CodeAnalysis;
using Il2CppDrova;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Deals damage to an actor, and heals one, through the game's own damage path.
    /// Use this rather than writing <c>Health._curHealth</c> or calling <c>Health.SetHealth</c>: a raw write that
    /// reaches zero skips the damage blockers and latches <c>_isDead</c>, which nothing ever clears.
    /// The amount is always a positive magnitude; the <see cref="DamageStatDesc"/> decides whether it heals or hurts,
    /// and the target's own defenses still apply.
    /// </summary>
    public static class DamageAccess
    {
        /// <summary>
        /// Deals damage of a given type to an actor.
        /// Leave <paramref name="source"/> null when the figure is already final: the game adds a source's offensive
        /// stats to the value, so an attacker whose gear is already in the figure counts twice. A null source only
        /// loses the aggro tag, the killed-by credit, block punishment and the player's crit and bleed rolls.
        /// </summary>
        /// <param name="target">The actor to damage. Its health component is looked up here.</param>
        /// <param name="amount">How much to deal, as a positive magnitude before the target's defenses.</param>
        /// <param name="damageType">Which resistance answers the blow. See <see cref="TryGetPhysicalDamageType"/>.</param>
        /// <param name="source">Who dealt it, or null to apply the figure as given.</param>
        /// <returns>False when the actor has no health component, which is normal for a frame or two
        /// after a world loads, or when the damage type is null.</returns>
        public static bool TryApplyDamage(Actor target, int amount, DamageStatDesc damageType, Entity? source = null)
        {
            if (target == null)
            {
                return false;
            }

            return TryApplyDamage(target.GetHealth(), amount, damageType, source);
        }

        /// <summary>
        /// Deals damage of a given type to a health component. See
        /// <see cref="TryApplyDamage(Actor, int, DamageStatDesc, Entity)"/> for what
        /// <paramref name="source"/> means and why it is usually null; this overload only saves the
        /// lookup for callers that already hold the component.
        /// </summary>
        /// <param name="target">The health component to damage.</param>
        /// <param name="amount">How much to deal, as a positive magnitude before the target's defenses.</param>
        /// <param name="damageType">Which resistance answers the blow.</param>
        /// <param name="source">Who dealt it, or null to apply the figure as given.</param>
        /// <returns>False when the target or the damage type is null.</returns>
        public static bool TryApplyDamage(Health target, int amount, DamageStatDesc damageType, Entity? source = null)
        {
            if (target == null || damageType == null)
            {
                return false;
            }

            try
            {
                HealthChange change = new(source, new HealthChangeValue(amount, damageType));
                target.ChangeHealth(change);
                return true;
            }
            catch (Exception e)
            {
                // Swallowed: the usual caller is a frame loop or a Harmony patch, where a throw loses every later hit too.
                MelonLogger.Error("[DamageAccess] applying damage failed: " + e);
                return false;
            }
        }

        /// <summary>
        /// Deals physical damage, the type essentially every weapon in the game deals, and the one to
        /// reach for when a mod has a number and no particular opinion about what kind of damage it is.
        /// </summary>
        /// <param name="target">The actor to damage.</param>
        /// <param name="amount">How much to deal, as a positive magnitude before the target's defenses.</param>
        /// <param name="source">Who dealt it, or null to apply the figure as given. See
        /// <see cref="TryApplyDamage(Actor, int, DamageStatDesc, Entity)"/>.</param>
        /// <returns>False when the actor has no health component or the gameplay settings are not up
        /// yet.</returns>
        public static bool TryApplyPhysicalDamage(Actor target, int amount, Entity? source = null)
        {
            if (!TryGetPhysicalDamageType(out DamageStatDesc? physical))
            {
                return false;
            }

            return TryApplyDamage(target, amount, physical, source);
        }

        /// <summary>
        /// Restores health through the same calculation, as a health change carrying the healing type.
        /// A dead actor is refused, an invincible one is healed.
        /// </summary>
        /// <param name="target">The actor to heal.</param>
        /// <param name="amount">How much to restore, as a positive magnitude.</param>
        /// <param name="source">Who healed them, or null. Unlike damage, a source here adds that entity's
        /// healing stats to the figure.</param>
        /// <returns>False when the actor has no health component or the global assets are not up yet.</returns>
        public static bool TryHeal(Actor target, int amount, Entity? source = null)
        {
            if (!TryGetHealingType(out DamageStatDesc? healing))
            {
                return false;
            }

            return TryApplyDamage(target, amount, healing, source);
        }

        /// <summary>
        /// The damage type answered by armor and physical resistance. What weapons deal.
        /// </summary>
        /// <param name="damageType">The type, or null when unavailable.</param>
        /// <returns>False while the game is still bootstrapping or no world is loaded.</returns>
        public static bool TryGetPhysicalDamageType([NotNullWhen(true)] out DamageStatDesc? damageType)
        {
            GameplaySettingsHandler? handler = ProviderAccess.GetGameplaySettingsHandler();
            damageType = handler != null ? handler.PhysicalDamageStatDesc : null;

            return damageType != null;
        }

        /// <summary>
        /// The damage type answered by magical resistance. What most spells deal.
        /// </summary>
        /// <param name="damageType">The type, or null when unavailable.</param>
        /// <returns>False while the game is still bootstrapping or no world is loaded.</returns>
        public static bool TryGetMagicalDamageType([NotNullWhen(true)] out DamageStatDesc? damageType)
        {
            GameplaySettingsHandler? handler = ProviderAccess.GetGameplaySettingsHandler();
            damageType = handler != null ? handler.MagicalDamageStatDesc : null;

            return damageType != null;
        }

        /// <summary>
        /// The damage type nothing resists. The blockers inside the health calculation still run.
        /// </summary>
        /// <param name="damageType">The type, or null when unavailable.</param>
        /// <returns>False while the game is still bootstrapping or no world is loaded.</returns>
        public static bool TryGetPureDamageType([NotNullWhen(true)] out DamageStatDesc? damageType)
        {
            GlobalAssetsGameHandler? handler = ProviderAccess.GetGlobalAssetsGameHandler();
            damageType = handler != null ? handler.PureDmgType : null;

            return damageType != null;
        }

        /// <summary>
        /// The type that makes a health change count as healing. Lives on a different handler from the
        /// damaging types, which is the only reason it has its own accessor.
        /// </summary>
        /// <param name="damageType">The type, or null when unavailable.</param>
        /// <returns>False while the game is still bootstrapping or no world is loaded.</returns>
        public static bool TryGetHealingType([NotNullWhen(true)] out DamageStatDesc? damageType)
        {
            GlobalAssetsGameHandler? handler = ProviderAccess.GetGlobalAssetsGameHandler();
            damageType = handler != null ? handler.HealingDamageType : null;

            return damageType != null;
        }
    }
}
