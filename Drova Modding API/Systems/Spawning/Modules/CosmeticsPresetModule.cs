using Il2CppDrova.InventorySystem.Cosmetics;
using Il2CppDrova.Items;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Drova_Modding_API.Access;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// A preset module that applies cosmetic items (hair, beard, etc.) to an NPC.
    /// </summary>
    public class CosmeticsPresetModule : INpcModule
    {
        private readonly List<AssetReferenceT<Item>> _cosmetics = new();
        private readonly List<string> _readableIds = new();

        /// <summary>
        /// Adds a cosmetic item to the preset.
        /// </summary>
        /// <param name="item">The cosmetic item reference to add</param>
        /// <returns>This preset for chaining</returns>
        public CosmeticsPresetModule With(AssetReferenceT<Item> item)
        {
            _cosmetics.Add(item);
            return this;
        }

        /// <summary>
        /// Adds a cosmetic item readable id to the preset.
        /// </summary>
        /// <param name="readableId">Readable id from the item database</param>
        /// <returns>This preset for chaining</returns>
        public CosmeticsPresetModule With(string readableId)
        {
            if (!string.IsNullOrWhiteSpace(readableId))
                _readableIds.Add(readableId);

            return this;
        }

        /// <inheritdoc />
        public void Apply(GameObject npc)
        {
            var inventory = npc.GetComponentInChildren<Il2CppDrova.InventorySystem.Inventory_StartupEquipSettings>();
            if (inventory == null) return;

            inventory._cosmeticPreset = ScriptableObject.CreateInstance<CosmeticPreset>();

            foreach (var readableId in _readableIds)
            {
                var item = ProviderAccess.ItemDatabase.GetItemByReadableId(readableId);
                if (item == null)
                {
                    MelonLogger.Warning($"Could not resolve cosmetic readable id '{readableId}'.");
                    continue;
                }

                inventory._cosmeticPreset._cosmeticItems.Add(item);
            }

            foreach (var cosmeticRef in _cosmetics)
            {
                var handle = cosmeticRef.LoadAssetAsync();
                handle.WaitForCompletion();
                if (handle.Result != null)
                    inventory._cosmeticPreset._cosmeticItems.Add(handle.Result);
            }
        }
    }
}
