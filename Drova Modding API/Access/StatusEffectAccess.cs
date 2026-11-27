using System.Diagnostics.CodeAnalysis;
using Il2CppDrova;
using Il2CppDrova.Items;
using Il2CppDrova.StatusEffects;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Applies status effects the way food, potions and hits do. Effects come from the game's status effect
    /// database by GUID, because names are not unique (two honey buffs share one).
    /// An effect with a HUD icon is saved and restored with the save game on its own.
    /// </summary>
    public static class StatusEffectAccess
    {
        /// <summary>
        /// The status effect asset with the given GUID.
        /// </summary>
        /// <returns>False before the database is loaded or for an unknown GUID.</returns>
        public static bool TryGet(string guid, [NotNullWhen(true)] out StatusEffect? effect)
        {
            SubDatabase_StatusEffect? database = ProviderAccess.StatusEffectDatabase;
            effect = database != null ? database.GetStatusEffectByGuid(guid) : null;
            return effect != null;
        }

        /// <summary>
        /// Applies <paramref name="effect"/> to <paramref name="actor"/>. Eating, drinking and being hit go through the same call,
        /// so a food or potion effect heals and buffs exactly like the item.
        /// </summary>
        /// <param name="actor">Who gets the effect.</param>
        /// <param name="effect">The effect asset, see <see cref="TryGet"/>.</param>
        /// <param name="relatedItem">The item the HUD tooltip describes. Most food and potion effects have no text of their own.</param>
        /// <param name="stacks">Stacks to start with, or 0 for the effect's own amount.</param>
        /// <param name="seconds">Run time of the first stack, or 0 for the effect's own duration.</param>
        /// <returns>False when the game refused it: the actor is dead, dodging or immune.</returns>
        public static bool TryApply(Actor actor, StatusEffect effect, Item? relatedItem = null, int stacks = 0, float seconds = 0f)
        {
            return Apply(actor, effect, null, relatedItem, stacks, seconds);
        }

        /// <summary>
        /// <see cref="TryApply"/> on behalf of <paramref name="owner"/>: the damage the effect deals over time counts as theirs,
        /// for kill credit and for co-op, which reports a player's blows to the machine that owns the target.
        /// </summary>
        /// <param name="actor">Who gets the effect.</param>
        /// <param name="effect">The effect asset, see <see cref="TryGet"/>.</param>
        /// <param name="owner">Who applied it, usually the attacker.</param>
        /// <returns>False when the game refused it: the actor is dead, dodging or immune.</returns>
        public static bool TryApplyFrom(Actor actor, StatusEffect effect, Entity owner)
        {
            return Apply(actor, effect, owner, null, 0, 0f);
        }

        /// <summary>
        /// Makes <paramref name="actor"/> bleed for <paramref name="damage"/> in total over the bleed's duration.
        /// The bleed effect alone deals nothing, its damage comes from the bleed value a hit leaves behind.
        /// </summary>
        /// <returns>False when the gameplay settings are not loaded or the game refused the effect.</returns>
        public static bool TryApplyBleeding(Actor actor, int damage)
        {
            return Bleed(actor, damage, null);
        }

        /// <summary>
        /// <see cref="TryApplyBleeding"/> on behalf of <paramref name="owner"/>, whose damage the bleeding then counts as, like a
        /// crit's bleeding in the game.
        /// </summary>
        /// <returns>False when the gameplay settings are not loaded or the game refused the effect.</returns>
        public static bool TryApplyBleedingFrom(Actor actor, int damage, Entity owner)
        {
            return Bleed(actor, damage, owner);
        }

        private static bool Apply(Actor actor, StatusEffect effect, Entity? owner, Item? relatedItem, int stacks, float seconds)
        {
            StatusEffectHandler? handler = actor != null ? actor.GetStatusEffectHandler() : null;
            if (handler == null || effect == null)
            {
                return false;
            }

            try
            {
                // The effect is its own source, as for the save loader, bleeding and poison.
                SpawnInfo info = new(effect.Cast<IStatusEffectSource>());
                if (owner != null)
                {
                    info.SetOwner(owner);
                }
                if (relatedItem != null)
                {
                    info.RelatedItem = relatedItem;
                }
                if (stacks > 0)
                {
                    info.SetOverrideStacks(stacks);
                }
                if (seconds > 0f)
                {
                    info.SetOverrideRuntime(seconds);
                }

                handler.AddEffectFromSource(effect, info);
                return handler.HasStatusEffectApplied(effect);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[StatusEffectAccess] applying " + effect.name + " failed: " + e);
                return false;
            }
        }

        private static bool Bleed(Actor actor, int damage, Entity? owner)
        {
            GameplaySettingsHandler? settings = ProviderAccess.GetGameplaySettingsHandler();
            StatusEffectHandler? handler = actor != null ? actor.GetStatusEffectHandler() : null;
            if (settings == null || handler == null || settings.BleedingStatusEffect == null)
            {
                return false;
            }

            handler.AddBleedingValue(damage);
            return Apply(actor!, settings.BleedingStatusEffect, owner, null, 0, 0f);
        }
    }
}
