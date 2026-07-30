using System;
using System.Collections;
using Il2CppDrova;
using Il2CppDrova.LoadingScreenHandles;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Access to the player actor and its properties.
    /// </summary>
    public static class PlayerAccess
    {
        /// <summary>
        /// Event triggered when the player actor is found and initialized. Fires every load.
        /// </summary>
        public static event Action<Actor>? OnPlayerFound;

        private static object? _waitForPlayerCoroutine;

        /// <summary>
        /// Get the actor reference of the player. Can be null in menus.
        /// </summary>
        /// <returns></returns>
        public static Actor? GetPlayer()
        {
            EntityGameHandler entityGameHandler = ProviderAccess.GetEntityGameHandler();
           
            if (entityGameHandler != null)
            {
                return entityGameHandler.PlayerActor;
            }

            return null;
        }

        /// <summary>
        /// See <see cref="GetPlayer"/>
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool TryGetPlayer(out Actor player)
        {
            player = GetPlayer();
            return player != null;
        }

        /// <summary>
        /// Starts a MelonCoroutine to wait for the player actor until it is not null.
        /// Once found, <see cref="OnPlayerFound"/> will be triggered.
        /// </summary>
        public static void StartWaitForPlayerCoroutine()
        {
            if (_waitForPlayerCoroutine != null)
            {
                MelonCoroutines.Stop(_waitForPlayerCoroutine);
            }
            _waitForPlayerCoroutine = MelonCoroutines.Start(WaitForPlayer());
        }

        private static IEnumerator WaitForPlayer()
        {
            while (!LoadingScreenHandler.Instance.IsWorldReady() || SceneGameHandler.IsLoadingScreenActive) {
                yield return new WaitForSeconds(1);
            }
            while (GetPlayer() == null)
            {
                yield return null;
            }
            while (!GetPlayer()!._isInitialized)
            {
                yield return null;
            }
            OnPlayerFound?.Invoke(GetPlayer());
        }

        /// <summary>
        /// The primary weapon slot of the player.
        /// </summary>
        /// <returns></returns>
        public static ActorEquipSlot GetPrimarySlot()
        {
            if (TryGetPlayer(out Actor player))
            {
                Il2CppDrova.Items.EquipmentSlotType slotType = ProviderAccess.GetGameplaySettingsHandler().PrimarySlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// The secondary weapon slot of the player. If the weapon is a two handed weapon, this will be the same as the primary slot.
        /// </summary>
        /// <returns></returns>
        public static ActorEquipSlot GetSecondarySlot()
        {
            if (TryGetPlayer(out Actor player))
            {
                Il2CppDrova.Items.EquipmentSlotType slotType = ProviderAccess.GetGameplaySettingsHandler().SecondarySlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// The slingshot ammo slot of the player.
        /// </summary>
        /// <returns></returns>
        public static ActorEquipSlot GetSlingshotSlot()
        {
            if (TryGetPlayer(out Actor player))
            {
                Il2CppDrova.Items.EquipmentSlotType slingAmmoSlotType = ProviderAccess.GetGameplaySettingsHandler().SlingAmmoSlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slingAmmoSlotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// The bow ammo slot of the player.
        /// </summary>
        /// <returns></returns>
        public static ActorEquipSlot GetBowSlot()
        {
            if (TryGetPlayer(out Actor player))
            {
                Il2CppDrova.Items.EquipmentSlotType slingAmmoSlotType = ProviderAccess.GetGameplaySettingsHandler().BowAmmoSlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slingAmmoSlotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// Gets the player's attribute stats.
        /// </summary>
        /// <returns></returns>
        public static PlayerAttributeStats GetPlayerAttributeStats()
        {
            if (TryGetPlayer(out Actor player))
            {
                return player.GetStats().TryCast<PlayerAttributeStats>();
            }
            return null;
        }

        /// <summary>
        /// Reads the local player's character level.
        ///
        /// **Ask this rather than reaching for <see cref="GetPlayerAttributeStats"/>.** The level lives on
        /// the base <c>AttributeStats</c> rather than on the player subclass, so the cast that accessor
        /// performs is not needed to read it - and that cast is what makes the obvious version dangerous.
        /// It misses for a frame or two after a world loads, and a mod that answers a failed read with a
        /// default of one does not get an error, it gets a *plausible* level. Anything scaled by it
        /// silently collapses to its weakest form and nothing anywhere says so.
        ///
        /// A <c>Try</c> pair rather than a number, so "not ready yet" is a case the caller has to handle
        /// instead of a value it can mistake for a real level.
        /// </summary>
        /// <param name="level">The player's level, when there is a player whose stats have loaded.</param>
        /// <returns>False in menus, during a load, and for the first frames of a world.</returns>
        public static bool TryGetLevel(out int level)
        {
            if (TryGetPlayer(out Actor player))
            {
                return TryGetLevel(player, out level);
            }

            level = 0;
            return false;
        }

        /// <summary>
        /// Reads any actor's character level. See <see cref="TryGetLevel(out int)"/> for why this is a
        /// <c>Try</c> pair.
        ///
        /// The overload taking an actor exists because "which player" is a question the static one cannot
        /// ask, and in a co-op session there is more than one.
        /// </summary>
        /// <param name="actor">Whose level to read. Can be null.</param>
        /// <param name="level">Their level, when the actor has stats.</param>
        /// <returns>False when the actor is null or its stats have not loaded.</returns>
        public static bool TryGetLevel(Actor? actor, out int level)
        {
            level = 0;

            if (actor == null) return false;

            AttributeStats stats = actor.GetStats();
            if (stats == null) return false;

            level = stats.Level;
            return true;
        }
    }
}
