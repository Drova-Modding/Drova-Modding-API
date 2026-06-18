using System;
using System.Collections;
using Il2CppDrova;
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
        /// Event triggered when the player actor is found.
        /// </summary>
        public static event Action<Actor>? OnPlayerFound;

        /// <summary>
        /// Get the actor reference of the player. Can be null in menus.
        /// </summary>
        /// <returns></returns>
        public static Actor GetPlayer()
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
            MelonCoroutines.Start(WaitForPlayer());
        }

        private static IEnumerator WaitForPlayer()
        {
            while (GetPlayer() == null)
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
    }
}
