using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Spawning.Modules;
using Il2CppDrova.Alignment;
using System.Collections.Generic;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Class that helps create NPCs by composing <see cref="INpcModule"/> presets.
    /// No module logic runs until <see cref="Create"/> is called.
    /// </summary>
    public class NpcCreator
    {
        private const string EnemyAlignmentName = "Alignment_Wild_Human";

        // Cached once at first use — FindObjectsOfTypeAll is expensive.
        private static AlignmentContainer? _cachedEnemyAlignment;

        /// <summary>
        /// Pre-caches the enemy alignment container. Call this once during mod startup
        /// so the first <see cref="IsPlayerFriendly"/> call pays no search cost.
        /// </summary>
        public static void CacheAlignments()
        {
            if (_cachedEnemyAlignment != null) return;
            var all = Resources.FindObjectsOfTypeAll<AlignmentContainer>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].name == EnemyAlignmentName)
                {
                    _cachedEnemyAlignment = all[i];
                    break;
                }
            }
        }

        private readonly string _name;
        private readonly Vector2 _spawnPosition;
        private readonly List<INpcModule> _modules = new();

        // Single shared preset instances — reused across calls on this builder.
        private CosmeticsPresetModule? _cosmeticsPreset;
        private EquipmentPresetModule? _equipmentPreset;

        /// <summary>
        /// Initialize a new NpcCreator.
        /// </summary>
        /// <param name="name">The name of the NPC</param>
        /// <param name="spawnPosition">The position to spawn the NPC</param>
        public NpcCreator(string name, Vector2 spawnPosition)
        {
            _name = name;
            _spawnPosition = spawnPosition;
        }

        /// <summary>
        /// Adds an <see cref="INpcModule"/> to be applied when the NPC is created.
        /// </summary>
        /// <param name="module">The module to add</param>
        /// <returns>This builder for chaining</returns>
        public NpcCreator WithModule(INpcModule module)
        {
            _modules.Add(module);
            return this;
        }

        // ── Convenience helpers ────────────────────────────────────────────────

        /// <summary>
        /// Adds a cosmetic item to the NPC (hair, beard, etc.).
        /// Multiple calls reuse the same <see cref="CosmeticsPresetModule"/> module.
        /// </summary>
        public NpcCreator WithCosmetic(UnityEngine.AddressableAssets.AssetReferenceT<Il2CppDrova.Items.Item> item)
        {
            if (_cosmeticsPreset == null)
            {
                _cosmeticsPreset = new CosmeticsPresetModule();
                _modules.Add(_cosmeticsPreset);
            }
            _cosmeticsPreset.With(item);
            return this;
        }

        /// <summary>
        /// Adds a cosmetic item to the NPC by readable ID (hair, beard, helmet, etc.).
        /// Multiple calls reuse the same <see cref="CosmeticsPresetModule"/> module.
        /// </summary>
        /// <param name="readableId">The item readable ID from <see cref="GlobalFields.ItemReadableIds"/>.</param>
        public NpcCreator WithCosmetic(string readableId)
        {
            if (_cosmeticsPreset == null)
            {
                _cosmeticsPreset = new CosmeticsPresetModule();
                _modules.Add(_cosmeticsPreset);
            }
            _cosmeticsPreset.With(readableId);
            return this;
        }

        /// <summary>
        /// Adds an equipment item to the NPC inventory.
        /// Multiple calls reuse the same <see cref="EquipmentPresetModule"/> module.
        /// </summary>
        public NpcCreator WithItem(UnityEngine.AddressableAssets.AssetReferenceT<Il2CppDrova.Items.Item> item)
        {
            if (_equipmentPreset == null)
            {
                _equipmentPreset = new EquipmentPresetModule();
                _modules.Add(_equipmentPreset);
            }
            _equipmentPreset.With(item);
            return this;
        }

        /// <summary>
        /// Adds an equipment item to the NPC inventory by readable ID.
        /// Multiple calls reuse the same <see cref="EquipmentPresetModule"/> module.
        /// </summary>
        /// <param name="readableId">The item readable ID from <see cref="GlobalFields.ItemReadableIds"/>.</param>
        public NpcCreator WithItem(string readableId)
        {
            if (_equipmentPreset == null)
            {
                _equipmentPreset = new EquipmentPresetModule();
                _modules.Add(_equipmentPreset);
            }
            _equipmentPreset.With(readableId);
            return this;
        }

        /// <summary>
        /// Sets the NPC friendly or foe to the player.
        /// When <paramref name="isPlayerFriendly"/> is <c>false</c> the
        /// cached <c>Alignment_Wild_Human</c> container is applied (falls back to a
        /// live search if <see cref="CacheAlignments"/> was not called beforehand).
        /// </summary>
        public NpcCreator IsPlayerFriendly(bool isPlayerFriendly)
        {
            if (!isPlayerFriendly)
            {
                // Use cache; populate lazily if CacheAlignments() was never called.
                if (_cachedEnemyAlignment == null) CacheAlignments();
                if (_cachedEnemyAlignment != null)
                    WithModule(new AlignmentModule(_cachedEnemyAlignment));
            }
            return this;
        }

        // ── Build ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates the NPC and applies all registered modules in order.
        /// </summary>
        /// <returns>The fully configured NPC <see cref="GameObject"/></returns>
        public GameObject Create()
        {
            var operation = AddressableAccess.NPCs.Human_Template.InstantiateAsync(_spawnPosition, Quaternion.identity);
            operation.WaitForCompletion();

            GameObject npc = operation.Result;
            npc.name = _name;

            var context = new ModuleContext(npc);
            var orderedModules = _modules
                .Select((module, index) => new { module, index })
                .OrderBy(item => item.module.Priority)
                .ThenBy(item => item.index)
                .Select(item => item.module);

            foreach (var module in orderedModules)
                module.Apply(context);

            return npc;
        }

        /// <summary>
        /// Convenience factory — creates an NPC with default settings.
        /// </summary>
        /// <param name="name">The name of the Actor</param>
        /// <param name="spawnPosition">Where to spawn it</param>
        public static GameObject CreateNpc(string name, Vector2 spawnPosition)
            => new NpcCreator(name, spawnPosition).Create();
    }
}
