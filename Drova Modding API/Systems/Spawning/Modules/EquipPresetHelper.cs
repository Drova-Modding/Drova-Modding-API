using Il2CppDrova;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using Il2CppDrova.Talent;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    internal static class EquipPresetHelper
    {
        public static ActorEquipPreset EnsureInitialized(Inventory_StartupEquipSettings inventory)
        {
            inventory._equipPreset ??= ScriptableObject.CreateInstance<ActorEquipPreset>();

            var preset = inventory._equipPreset;
            preset._attributes ??= new Il2CppSystem.Collections.Generic.List<AttributeStats.InitialAttribute>();
            preset._equipment ??= new Il2CppSystem.Collections.Generic.List<Item>();
            preset._fallbackEquipment ??= new Il2CppSystem.Collections.Generic.List<Item>();
            preset._startTalents ??= new Il2CppSystem.Collections.Generic.List<TalentContainer>();
            preset._inventoryItems ??= new Il2CppSystem.Collections.Generic.List<ItemStack>();

            return preset;
        }
    }
}

