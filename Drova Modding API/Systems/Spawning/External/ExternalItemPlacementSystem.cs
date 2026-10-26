using System.Text.Json;
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Systems.Quests;
using Il2CppDrova;
using Il2CppDrova.InteractionSystem;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using Il2CppDrova.Saveables;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Places items in the world as copies of the game's <c>Kiepe</c> pickup with a stable guid.
    /// <see cref="Saveable_PickUp_Once"/> keeps a picked item gone in that save.
    /// </summary>
    public static class ExternalItemPlacementSystem
    {
        private const string FolderName = "ItemPlacement";
        private const string PickupAssetId = "Kiepe";
        private const string PickupNamePrefix = "Modding_API_Pickup_";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly Dictionary<string, ExternalItemDefinition> DefinitionsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, GameObject> SpawnedById = new(StringComparer.OrdinalIgnoreCase);
        private static GameObject? _holder;
        private static Scene _scene;

        /// <summary>
        /// Absolute path of the folder item files are read from.
        /// </summary>
        public static string GetFolderPath() => Path.Combine(Utils.SavePath, FolderName);

        /// <summary>
        /// The item definitions read at the last world load, by id.
        /// </summary>
        public static IReadOnlyDictionary<string, ExternalItemDefinition> Definitions => DefinitionsById;

        /// <summary>
        /// The save guid of a placed item.
        /// </summary>
        public static string GetPickupGuid(string id) => LazyActorCreator.StableGuid("Modding_API.ItemPickup", id);

        internal static void Initialize()
        {
            GVarConditions.Changed += Refresh;
            GameEvents.OnLootedAll += OnLootedAll;
        }

        /// <summary>
        /// Reads every item file and places the items. Runs on each world load.
        /// </summary>
        internal static void SpawnConfiguredItems(Scene scene)
        {
            DefinitionsById.Clear();
            SpawnedById.Clear();
            _scene = scene;
            string folder = GetFolderPath();
            Directory.CreateDirectory(folder);

            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    ExternalItemConfigFile? config = JsonSerializer.Deserialize<ExternalItemConfigFile>(File.ReadAllText(file), JsonOptions);
                    foreach (ExternalItemDefinition definition in config?.Items ?? [])
                    {
                        if (!string.IsNullOrWhiteSpace(definition.Id))
                        {
                            DefinitionsById[definition.Id] = definition;
                        }
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[Items] Could not read " + file + ": " + e.Message);
                }
            }

            Refresh();
        }

        private static void Refresh()
        {
            if (DefinitionsById.Count == 0 || !_scene.isLoaded)
            {
                return;
            }

            if (!GameDatabase.Instance.DynamicObjects.TryGetPrefab(PickupAssetId, out SaveRoot_Dynamic template) || template == null)
            {
                MelonLogger.Warning($"[Items] The game database has no '{PickupAssetId}' pickup, no items placed.");
                return;
            }

            foreach (ExternalItemDefinition definition in DefinitionsById.Values)
            {
                // A taken pickup keeps its destroyed entry, so it is not placed again in this world.
                bool known = SpawnedById.TryGetValue(definition.Id, out GameObject? spawned);
                bool wanted = definition.Enabled && GVarConditions.AreMetForPlacement(definition.Conditions);
                if (wanted && !known)
                {
                    GameObject? created = Spawn(definition, template, _scene);
                    if (created != null)
                    {
                        SpawnedById[definition.Id] = created;
                    }
                }
                else if (!wanted && spawned != null)
                {
                    Object.Destroy(spawned);
                    SpawnedById.Remove(definition.Id);
                }
            }
        }

        private static void OnLootedAll(Interact_Bhvr_LootAll lootAll)
        {
            SaveRoot_Dynamic? root = lootAll.GetComponentInParent<SaveRoot_Dynamic>();
            if (root == null || !root.name.StartsWith(PickupNamePrefix, StringComparison.Ordinal)
                || !DefinitionsById.TryGetValue(root.name[PickupNamePrefix.Length..], out ExternalItemDefinition? definition)
                || string.IsNullOrWhiteSpace(definition.TakenGVarList) || string.IsNullOrWhiteSpace(definition.TakenGBool))
            {
                return;
            }

            GVarConditions.TrySetBool(definition.TakenGVarList, definition.TakenGBool, true);
        }

        private static GameObject? Spawn(ExternalItemDefinition definition, SaveRoot_Dynamic template, Scene scene)
        {
            Item? item = ProviderAccess.ItemDatabase?.GetItemByReadableId(definition.Item);
            if (item == null)
            {
                MelonLogger.Warning($"[Items] '{definition.Id}': unknown item '{definition.Item}'.");
                return null;
            }

            if (_holder == null)
            {
                _holder = new GameObject("Modding_API_PickupHolder");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
            }

            // Copied under the inactive holder, so nothing in it wakes up before it carries its own guid and loot.
            SaveRoot_Dynamic pickup = Object.Instantiate(template, _holder.transform);
            pickup.name = PickupNamePrefix + definition.Id;

            // With a guid the save root keys its state by it and stays out of the game's own respawn list.
            string guid = GetPickupGuid(definition.Id);
            GuidComponent guidComponent = pickup._guidComponent != null ? pickup._guidComponent : pickup.gameObject.AddComponent<GuidComponent>();
            guidComponent._guid = new Il2CppSystem.Guid(guid);
            guidComponent._guidString = guid;
            pickup._guidComponent = guidComponent;

            Interact_Bhvr_LootAll? lootAll = pickup.GetComponentInChildren<Interact_Bhvr_LootAll>(true);
            if (lootAll == null || lootAll._loot == null)
            {
                MelonLogger.Warning($"[Items] The '{PickupAssetId}' pickup has no loot.");
                Object.Destroy(pickup.gameObject);
                return null;
            }

            // A guid opts out of the save's dynamic list, so the scene pickup state remembers the loot-all instead.
            Saveable_PickUp_Once once = pickup.gameObject.AddComponent<Saveable_PickUp_Once>();
            once._pickUpRoot = pickup.gameObject;
            once._lootAll = lootAll;

            Inventory loot = lootAll._loot;
            loot.InventoryItems.Clear();
            loot.InventoryItems.Add(new ItemStack(item, Math.Max(1, definition.Amount)));

            // The basket's own sprite becomes the item's icon, and the basket shadow goes.
            foreach (SpriteRenderer renderer in pickup.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == "Visual" && item.Icon != null)
                {
                    renderer.sprite = item.Icon;
                }
                else if (renderer.name == "Shadow")
                {
                    renderer.enabled = false;
                }
            }

            pickup.transform.position = new Vector3(definition.X, definition.Y, 0f);
            pickup.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(pickup.gameObject, scene);
            return pickup.gameObject;
        }
    }
}
