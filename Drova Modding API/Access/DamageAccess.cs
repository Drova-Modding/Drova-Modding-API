using Il2CppDrova;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Deals damage to an actor, and heals one, through the game's own damage path.
    ///
    /// **Use this rather than writing <c>Health._curHealth</c> or calling <c>Health.SetHealth</c>.**
    /// A direct write stores the figure and skips the entire calculation, which sounds like the honest
    /// way to apply a number you have already worked out and is in fact the dangerous one. Damage
    /// blockers live inside that calculation, and one of them is what turns a killing blow into whatever
    /// the game does instead of dying. A raw write that reaches zero latches <c>_isDead</c>, which
    /// nothing ever clears, so the actor is permanently dead with every revive path standing by unused.
    /// Invincibility, resistances, armor, block angle and the death events all live in there too.
    ///
    /// The amount is always a positive magnitude. Whether it heals or hurts is a property of the
    /// <see cref="DamageStatDesc"/>, not of the sign - <c>HealthChange</c> negates it itself for a
    /// damaging type - so passing a negative amount to <see cref="TryApplyDamage(Actor, int, DamageStatDesc, Entity)"/>
    /// does not heal, it applies a nonsensical figure.
    ///
    /// Whatever is passed is still resisted, blocked and mitigated by the target. This deals a blow, it
    /// does not set an outcome; the health actually lost is decided by the target's own stats.
    /// </summary>
    public static class DamageAccess
    {
        /// <summary>
        /// Deals damage of a given type to an actor.
        ///
        /// **Leave <paramref name="source"/> null when the figure is already final**, which is the usual
        /// case for a mod that computed a number itself. A non-null source is not attribution alone: the
        /// game runs that entity's offensive stats over the value, adding its damage stat for this type
        /// and scaling by its damage factor. Naming an attacker whose gear is already reflected in the
        /// figure therefore counts their weapon twice.
        ///
        /// What a null source costs is only what the attacker's side of the blow would have carried: the
        /// aggro tag, the killed-by credit, the block punishment, and the crit and bleed rolls, which the
        /// game only makes for a source it recognizes as the player. Blocking still works - the block
        /// angle is only measured for projectiles - so the target's guard, stamina and perfect-block
        /// window all still decide the outcome.
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
                // Swallowed rather than thrown on, because the usual caller is inside a frame loop or a
                // Harmony patch: one blow that cannot be applied is a hit that did not land, and an
                // exception let out of there is every hit after it too.
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
            if (!TryGetPhysicalDamageType(out DamageStatDesc physical))
            {
                return false;
            }

            return TryApplyDamage(target, amount, physical, source);
        }

        /// <summary>
        /// Restores health, through the same calculation everything else goes through.
        ///
        /// Healing is not a negative blow; it is an ordinary health change carrying the healing type,
        /// which is what makes the game treat it as healing everywhere it matters - a dead actor is still
        /// refused, but an invincible one is healed rather than skipped.
        /// </summary>
        /// <param name="target">The actor to heal.</param>
        /// <param name="amount">How much to restore, as a positive magnitude.</param>
        /// <param name="source">Who healed them, or null. Unlike damage, a source here adds that entity's
        /// healing stats to the figure.</param>
        /// <returns>False when the actor has no health component or the global assets are not up yet.</returns>
        public static bool TryHeal(Actor target, int amount, Entity? source = null)
        {
            if (!TryGetHealingType(out DamageStatDesc healing))
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
        public static bool TryGetPhysicalDamageType(out DamageStatDesc damageType)
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
        public static bool TryGetMagicalDamageType(out DamageStatDesc damageType)
        {
            GameplaySettingsHandler? handler = ProviderAccess.GetGameplaySettingsHandler();
            damageType = handler != null ? handler.MagicalDamageStatDesc : null;

            return damageType != null;
        }

        /// <summary>
        /// The damage type nothing resists. Use it when a mod means exactly the number it passed and does
        /// not want the target's armor to argue with it - and note that the blockers inside the health
        /// calculation still run, which is the point of going through this at all.
        /// </summary>
        /// <param name="damageType">The type, or null when unavailable.</param>
        /// <returns>False while the game is still bootstrapping or no world is loaded.</returns>
        public static bool TryGetPureDamageType(out DamageStatDesc damageType)
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
        public static bool TryGetHealingType(out DamageStatDesc damageType)
        {
            GlobalAssetsGameHandler? handler = ProviderAccess.GetGlobalAssetsGameHandler();
            damageType = handler != null ? handler.HealingDamageType : null;

            return damageType != null;
        }
    }
}
