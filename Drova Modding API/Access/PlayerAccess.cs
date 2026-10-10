using System.Collections;
using System.Diagnostics.CodeAnalysis;
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
        private static object? _waitForPlayerCoroutine;

        /// <summary>
        /// Event triggered when the player actor is found and initialized. Fires every load.
        /// </summary>
        public static event Action<Actor>? OnPlayerFound;

        /// <summary>
        /// Get the actor reference of the player. Can be null in menus.
        /// </summary>
        public static Actor? GetPlayer()
        {
            EntityGameHandler? entityGameHandler = ProviderAccess.GetEntityGameHandler();
           
            if (entityGameHandler != null)
            {
                return entityGameHandler.PlayerActor;
            }

            return null;
        }

        /// <summary>
        /// See <see cref="GetPlayer"/>.
        /// </summary>
        /// <param name="player">The player actor, null in menus.</param>
        /// <returns>False in menus.</returns>
        public static bool TryGetPlayer([NotNullWhen(true)] out Actor? player)
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
        public static ActorEquipSlot? GetPrimarySlot()
        {
            GameplaySettingsHandler? settings = ProviderAccess.GetGameplaySettingsHandler();
            if (settings != null && TryGetPlayer(out Actor? player))
            {
                Il2CppDrova.Items.EquipmentSlotType slotType = settings.PrimarySlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// The secondary weapon slot of the player. If the weapon is a two handed weapon, this will be the same as the primary slot.
        /// </summary>
        public static ActorEquipSlot? GetSecondarySlot()
        {
            GameplaySettingsHandler? settings = ProviderAccess.GetGameplaySettingsHandler();
            if (settings != null && TryGetPlayer(out Actor? player))
            {
                Il2CppDrova.Items.EquipmentSlotType slotType = settings.SecondarySlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// The slingshot ammo slot of the player.
        /// </summary>
        public static ActorEquipSlot? GetSlingshotSlot()
        {
            GameplaySettingsHandler? settings = ProviderAccess.GetGameplaySettingsHandler();
            if (settings != null && TryGetPlayer(out Actor? player))
            {
                Il2CppDrova.Items.EquipmentSlotType slingAmmoSlotType = settings.SlingAmmoSlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slingAmmoSlotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// The bow ammo slot of the player.
        /// </summary>
        public static ActorEquipSlot? GetBowSlot()
        {
            GameplaySettingsHandler? settings = ProviderAccess.GetGameplaySettingsHandler();
            if (settings != null && TryGetPlayer(out Actor? player))
            {
                Il2CppDrova.Items.EquipmentSlotType slingAmmoSlotType = settings.BowAmmoSlot;
                ActorEquipSlot ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slingAmmoSlotType);

                return ammoSlot;
            }

            return null;
        }

        /// <summary>
        /// Gets the player's attribute stats.
        /// </summary>
        public static PlayerAttributeStats? GetPlayerAttributeStats()
        {
            if (TryGetPlayer(out Actor? player))
            {
                return player.GetStats().TryCast<PlayerAttributeStats>();
            }
            return null;
        }

        /// <summary>
        /// Reads the local player's character level. The stats miss for a frame or two after a world loads, so
        /// handle false instead of defaulting to level one.
        /// </summary>
        /// <param name="level">The player's level, when there is a player whose stats have loaded.</param>
        /// <returns>False in menus, during a load, and for the first frames of a world.</returns>
        public static bool TryGetLevel(out int level)
        {
            if (TryGetPlayer(out Actor? player))
            {
                return TryGetLevel(player, out level);
            }

            level = 0;
            return false;
        }

        /// <summary>
        /// Reads any actor's character level, e.g. a co-op teammate's. See <see cref="TryGetLevel(out int)"/>.
        /// </summary>
        /// <param name="actor">Whose level to read. Can be null.</param>
        /// <param name="level">Their level, when the actor has stats.</param>
        /// <returns>False when the actor is null or its stats have not loaded.</returns>
        public static bool TryGetLevel(Actor? actor, out int level)
        {
            level = 0;

            if (actor == null) return false;

            AttributeStats? stats = actor.GetStats();
            if (stats == null) return false;

            level = stats.Level;
            return true;
        }
    }
}
