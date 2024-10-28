using Il2CppDrova;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Access to the player actor and its properties.
    /// </summary>
    public class PlayerAccess
    {
        /// <summary>
        /// Get the actor reference of the player. Can be null in menus.
        /// </summary>
        /// <returns></returns>
        public static Actor GetPlayer()
        {
            var entityGameHandler = ProviderAccess.GetEntityGameHandler();
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
        /// The primary weapon slot of the player.
        /// </summary>
        /// <returns></returns>
        public static ActorEquipSlot GetPrimarySlot()
        {
            if (TryGetPlayer(out var player))
            {
                var slotType = ProviderAccess.GetGameplaySettingsHandler().PrimarySlot;
                var ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slotType);

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
            if (TryGetPlayer(out var player))
            {
                var slotType = ProviderAccess.GetGameplaySettingsHandler().SecondarySlot;
                var ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slotType);

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
            if (TryGetPlayer(out var player))
            {
                var slingAmmoSlotType = ProviderAccess.GetGameplaySettingsHandler().SlingAmmoSlot;
                var ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slingAmmoSlotType);

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
            if (TryGetPlayer(out var player))
            {
                var slingAmmoSlotType = ProviderAccess.GetGameplaySettingsHandler().BowAmmoSlot;
                var ammoSlot = player.GetEquipmentModule().GetFirstSlotInActiveSet(slingAmmoSlotType);

                return ammoSlot;
            }

            return null;
        }
    }
}
