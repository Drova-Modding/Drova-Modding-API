using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Drova_Modding_API.Access;
using Il2Cpp;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;
using Il2CppDrova.Crimes;
using Il2CppDrova.HitSystem;
using Il2CppDrova.InteractionSystem;
using Il2CppDrova.Data;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using Il2CppDrova.Saveables;
using Il2CppOpenWorldSystem;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Il2CppGuid = Il2CppSystem.Guid;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// Applies the layouts of <c>Mods/Modding_API/MapEditor/*.json</c>: places props and invisible blockers near the
    /// player, releases far ones again like the game's own chunk streaming, and hides vanilla objects as their scenes load.
    /// The navmesh is updated around solid colliders as they come and go, so NPC paths go around them.
    /// </summary>
    public static class MapLayoutSystem
    {
        private const string FolderName = "MapEditor";
        private const string EditorFileName = "map_editor.json";
        private const string BlockerId = "Blocker";
        private const string GuidScope = "Modding_API.Map";
        private const string ConsumedSaveKey = "DrovaModdingAPI_MapConsumed";
        private const string BlockerLayer = "Obstacle";
        private const float SpawnRange = 1600f;
        private const float ReleaseRange = 2400f;
        private const float CheckSeconds = 0.5f;
        private const float NavmeshMargin = 32f;
        private const int MaxUndoSteps = 50;
        private const string UndoFileName = "map_editor.undo";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly Dictionary<string, MapObjectDefinition> ObjectsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> FileById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, GameObject> InstancesById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, AsyncOperationHandle<GameObject>> HandlesById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> FailedIds = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<AsyncOperationHandle<GameObject>> Abandoned = [];
        private static readonly List<(MapRemovalDefinition Removal, string File)> Removals = [];
        private static readonly List<(MapAreaDefinition Area, string File)> AreaEntries = [];
        private static List<string?[]>? _undoStack;
        private static readonly Dictionary<string, (GameObject Target, OW_ActivatedObject.EActivation Activation)> HiddenByKey = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Bounds> CutById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ConsumedIds = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> SourceSceneById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> LoadingIds = [];
        private static readonly List<string> DestroyedIds = [];
        private static readonly List<Vector2> Origins = [];
        private static readonly HashSet<string> PickupIds = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, AsyncOperationHandle<GameObject>> StampPrefabs = new(StringComparer.OrdinalIgnoreCase);
        private static GameObject? _root;
        private static float _nextCheck;

        static MapLayoutSystem()
        {
            SceneStreamAccess.OnSceneLoaded += (_, scene) => ApplyRemovals(scene);
        }

        /// <summary>
        /// The game destroyed a placed object itself (a sprung trap, a taken pickup), with the object's id and whether it was a
        /// pickup. Raised on the main thread. A co-op mod can pass it on and call <see cref="MarkConsumed"/> on the other machines.
        /// </summary>
        public static event Action<string, bool>? OnConsumed;

        /// <summary>
        /// Absolute path of the folder layout files are read from.
        /// </summary>
        public static string GetFolderPath() => Path.Combine(Utils.SavePath, FolderName);

        /// <summary>
        /// Every placed prop and blocker, by id.
        /// </summary>
        public static IReadOnlyDictionary<string, MapObjectDefinition> Objects => ObjectsById;

        /// <summary>
        /// Every hidden vanilla object, oldest first.
        /// </summary>
        public static IReadOnlyList<MapRemovalDefinition> Removed => Removals.Select(entry => entry.Removal).ToList();

        /// <summary>
        /// How many vanilla objects are hidden, without copying the list.
        /// </summary>
        internal static int RemovedCount => Removals.Count;

        /// <summary>
        /// Shows the most recently hidden vanilla object again.
        /// </summary>
        internal static void UnhideLast()
        {
            if (Removals.Count > 0)
            {
                Unhide(Removals[^1].Removal);
            }
        }

        private static string EditorFile => Path.Combine(GetFolderPath(), EditorFileName);

        /// <summary>
        /// The live object of a prop, when it is near the player and loaded.
        /// </summary>
        public static bool TryGetInstance(string id, [NotNullWhen(true)] out GameObject? instance)
        {
            InstancesById.TryGetValue(id, out instance);
            return instance != null;
        }

        /// <summary>
        /// Whether <see cref="Undo"/> has an earlier state of a layout file.
        /// </summary>
        public static bool CanUndo => UndoStack.Count > 0;

        /// <summary>
        /// Puts the layout file changed last back as it was before that change, and places everything again. The last 50
        /// changes are kept in <c>map_editor.undo</c>, so they survive a restart.
        /// </summary>
        public static bool Undo()
        {
            if (UndoStack.Count == 0)
            {
                return false;
            }

            string?[] step = UndoStack[^1];
            UndoStack.RemoveAt(UndoStack.Count - 1);
            WriteUndo();
            string file = Path.Combine(GetFolderPath(), step[0]!);
            try
            {
                if (step[1] == null)
                {
                    File.Delete(file);
                }
                else
                {
                    File.WriteAllText(file, step[1]);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[MapEditor] Could not undo the last change of " + file + ": " + e.Message);
                return false;
            }

            Reload();
            return true;
        }

        /// <summary>
        /// Saves a changed area to its file and makes its navmesh again.
        /// </summary>
        public static void UpdateArea(MapAreaDefinition area)
        {
            int index = AreaEntries.FindIndex(entry => entry.Area == area);
            if (index < 0)
            {
                return;
            }

            Save(AreaEntries[index].File);
            MapAreas.Set(AreaEntries.Select(entry => entry.Area));
        }

        /// <summary>
        /// Adds a new walkable area to <c>map_editor.json</c>. The id is filled in.
        /// </summary>
        public static MapAreaDefinition AddArea(MapAreaDefinition area)
        {
            int number = 1;
            while (AreaEntries.Any(entry => entry.Area.Id == "Area_" + number))
            {
                number++;
            }

            area.Id = "Area_" + number;
            AreaEntries.Add((area, EditorFile));
            Save(EditorFile);
            MapAreas.Set(AreaEntries.Select(entry => entry.Area));
            return area;
        }

        /// <summary>
        /// Removes an area from its file.
        /// </summary>
        public static void RemoveArea(string id)
        {
            int index = AreaEntries.FindIndex(entry => entry.Area.Id == id);
            if (index < 0)
            {
                return;
            }

            string file = AreaEntries[index].File;
            AreaEntries.RemoveAt(index);
            Save(file);
            MapAreas.Set(AreaEntries.Select(entry => entry.Area));
        }

        /// <summary>
        /// Whether the object was placed by a layout, not authored by the game.
        /// </summary>
        public static bool IsOwn(Transform target) => _root != null && target.IsChildOf(_root.transform);

        /// <summary>
        /// Places a new addressable prop and saves it into <c>map_editor.json</c>. An empty <paramref name="prefabGuid"/> places a blocker.
        /// </summary>
        public static MapObjectDefinition Add(string prefabGuid, Vector2 position)
        {
            return Add(new MapObjectDefinition { Prefab = prefabGuid, X = position.x, Y = position.y });
        }

        /// <summary>
        /// Places a new prop of any source and saves it into <c>map_editor.json</c>. The id is filled in from the source's name.
        /// </summary>
        public static MapObjectDefinition Add(MapObjectDefinition definition)
        {
            string baseId = definition.IsBlocker ? BlockerId
                : definition.Dynamic.Length > 0 ? definition.Dynamic
                : definition.Path.Length > 0 ? Path.GetFileName(definition.Path).Split('[')[0]
                : MapPrefabCatalog.PathOf(definition.Prefab) is { } path ? MapPrefabCatalog.NameOf(path) : definition.Prefab;
            int number = 1;
            while (ObjectsById.ContainsKey(baseId + "_" + number) || ConsumedIds.Contains(baseId + "_" + number))
            {
                number++;
            }

            definition.Id = baseId + "_" + number;
            if (definition.Scene.Length > 0 || definition.Dynamic.Length > 0)
            {
                definition.Seed ??= Guid.NewGuid().ToString("N");
            }

            ObjectsById[definition.Id] = definition;
            FileById[definition.Id] = EditorFile;
            Save(EditorFile);
            Spawn(definition);
            return definition;
        }

        /// <summary>
        /// Saves a changed prop and moves its live object and its navmesh cut to match.
        /// </summary>
        public static void Update(MapObjectDefinition definition)
        {
            if (TryGetInstance(definition.Id, out GameObject? instance))
            {
                ApplyTransform(instance, definition);
                Cut(definition.Id, instance);
            }

            if (FileById.TryGetValue(definition.Id, out string? file))
            {
                Save(file);
            }
        }

        /// <summary>
        /// Removes a prop from the world and from its file.
        /// </summary>
        public static void Remove(string id)
        {
            Release(id);
            ObjectsById.Remove(id);
            if (ConsumedIds.Remove(id))
            {
                WriteConsumed();
            }

            if (FileById.Remove(id, out string? file))
            {
                Save(file);
            }
        }

        /// <summary>
        /// Hides a vanilla object now and whenever its scene loads, and saves that into <c>map_editor.json</c>.
        /// </summary>
        public static MapRemovalDefinition Hide(GameObject target)
        {
            MapRemovalDefinition removal = new() { Scene = target.scene.name, Path = PathOf(target.transform) };
            Removals.Add((removal, EditorFile));
            HideObject(removal, target);
            Save(EditorFile);
            return removal;
        }

        /// <summary>
        /// Shows a hidden vanilla object again and drops it from its file.
        /// </summary>
        public static void Unhide(MapRemovalDefinition removal)
        {
            int index = Removals.FindIndex(entry => entry.Removal == removal);
            if (index < 0)
            {
                return;
            }

            string file = Removals[index].File;
            Removals.RemoveAt(index);
            if (HiddenByKey.Remove(KeyOf(removal), out (GameObject Target, OW_ActivatedObject.EActivation Activation) hidden))
            {
                Show(hidden.Target, hidden.Activation);
            }

            Save(file);
        }

        /// <summary>
        /// Re-reads every layout file, places the props again and re-applies the removals to the loaded scenes.
        /// </summary>
        public static void Reload()
        {
            foreach (string id in InstancesById.Keys.Concat(HandlesById.Keys).ToList())
            {
                Release(id);
            }

            foreach ((GameObject target, OW_ActivatedObject.EActivation activation) in HiddenByKey.Values)
            {
                Show(target, activation);
            }

            HiddenByKey.Clear();
            foreach (AsyncOperationHandle<GameObject> load in StampPrefabs.Values)
            {
                if (load != null && load.IsValid())
                {
                    Addressables.Release(load);
                }
            }

            StampPrefabs.Clear();
            ObjectsById.Clear();
            FileById.Clear();
            FailedIds.Clear();
            Removals.Clear();
            AreaEntries.Clear();
            string folder = GetFolderPath();
            Directory.CreateDirectory(folder);
            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    MapLayoutFile? layout = JsonSerializer.Deserialize<MapLayoutFile>(File.ReadAllText(file), JsonOptions);
                    foreach (MapObjectDefinition definition in layout?.Objects ?? [])
                    {
                        if (string.IsNullOrWhiteSpace(definition.Id) || !ObjectsById.TryAdd(definition.Id, definition))
                        {
                            MelonLogger.Warning($"[MapEditor] {Path.GetFileName(file)}: skipping a prop without id or with a duplicate id '{definition.Id}'.");
                            continue;
                        }

                        FileById[definition.Id] = file;
                    }

                    foreach (MapRemovalDefinition removal in layout?.Removed ?? [])
                    {
                        Removals.Add((removal, file));
                    }

                    foreach (MapAreaDefinition area in layout?.Areas ?? [])
                    {
                        AreaEntries.Add((area, file));
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[MapEditor] Could not read " + file + ": " + e.Message);
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                ApplyRemovals(SceneManager.GetSceneAt(i));
            }

            MapAreas.Set(AreaEntries.Select(entry => entry.Area));

            _nextCheck = 0f;
        }

        /// <summary>
        /// A world loads: props go under a root in the actor scene, which unloads with the world.
        /// </summary>
        internal static void BeginWorld(Scene scene)
        {
            _root = new GameObject("Modding_API_Map");
            SceneManager.MoveGameObjectToScene(_root, scene);
            ConsumedIds.Clear();
            string consumed = string.Empty;
            if (Savegame.Current != null && Savegame.Current.Data.GetString(ConsumedSaveKey, ref consumed))
            {
                ConsumedIds.UnionWith(consumed.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            }

            Reload();
        }

        /// <summary>
        /// Finishes loading props and, every half second, places the ones near the player and releases far ones.
        /// Called from the API melon's update.
        /// </summary>
        internal static void Tick()
        {
            if (_root == null)
            {
                return;
            }

            MapSceneTemplates.Tick();
            for (int i = Abandoned.Count - 1; i >= 0; i--)
            {
                if (Abandoned[i].IsDone)
                {
                    ReleaseHandle(Abandoned[i]);
                    Abandoned.RemoveAt(i);
                }
            }

            for (int i = LoadingIds.Count - 1; i >= 0; i--)
            {
                string id = LoadingIds[i];
                if (!HandlesById.TryGetValue(id, out AsyncOperationHandle<GameObject> load))
                {
                    LoadingIds.RemoveAt(i);
                    continue;
                }

                if (!load.IsDone)
                {
                    continue;
                }

                LoadingIds.RemoveAt(i);
                GameObject? instance = load.Status == AsyncOperationStatus.Succeeded ? load.Result : null;
                if (instance == null || !ObjectsById.TryGetValue(id, out MapObjectDefinition? definition))
                {
                    HandlesById.Remove(id);
                    ReleaseHandle(load);
                    if (FailedIds.Add(id))
                    {
                        MelonLogger.Warning($"[MapEditor] '{id}': its prefab did not load.");
                    }

                    continue;
                }

                instance.name = "Map_" + definition.Id;
                ApplyTransform(instance, definition);
                InstancesById[id] = instance;
                Cut(id, instance);
            }

            if (Time.unscaledTime < _nextCheck || !PlayerAccess.TryGetPlayer(out Actor? player))
            {
                return;
            }

            _nextCheck = Time.unscaledTime + CheckSeconds;
            MapAreas.Tick();
            MapAreas.UpdateWeather(player.GetFeetPosition());
            foreach (KeyValuePair<string, GameObject> placed in InstancesById)
            {
                if (placed.Value == null)
                {
                    DestroyedIds.Add(placed.Key);
                }
            }

            foreach (string id in DestroyedIds)
            {
                Consume(id);
            }

            DestroyedIds.Clear();

            CollectOrigins(player);
            foreach (MapObjectDefinition definition in ObjectsById.Values)
            {
                float distance = NearestOrigin(new Vector2(definition.X, definition.Y));
                bool present = InstancesById.ContainsKey(definition.Id) || HandlesById.ContainsKey(definition.Id);
                if (distance < SpawnRange && !present)
                {
                    Spawn(definition);
                }
                else if (distance > ReleaseRange && present)
                {
                    Release(definition.Id);
                }
            }
        }

        /// <summary>
        /// Every reference point of the open world root, not only the local player, since co-op gives each teammate one.
        /// </summary>
        private static void CollectOrigins(Actor player)
        {
            Origins.Clear();
            Origins.Add(player.GetFeetPosition());
            OW_Root? root = OW_Root.CurrentRoot;
            if (root == null || root._referencePoints == null)
            {
                return;
            }

            for (int i = 0; i < root._referencePoints.Count; i++)
            {
                OW_ReferencePoint? point = root._referencePoints[i]?.TryCast<OW_ReferencePoint>();
                if (point != null && point.isActiveAndEnabled)
                {
                    Origins.Add(point.transform.position);
                }
            }
        }

        private static float NearestOrigin(Vector2 position)
        {
            float nearest = float.MaxValue;
            foreach (Vector2 origin in Origins)
            {
                nearest = Mathf.Min(nearest, Vector2.Distance(origin, position));
            }

            return nearest;
        }

        private static void Spawn(MapObjectDefinition definition)
        {
            if (_root == null || FailedIds.Contains(definition.Id) || ConsumedIds.Contains(definition.Id) || HandlesById.ContainsKey(definition.Id) || InstancesById.ContainsKey(definition.Id))
            {
                return;
            }

            if (definition.Stamp)
            {
                SpawnStamp(definition, _root.transform);
                return;
            }

            if (definition.Scene.Length > 0 || definition.Dynamic.Length > 0)
            {
                GameObject? source = null;
                if (definition.Dynamic.Length > 0)
                {
                    source = GameDatabase.Instance.DynamicObjects.TryGetPrefab(definition.Dynamic, out SaveRoot_Dynamic prefab) && prefab != null ? prefab.gameObject : null;
                }
                else if (!MapSceneTemplates.TryGet(definition.Scene, definition.Path, out source))
                {
                    return;
                }

                if (source == null)
                {
                    FailedIds.Add(definition.Id);
                    MelonLogger.Warning($"[MapEditor] '{definition.Id}': its source object was not found.");
                    return;
                }

                GameObject copy = SpawnCopy(definition, source, _root.transform);
                InstancesById[definition.Id] = copy;
                if (copy.GetComponentInChildren<Interact_Bhvr_LootAll>(true) != null)
                {
                    PickupIds.Add(definition.Id);
                }

                if (definition.Scene.Length > 0)
                {
                    MapSceneTemplates.Use(definition.Scene);
                    SourceSceneById[definition.Id] = definition.Scene;
                }

                Cut(definition.Id, copy);
                return;
            }

            if (definition.IsBlocker)
            {
                GameObject blocker = CreateBlocker(definition, _root.transform);
                InstancesById[definition.Id] = blocker;
                Cut(definition.Id, blocker);
                return;
            }

            try
            {
                // A reference per load: one shared AssetReference fails on its second load.
                AssetReferenceGameObject reference = new(definition.Prefab);
                HandlesById[definition.Id] = reference.InstantiateAsync(new Vector3(definition.X, definition.Y, 0f), Quaternion.identity, _root.transform);
                LoadingIds.Add(definition.Id);
            }
            catch (Exception e)
            {
                FailedIds.Add(definition.Id);
                MelonLogger.Warning($"[MapEditor] '{definition.Id}': could not load prefab '{definition.Prefab}': {e.Message}");
            }
        }

        /// <summary>
        /// Copies a chunk prop or dynamic object under the inactive holder, where it can take its own guids before it wakes up.
        /// </summary>
        private static GameObject SpawnCopy(MapObjectDefinition definition, GameObject source, Transform parent)
        {
            GameObject copy = Object.Instantiate(source, MapSceneTemplates.Holder);
            copy.name = "Map_" + definition.Id;
            int index = 0;
            foreach (GuidComponent guidComponent in copy.GetComponentsInChildren<GuidComponent>(true))
            {
                SetGuid(guidComponent, LazyActorCreator.StableGuid(GuidScope, (definition.Seed ?? definition.Id) + "/" + index++));
            }

            // A dynamic save object without a guid would be saved into the game's respawn list and come back twice.
            SaveRoot_Dynamic? dynamicRoot = copy.GetComponent<SaveRoot_Dynamic>();
            if (dynamicRoot != null && dynamicRoot._guidComponent == null)
            {
                GuidComponent added = copy.AddComponent<GuidComponent>();
                SetGuid(added, LazyActorCreator.StableGuid(GuidScope, definition.Seed ?? definition.Id));
                dynamicRoot._guidComponent = added;
            }

            if (definition.Loot != null)
            {
                SetLoot(copy, definition);
            }

            if (!definition.Crime)
            {
                DisableCrimes(copy);
            }

            OW_Teleporter? gate = definition.TargetX.HasValue ? copy.GetComponentInChildren<OW_Teleporter>(true) : null;
            if (gate != null)
            {
                gate._targetPos = new Vector2(definition.TargetX!.Value, definition.TargetY ?? 0f);
            }

            Depot_Wrapper? depot = definition.Locked.HasValue ? copy.GetComponentInChildren<Depot_Wrapper>(true) : null;
            if (depot != null && definition.Locked == true)
            {
                depot.Lock();
            }
            else if (depot != null)
            {
                depot.Unlock();
            }

            ApplyTransform(copy, definition);
            copy.SetActive(true);
            copy.transform.SetParent(parent, true);
            return copy;
        }

        /// <summary>
        /// The loot preset fills the inventory on wake-up, and a saved inventory replaces that on load.
        /// </summary>
        private static void SetLoot(GameObject copy, MapObjectDefinition definition)
        {
            Inventory_LootBhvr? loot = copy.GetComponentInChildren<Inventory_LootBhvr>(true);
            if (loot == null)
            {
                MelonLogger.Warning($"[MapEditor] '{definition.Id}' has Loot but is not a container.");
                return;
            }

            loot.LootBhvr._generatedLoot.Clear();
            loot.LootBhvr._fixLoot.Clear();
            foreach (MapLootDefinition stack in definition.Loot!)
            {
                Item? item = ProviderAccess.ItemDatabase?.GetItemByReadableId(stack.Item);
                if (item == null)
                {
                    MelonLogger.Warning($"[MapEditor] '{definition.Id}': unknown item '{stack.Item}'.");
                    continue;
                }

                loot.LootBhvr._fixLoot.Add(new ItemStack(item, Math.Max(1, stack.Amount)));
            }
        }

        /// <summary>
        /// A placed object the game destroyed itself (a sprung trap, a taken pickup) stays gone in this save.
        /// It is written into the save data at once, so autosaves carry it too.
        /// </summary>
        private static void Consume(string id)
        {
            InstancesById.Remove(id);
            StopUsingSource(id);
            if (HandlesById.Remove(id, out AsyncOperationHandle<GameObject> handle))
            {
                ReleaseHandle(handle);
            }

            if (CutById.Remove(id, out Bounds cut))
            {
                UpdateNavmesh(cut);
            }

            ConsumedIds.Add(id);
            WriteConsumed();
            bool pickup = PickupIds.Remove(id);
            try
            {
                OnConsumed?.Invoke(id, pickup);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[MapEditor] An OnConsumed handler failed: " + e);
            }
        }

        /// <summary>
        /// Removes a placed object as consumed in this save, as though the game had destroyed it here. For an object consumed on
        /// another machine sharing the world. Does not raise <see cref="OnConsumed"/>.
        /// </summary>
        public static void MarkConsumed(string id)
        {
            if (!ObjectsById.ContainsKey(id) || !ConsumedIds.Add(id))
            {
                return;
            }

            Release(id);
            WriteConsumed();
        }

        private static void WriteConsumed()
        {
            Savegame.Current?.Data.SetString(ConsumedSaveKey, string.Join("\n", ConsumedIds));
        }

        private static void DisableCrimes(GameObject copy)
        {
            foreach (Interact_Bhvr_Crime crime in copy.GetComponentsInChildren<Interact_Bhvr_Crime>(true))
            {
                DisableCrime(crime._crime);
            }

            foreach (Interact_Condition_Locked locked in copy.GetComponentsInChildren<Interact_Condition_Locked>(true))
            {
                DisableCrime(locked._crime);
            }

            foreach (Interact_Bhvr_CrimeOnBlockingInteraction blocking in copy.GetComponentsInChildren<Interact_Bhvr_CrimeOnBlockingInteraction>(true))
            {
                DisableCrime(blocking._crimeModule);
            }

            foreach (HitReceiveBhvr_Crime hit in copy.GetComponentsInChildren<HitReceiveBhvr_Crime>(true))
            {
                DisableCrime(hit._crime);
            }
        }

        private static void DisableCrime(CrimeModule? crime)
        {
            if (crime != null)
            {
                crime.SetIsCrime(false);
                crime._crimeDisabled = true;
            }
        }

        private static void SetGuid(GuidComponent guidComponent, string guid)
        {
            guidComponent._guid = new Il2CppGuid(guid);
            guidComponent._guidString = guid;
        }

        /// <summary>
        /// A stamp's source is the prefab asset itself, loaded once and kept, or a chunk template or dynamic prefab.
        /// While the source still loads nothing is placed, and the next tick tries again.
        /// </summary>
        private static void SpawnStamp(MapObjectDefinition definition, Transform parent)
        {
            GameObject? source = null;
            if (definition.Prefab.Length > 0)
            {
                if (!StampPrefabs.TryGetValue(definition.Prefab, out AsyncOperationHandle<GameObject>? load) || load == null)
                {
                    load = Addressables.LoadAssetAsync<GameObject>(definition.Prefab);
                    StampPrefabs[definition.Prefab] = load;
                }

                if (!load.IsDone)
                {
                    return;
                }

                source = load.Status == AsyncOperationStatus.Succeeded ? load.Result : null;
            }
            else if (definition.Dynamic.Length > 0)
            {
                source = GameDatabase.Instance.DynamicObjects.TryGetPrefab(definition.Dynamic, out SaveRoot_Dynamic prefab) && prefab != null ? prefab.gameObject : null;
            }
            else if (definition.Scene.Length > 0 && !MapSceneTemplates.TryGet(definition.Scene, definition.Path, out source))
            {
                return;
            }

            if (source == null)
            {
                FailedIds.Add(definition.Id);
                MelonLogger.Warning($"[MapEditor] '{definition.Id}': its stamp source was not found.");
                return;
            }

            GameObject stamp = MapStamps.Create(definition, source, parent);
            ApplyTransform(stamp, definition);
            InstancesById[definition.Id] = stamp;
            MapStamps.AddTerrain(definition, stamp);
            if (definition.Scene.Length > 0)
            {
                MapSceneTemplates.Use(definition.Scene);
                SourceSceneById[definition.Id] = definition.Scene;
            }
        }

        private static GameObject CreateBlocker(MapObjectDefinition definition, Transform parent)
        {
            GameObject blocker = new("Map_" + definition.Id);
            blocker.transform.SetParent(parent, false);
            blocker.layer = LayerMask.NameToLayer(BlockerLayer);
            blocker.AddComponent<BoxCollider2D>();
            ApplyTransform(blocker, definition);
            return blocker;
        }

        /// <summary>
        /// Rebuilds the navmesh where the object's solid colliders were and are now, as Drova's <c>ActorStaticObstacle</c> does.
        /// </summary>
        private static void Cut(string id, GameObject instance)
        {
            bool had = CutById.Remove(id, out Bounds before);
            if (TryGetColliderBounds(instance, out Bounds now))
            {
                CutById[id] = now;
                if (had)
                {
                    now.Encapsulate(before);
                }

                UpdateNavmesh(now);
            }
            else if (had)
            {
                UpdateNavmesh(before);
            }
        }

        /// <summary>
        /// The handle, not the instance, is released: a world unload destroys the instance but keeps the handle's load.
        /// Instances are never switched off first: saveables record every switch, and the next copy would load it.
        /// </summary>
        private static void Release(string id)
        {
            InstancesById.Remove(id, out GameObject? instance);
            StopUsingSource(id);
            PickupIds.Remove(id);
            MapStamps.RemoveTerrain(id);
            if (instance != null)
            {
                foreach (Collider2D collider in instance.GetComponentsInChildren<Collider2D>())
                {
                    collider.enabled = false;
                }
            }

            if (CutById.Remove(id, out Bounds cut))
            {
                UpdateNavmesh(cut);
            }

            if (HandlesById.Remove(id, out AsyncOperationHandle<GameObject> handle))
            {
                // A load still running cannot be cancelled; its instance is released once it finishes.
                if (handle.IsDone)
                {
                    ReleaseHandle(handle);
                }
                else
                {
                    Abandoned.Add(handle);
                }
            }
            else if (instance != null)
            {
                // Saveables go first: they record the object as switched off when it is destroyed around them.
                foreach (ASaveableMonoBehaviourBase saveable in instance.GetComponentsInChildren<ASaveableMonoBehaviourBase>(true))
                {
                    Object.DestroyImmediate(saveable);
                }

                // At once, so its guids are free for a copy placed under the same id in this frame.
                Object.DestroyImmediate(instance);
            }
        }

        private static void StopUsingSource(string id)
        {
            if (SourceSceneById.Remove(id, out string? scene))
            {
                MapSceneTemplates.Unuse(scene);
            }
        }

        private static void ReleaseHandle(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
        }

        private static void ApplyTransform(GameObject instance, MapObjectDefinition definition)
        {
            instance.transform.position = new Vector3(definition.X, definition.Y, 0f);
            float scale = definition.Scale <= 0f ? 1f : definition.Scale;
            instance.transform.localScale = new Vector3(definition.FlipX ? -scale : scale, scale, 1f);
            instance.transform.rotation = Quaternion.Euler(0f, 0f, definition.Rotation);
            BoxCollider2D? box = definition.IsBlocker ? instance.GetComponent<BoxCollider2D>() : null;
            if (box != null)
            {
                box.size = new Vector2(definition.Width, definition.Height);
            }
        }

        private static void ApplyRemovals(Scene scene)
        {
            foreach ((MapRemovalDefinition removal, string _) in Removals)
            {
                if (!removal.Scene.Equals(scene.name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GameObject? target = Find(scene, removal.Path);
                if (target == null)
                {
                    MelonLogger.Warning($"[MapEditor] '{removal.Path}' is not in {removal.Scene}.");
                    continue;
                }

                HideObject(removal, target);
            }
        }

        /// <summary>
        /// Chunk objects load inactive and <c>OW_ObjectActivator</c> turns them on over the next frames, so they opt out of it.
        /// </summary>
        private static void HideObject(MapRemovalDefinition removal, GameObject target)
        {
            OW_ActivatedObject? activated = target.GetComponent<OW_ActivatedObject>();
            if (!HiddenByKey.TryGetValue(KeyOf(removal), out (GameObject Target, OW_ActivatedObject.EActivation Activation) known) || known.Target != target)
            {
                HiddenByKey[KeyOf(removal)] = (target, activated != null ? activated.Activation : OW_ActivatedObject.EActivation.None);
            }

            if (activated != null)
            {
                activated.SetActivation(OW_ActivatedObject.EActivation.None);
            }

            if (!target.activeSelf)
            {
                return;
            }

            bool solid = TryGetColliderBounds(target, out Bounds bounds);
            target.SetActive(false);
            if (solid)
            {
                UpdateNavmesh(bounds);
            }
        }

        private static void Show(GameObject target, OW_ActivatedObject.EActivation activation)
        {
            if (target == null)
            {
                return;
            }

            OW_ActivatedObject? activated = target.GetComponent<OW_ActivatedObject>();
            if (activated != null)
            {
                activated.SetActivation(activation);
            }

            target.SetActive(true);
            if (TryGetColliderBounds(target, out Bounds bounds))
            {
                UpdateNavmesh(bounds);
            }
        }

        private static void UpdateNavmesh(Bounds bounds)
        {
            if (AstarPath.active != null)
            {
                bounds.Expand(NavmeshMargin);
                AstarPath.active.UpdateGraphs(bounds);
            }
        }

        private static bool TryGetColliderBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (Collider2D collider in target.GetComponentsInChildren<Collider2D>())
            {
                if (!collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (any)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    any = true;
                }
            }

            return any;
        }

        /// <summary>
        /// Walks a path written by <see cref="PathOf"/>.
        /// </summary>
        internal static GameObject? Find(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            (string rootName, int rootIndex) = ParseSegment(segments[0]);
            List<Transform> roots = [];
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    roots.Add(root.transform);
                }
            }

            // Root ranks are unreliable in catalogs from exported scenes, so every same-named root is tried.
            if (rootIndex < roots.Count)
            {
                Transform ranked = roots[rootIndex];
                roots.RemoveAt(rootIndex);
                roots.Insert(0, ranked);
            }

            foreach (Transform root in roots)
            {
                Transform? found = FindBelow(root, segments);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform? FindBelow(Transform root, string[] segments)
        {
            Transform current = root;
            for (int s = 1; s < segments.Length; s++)
            {
                (string name, int index) = ParseSegment(segments[s]);
                Transform? next = null;
                int seen = 0;
                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (child.name == name && seen++ == index)
                    {
                        next = child;
                        break;
                    }
                }

                if (next == null)
                {
                    return null;
                }

                current = next;
            }

            return current;
        }

        /// <summary>
        /// Names from the scene root, joined by <c>/</c>. A name shared with earlier siblings gets its rank, as <c>Name[2]</c>.
        /// </summary>
        private static string PathOf(Transform target)
        {
            List<string> segments = [];
            for (Transform? current = target; current != null; current = current.parent)
            {
                int index = 0;
                Transform? parent = current.parent;
                if (parent != null)
                {
                    for (int i = 0; i < current.GetSiblingIndex(); i++)
                    {
                        if (parent.GetChild(i).name == current.name)
                        {
                            index++;
                        }
                    }
                }
                else
                {
                    foreach (GameObject root in current.gameObject.scene.GetRootGameObjects())
                    {
                        if (root.transform == current)
                        {
                            break;
                        }

                        if (root.name == current.name)
                        {
                            index++;
                        }
                    }
                }

                segments.Insert(0, index == 0 && !current.name.EndsWith(']') ? current.name : $"{current.name}[{index}]");
            }

            return string.Join("/", segments);
        }

        private static (string Name, int Index) ParseSegment(string segment)
        {
            int open = segment.LastIndexOf('[');
            return open > 0 && segment.EndsWith(']') && int.TryParse(segment[(open + 1)..^1], out int index)
                ? (segment[..open], index)
                : (segment, 0);
        }

        private static string KeyOf(MapRemovalDefinition removal) => removal.Scene + "|" + removal.Path;

        private static List<string?[]> UndoStack => _undoStack ??= ReadUndo();

        private static string UndoFile => Path.Combine(GetFolderPath(), UndoFileName);

        private static List<string?[]> ReadUndo()
        {
            try
            {
                return File.Exists(UndoFile) ? JsonSerializer.Deserialize<List<string?[]>>(File.ReadAllText(UndoFile)) ?? [] : [];
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[MapEditor] Could not read " + UndoFile + ", undo starts empty: " + e.Message);
                return [];
            }
        }

        private static void WriteUndo()
        {
            try
            {
                File.WriteAllText(UndoFile, JsonSerializer.Serialize(UndoStack));
            }
            catch (Exception e)
            {
                MelonLogger.Error("[MapEditor] Could not save " + UndoFile + ": " + e.Message);
            }
        }

        private static void Save(string file)
        {
            try
            {
                MapLayoutFile layout = new()
                {
                    Objects = ObjectsById.Values.Where(definition => FileById.TryGetValue(definition.Id, out string? owner) && owner == file).ToList(),
                    Removed = Removals.Where(entry => entry.File == file).Select(entry => entry.Removal).ToList(),
                    Areas = AreaEntries.Where(entry => entry.File == file).Select(entry => entry.Area).ToList()
                };
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                string json = JsonSerializer.Serialize(layout, JsonOptions);
                string? previous = File.Exists(file) ? File.ReadAllText(file) : null;
                if (previous != json)
                {
                    UndoStack.Add([Path.GetFileName(file), previous]);
                    if (UndoStack.Count > MaxUndoSteps)
                    {
                        UndoStack.RemoveAt(0);
                    }

                    WriteUndo();
                }

                File.WriteAllText(file, json);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[MapEditor] Could not save " + file + ": " + e.Message);
            }
        }
    }
}
