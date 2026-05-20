using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.SaveGame.Store;
using Drova_Modding_API.Systems.Spawning.Modules;
using Il2CppDrova;
using Il2CppDrova.Alignment;
using Il2CppDrova.Saveables;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

        private static bool _templatePrewarmed;

        /// <summary>
        /// Pre-loads the Human NPC template and caches alignment lookups so the first
        /// <see cref="Create"/> call doesn't stall on Addressables I/O. Safe to call multiple
        /// times — later invocations are no-ops. Call once during mod startup.
        /// </summary>
        public static void Prewarm()
        {
            CacheAlignments();

            if (_templatePrewarmed) return;
            // LoadAssetAsync keeps the GameObject template resident; later InstantiateAsync
            // calls resolve from cache instead of hitting disk/AssetBundle.
            AddressableAccess.NPCs.Human_Template.LoadAssetAsync();
            _templatePrewarmed = true;
        }

        private readonly string _name;
        private readonly Vector2 _spawnPosition;
        private readonly List<INpcModule> _modules = new();

        private readonly AssetReferenceGameObject _lazyActorReference = AddressableAccess.NPCs.Human_Template;
        private AssetReference? _lazyEntityInfoReference = AddressableAccess.EntityInfos.EntityInfo_Bandit;
        private EntityInfo? _customLazyEntityInfo;

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

        /// <summary>
        /// Sets the entity info reference used by <see cref="CreateLazy"/>.
        /// </summary>
        public NpcCreator WithLazyEntityInfo(AssetReference entityInfoReference)
        {
            _lazyEntityInfoReference = entityInfoReference;
            _customLazyEntityInfo = null;
            return this;
        }

        /// <summary>
        /// Sets a preloaded custom entity info used by <see cref="CreateLazy"/>.
        /// </summary>
        public NpcCreator WithLazyEntityInfo(EntityInfo entityInfo)
        {
            _customLazyEntityInfo = entityInfo;
            return this;
        }

        // ── Convenience helpers ────────────────────────────────────────────────

        /// <summary>
        /// Adds a cosmetic item to the NPC (hair, beard, etc.).
        /// Multiple calls reuse the same <see cref="CosmeticsPresetModule"/> module.
        /// </summary>
        public NpcCreator WithCosmetic(AssetReferenceT<Il2CppDrova.Items.Item> item)
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
        /// <param name="readableId">The item-readable ID from <see cref="GlobalFields.ItemReadableIds"/>.</param>
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
        public NpcCreator WithItem(AssetReferenceT<Il2CppDrova.Items.Item> item)
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
        /// <param name="readableId">The item-readable ID from <see cref="GlobalFields.ItemReadableIds"/>.</param>
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
        /// Sets whether the NPC is friendly to the player.
        /// Passing <c>false</c> applies the cached <c>Alignment_Wild_Human</c> container
        /// (falls back to a live search if <see cref="CacheAlignments"/> was not called beforehand).
        /// Passing <c>true</c> removes any previously added alignment override so the
        /// NPC keeps its template/default alignment.
        /// </summary>
        public NpcCreator IsPlayerFriendly(bool isPlayerFriendly)
        {
            _modules.RemoveAll(module => module is AlignmentModule);

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
            ApplyModules(npc);
            return npc;
        }

        /// <summary>
        /// Creates a lazy NPC and applies modules every time the runtime actor is loaded.
        /// </summary>
        /// <param name="saveToLazyActorStore">If true, writes lazy actor metadata to the save store for restore.</param>
        /// <returns>The configured lazy actor handle.</returns>
        public LazyActor CreateLazy(bool saveToLazyActorStore = false)
        {
            LazyActor lazyActor = LazyActorCreator.CreateLazyActor(_name, _lazyActorReference, _spawnPosition, _lazyEntityInfoReference, _customLazyEntityInfo, true);

            // Register a pre-init callback so module logic runs before Actor.StartInitAsync.
            // We cannot use ActorSpawnEvent because SpawnArgs is a non-blittable struct and
            // Il2CppInterop refuses to convert the delegate.
            LazyActorPreInitRegistry.Register(lazyActor, OnActorLoaded);

            if (!saveToLazyActorStore)
            {
                return lazyActor;
            }

            if (_lazyEntityInfoReference == null)
            {
                MelonLogger.Warning("NpcCreator.CreateLazy(saveToLazyActorStore: true) requires an entity-info addressable reference. Use WithLazyEntityInfo(AssetReference) when saving is needed.");
                return lazyActor;
            }

            IStore<LazyActorSaveData> lazyActorStore = SaveGameSystem.Instance.GetStore<LazyActorSaveData>();
            lazyActorStore.Add(new LazyActorSaveData(_name, _lazyActorReference.AssetGUID, _lazyEntityInfoReference.AssetGUID, lazyActor._guidstring));
            return lazyActor;

            void OnActorLoaded(Actor actor)
            {
                if (actor == null)
                    return;
                ApplyModules(actor.gameObject);
            }
        }

        private void ApplyModules(GameObject npc)
        {
            MelonLogger.Msg("Applying modules");
            var context = new ModuleContext(npc);
            INpcModule[] modules = GetOrderedModules();
            for (int i = 0; i < modules.Length; i++)
                modules[i].Apply(context);
        }

        private INpcModule[] GetOrderedModules()
        {
            int count = _modules.Count;
            if (count <= 1)
                return _modules.ToArray();

            bool needsSort = false;
            for (int i = 1; i < count; i++)
            {
                if (_modules[i].Priority < _modules[i - 1].Priority) { needsSort = true; break; }
            }

            if (!needsSort)
                return [.. _modules];

            // LINQ OrderBy is stable, preserving insertion order for equal priorities.
            return _modules.OrderBy(m => m.Priority).ToArray();
        }

        /// <summary>
        /// Convenience factory — creates an NPC with default settings.
        /// </summary>
        /// <param name="name">The name of the Actor</param>
        /// <param name="spawnPosition">Where to spawn it</param>
        public static GameObject CreateNpc(string name, Vector2 spawnPosition)
            => new NpcCreator(name, spawnPosition).Create();

        /// <summary>
        /// Convenience factory — creates a lazy NPC with default settings.
        /// </summary>
        public static LazyActor CreateLazyNpc(string name, Vector2 spawnPosition, bool saveToLazyActorStore = false)
            => new NpcCreator(name, spawnPosition).CreateLazy(saveToLazyActorStore);
    }
}
