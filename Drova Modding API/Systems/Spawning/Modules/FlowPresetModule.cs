using Drova_Modding_API.Access;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Applies flow (spell) items to an NPC spell preset.
    /// </summary>
    public class FlowPresetModule : INpcModule
    {
        private const string DefaultSpellPresetName = "SpellPreset_T1";

        private static SpellEquipPreset? _cachedDefaultSpellPreset;
        private static bool _defaultSpellPresetLookupAttempted;

        private readonly HashSet<string> _readableIds = new();
        private readonly List<AssetReferenceT<Item>> _flows = new();
        private bool _useDefaultPresetTemplate;

        /// <summary>
        /// Enables initializing this module's spell preset from the default <c>SpellPreset_T1</c> template.
        /// The template is looked up and cached automatically on first use.
        /// </summary>
        /// <param name="enabled">True to use the default preset template; false to disable it.</param>
        /// <returns>This module for chaining.</returns>
        public FlowPresetModule UseDefaultPresetTemplate(bool enabled = true)
        {
            _useDefaultPresetTemplate = enabled;
            return this;
        }

        /// <summary>
        /// Optionally pre-caches the default spell preset used by <see cref="UseDefaultPresetTemplate"/>.
        /// </summary>
        public static void CacheDefaultSpellPreset()
        {
            _ = GetDefaultSpellPreset();
        }

        /// <summary>
        /// Adds a flow item by readable id.
        /// </summary>
        /// <param name="readableId">Flow readable id from the item database.</param>
        /// <returns>This module for chaining.</returns>
        public FlowPresetModule With(string readableId)
        {
            if (string.IsNullOrWhiteSpace(readableId)) return this;
            _readableIds.Add(readableId);
            return this;
        }

        /// <summary>
        /// Adds a flow item by addressable reference.
        /// </summary>
        /// <param name="flow">Addressable flow item reference.</param>
        /// <returns>This module for chaining.</returns>
        public FlowPresetModule With(AssetReferenceT<Item> flow)
        {
            _flows.Add(flow);
            return this;
        }

        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            var inventory = context.GetComponentInChildren<Inventory_StartupEquipSettings>();
            if (inventory == null) return;

            if (inventory._equipPreset == null)
            {
                inventory._equipPreset = ScriptableObject.CreateInstance<ActorEquipPreset>();
            }

            if (inventory._equipPreset._spellEquipPreset == null)
            {
                var spellPreset = ScriptableObject.CreateInstance<SpellEquipPreset>();
                InitializeSpellPreset(spellPreset);
                inventory._equipPreset._spellEquipPreset = spellPreset;
            }

            var pendingFlows = new List<Item>();

            foreach (var readableId in _readableIds)
            {
                var item = ProviderAccess.ItemDatabase.GetItemByReadableId(readableId);
                if (item == null)
                {
                    MelonLogger.Warning($"Could not resolve flow readable id '{readableId}'.");
                    continue;
                }

                pendingFlows.Add(item);
            }

            foreach (var itemRef in _flows)
            {
                var handle = itemRef.LoadAssetAsync();
                handle.WaitForCompletion();

                if (handle.Result == null)
                {
                    MelonLogger.Warning($"Could not load flow from addressable reference '{itemRef.AssetGUID}'.");
                    continue;
                }

                pendingFlows.Add(handle.Result);
            }

            var slots = inventory._equipPreset._spellEquipPreset._slots;
            if (slots == null || slots.Count == 0)
            {
                InitializeSpellPreset(inventory._equipPreset._spellEquipPreset);
                slots = inventory._equipPreset._spellEquipPreset._slots;
                if (slots == null || slots.Count == 0) return;
            }

            var slotIndex = 0;
            foreach (var flow in pendingFlows)
            {
                while (slotIndex < slots.Count && slots[slotIndex].SpellItem != null)
                {
                    slotIndex++;
                }

                if (slotIndex >= slots.Count)
                {
                    MelonLogger.Warning("Not enough spell slots available to apply all configured flows.");
                    break;
                }

                slots[slotIndex].SpellItem = flow;
                slotIndex++;
            }
        }

        private void InitializeSpellPreset(SpellEquipPreset spellPreset)
        {
            if (_useDefaultPresetTemplate)
            {
                InitializeWithDefaultPreset(spellPreset);
                return;
            }

            spellPreset.InitSpellSlots();
        }

        private static SpellEquipPreset? GetDefaultSpellPreset()
        {
            if (_cachedDefaultSpellPreset != null) return _cachedDefaultSpellPreset;
            if (_defaultSpellPresetLookupAttempted) return null;

            _defaultSpellPresetLookupAttempted = true;
            var allPresets = Resources.FindObjectsOfTypeAll<SpellEquipPreset>();
            for (var i = 0; i < allPresets.Length; i++)
            {
                var preset = allPresets[i];
                if (preset != null && preset.name == DefaultSpellPresetName)
                {
                    _cachedDefaultSpellPreset = preset;
                    break;
                }
            }

            if (_cachedDefaultSpellPreset == null)
            {
                MelonLogger.Warning($"Could not find default spell preset '{DefaultSpellPresetName}'. Falling back to InitSpellSlots().");
            }

            return _cachedDefaultSpellPreset;
        }

        /// <summary>
        /// Initializes a spell preset using the cached default SpellPreset_T1 slot layout.
        /// Falls back to InitSpellSlots when the preset cannot be resolved.
        /// </summary>
        private static void InitializeWithDefaultPreset(SpellEquipPreset spellPreset)
        {
            // Always let game code create and initialize the slot list.
            spellPreset.InitSpellSlots();

            var defaultPreset = GetDefaultSpellPreset();
            var sourceSlots = defaultPreset?._slots;
            var targetSlots = spellPreset._slots;

            if (sourceSlots == null || sourceSlots.Count == 0 || targetSlots == null || targetSlots.Count == 0)
            {
                return;
            }

            var count = Mathf.Min(sourceSlots.Count, targetSlots.Count);
            for (var i = 0; i < count; i++)
            {
                // Mirror the default preset layout and default flow item per slot.
                targetSlots[i].Slot = sourceSlots[i].Slot;
                targetSlots[i].SpellItem = sourceSlots[i].SpellItem;
            }
        }
    }
}