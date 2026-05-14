using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Drova_Modding_API.Access;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// A preset module that equips items (weapons, armor, etc.) to an NPC's inventory.
    /// </summary>
    public class EquipmentPresetModule : INpcModule
    {
        private readonly List<AssetReferenceT<Item>> _items = new();
        private readonly List<string> _readableIds = new();

        /// <summary>
        /// Adds an item to the equipment preset.
        /// </summary>
        /// <param name="item">The item reference to add</param>
        /// <returns>This preset for chaining</returns>
        public EquipmentPresetModule With(AssetReferenceT<Item> item)
        {
            _items.Add(item);
            return this;
        }

        /// <summary>
        /// Adds an item-readable id to the equipment preset.
        /// </summary>
        /// <param name="readableId">Readable id from the item database</param>
        /// <returns>This preset for chaining</returns>
        public EquipmentPresetModule With(string readableId)
        {
            if (!string.IsNullOrWhiteSpace(readableId))
                _readableIds.Add(readableId);

            return this;
        }

        /// <inheritdoc />
        public void Apply(GameObject npc)
        {
            var inventory = npc.GetComponentInChildren<Inventory_StartupEquipSettings>();
            if (inventory == null) return;

            inventory._equipPreset = ScriptableObject.CreateInstance<ActorEquipPreset>();

            foreach (var readableId in _readableIds)
            {
                var item = ProviderAccess.ItemDatabase.GetItemByReadableId(readableId);
                if (item == null)
                {
                    MelonLogger.Warning($"Could not resolve equipment readable id '{readableId}'.");
                    continue;
                }

                inventory._equipPreset._equipment.Add(item);
            }

            foreach (var itemRef in _items)
            {
                var handle = itemRef.LoadAssetAsync();
                handle.WaitForCompletion();
                if (handle.Result != null)
                    inventory._equipPreset._equipment.Add(handle.Result);
            }
        }
    }
}
