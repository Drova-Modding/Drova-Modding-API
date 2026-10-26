using Il2CppDrova.InventorySystem;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// Copies of props baked into chunk scenes. A copy is taken from the loaded chunk, or from a short additive load of it.
    /// A copy references assets of its chunk's bundles (sprites, materials, interaction categories), so those bundles stay
    /// loaded while anything placed from that chunk is in use, and are released with the templates a while after the last use.
    /// </summary>
    internal static class MapSceneTemplates
    {
        private const float IdleSeconds = 30f;
        private const float IdleCheckSeconds = 1f;

        private static readonly Dictionary<string, GameObject?> TemplatesByKey = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> SceneLoads = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<string>> PathsByScene = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, AsyncOperationHandle> BundlesByScene = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<(string Name, AsyncOperationHandle<SceneInstance> Scene, AsyncOperationHandle Bundles)> PendingUnloads = [];
        private static readonly Dictionary<string, int> UsersByScene = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, float> IdleSinceByScene = new(StringComparer.OrdinalIgnoreCase);
        private static GameObject? _holder;
        private static float _nextIdleCheck;

        /// <summary>
        /// An inactive parent that outlives worlds. Objects copied under it stay asleep until they are moved out.
        /// </summary>
        internal static Transform Holder
        {
            get
            {
                if (_holder == null)
                {
                    _holder = new GameObject("Modding_API_MapTemplates");
                    _holder.SetActive(false);
                    Object.DontDestroyOnLoad(_holder);
                }

                return _holder.transform;
            }
        }

        /// <summary>
        /// Chunk scenes whose bundles are held now. For tests.
        /// </summary>
        internal static int HeldScenes => BundlesByScene.Count;

        /// <summary>
        /// Something placed from <paramref name="scene"/> is in the world, so its template and bundles must stay.
        /// </summary>
        internal static void Use(string scene)
        {
            UsersByScene[scene] = UsersByScene.GetValueOrDefault(scene) + 1;
            IdleSinceByScene.Remove(scene);
        }

        /// <summary>
        /// Ends a <see cref="Use"/>. The last one starts the countdown to releasing the scene's templates and bundles.
        /// </summary>
        internal static void Unuse(string scene)
        {
            int users = UsersByScene.GetValueOrDefault(scene) - 1;
            if (users > 0)
            {
                UsersByScene[scene] = users;
                return;
            }

            UsersByScene.Remove(scene);
            IdleSinceByScene[scene] = Time.unscaledTime;
        }

        /// <summary>
        /// True once the template is decided: <paramref name="template"/> is the copy, or null when the object was not found.
        /// False while it is still being fetched; a fetch is started on the first call.
        /// </summary>
        internal static bool TryGet(string scene, string path, out GameObject? template)
        {
            string key = scene + "|" + path;
            if (TemplatesByKey.TryGetValue(key, out template))
            {
                return true;
            }

            Request(scene, path);
            return TemplatesByKey.TryGetValue(key, out template);
        }

        /// <summary>
        /// Takes the templates of chunk scenes that finished loading, and unloads those scenes once their bundles are held.
        /// Called from the layout's tick.
        /// </summary>
        internal static void Tick()
        {
            if (SceneLoads.Count > 0)
            {
                FinishSceneLoads();
            }

            for (int i = PendingUnloads.Count - 1; i >= 0; i--)
            {
                (string _, AsyncOperationHandle<SceneInstance> scene, AsyncOperationHandle bundles) = PendingUnloads[i];
                if (!bundles.IsDone)
                {
                    continue;
                }

                PendingUnloads.RemoveAt(i);
                if (scene.IsValid())
                {
                    Addressables.UnloadSceneAsync(scene);
                }
            }

            if (IdleSinceByScene.Count == 0 || Time.unscaledTime < _nextIdleCheck)
            {
                return;
            }

            _nextIdleCheck = Time.unscaledTime + IdleCheckSeconds;
            foreach (KeyValuePair<string, float> idle in IdleSinceByScene.ToList())
            {
                if (Time.unscaledTime - idle.Value >= IdleSeconds && !SceneLoads.ContainsKey(idle.Key) && !IsUnloadPending(idle.Key))
                {
                    Evict(idle.Key);
                }
            }
        }

        private static void FinishSceneLoads()
        {
            foreach (KeyValuePair<string, AsyncOperationHandle<SceneInstance>> load in SceneLoads.ToList())
            {
                if (!load.Value.IsDone)
                {
                    continue;
                }

                SceneLoads.Remove(load.Key);
                PathsByScene.Remove(load.Key, out List<string>? paths);
                Scene? scene = load.Value.Status == AsyncOperationStatus.Succeeded ? load.Value.Result.Scene : null;
                foreach (string path in paths ?? [])
                {
                    GameObject? source = scene is { } loaded ? MapLayoutSystem.Find(loaded, path) : null;
                    if (source == null)
                    {
                        MelonLogger.Warning($"[MapEditor] '{path}' is not in {load.Key}.");
                        TemplatesByKey[load.Key + "|" + path] = null;
                        continue;
                    }

                    Harvest(source, load.Key, path);
                }

                // A scene that never loaded has nothing to unload: unloading a failed handle throws.
                if (scene == null)
                {
                    Addressables.Release(load.Value);
                    continue;
                }

                PendingUnloads.Add((load.Key, load.Value, HoldBundles(load.Key)));
            }
        }

        private static bool IsUnloadPending(string scene)
        {
            foreach ((string name, AsyncOperationHandle<SceneInstance> _, AsyncOperationHandle _) in PendingUnloads)
            {
                if (name == scene)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Evict(string scene)
        {
            IdleSinceByScene.Remove(scene);
            string prefix = scene + "|";
            foreach (string key in TemplatesByKey.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                if (TemplatesByKey.Remove(key, out GameObject? template) && template != null)
                {
                    Object.Destroy(template);
                }
            }

            if (BundlesByScene.Remove(scene, out AsyncOperationHandle bundles) && bundles.IsValid())
            {
                Addressables.Release(bundles);
            }
        }

        private static void Harvest(GameObject source, string scene, string path)
        {
            GameObject template = Object.Instantiate(source, Holder);
            template.name = source.name;

            // A live source has already filled its inventory from its loot preset, and each copy fills it again on wake-up.
            foreach (Inventory_LootBhvr loot in template.GetComponentsInChildren<Inventory_LootBhvr>(true))
            {
                loot._inventory.InventoryItems.Clear();
            }

            HoldBundles(scene);
            TemplatesByKey[scene + "|" + path] = template;
            if (!UsersByScene.ContainsKey(scene))
            {
                IdleSinceByScene[scene] = Time.unscaledTime;
            }
        }

        private static AsyncOperationHandle HoldBundles(string scene)
        {
            if (!BundlesByScene.TryGetValue(scene, out AsyncOperationHandle bundles))
            {
                bundles = Addressables.DownloadDependenciesAsync(scene, false);
                BundlesByScene[scene] = bundles;
            }

            return bundles;
        }

        private static void Request(string scene, string path)
        {
            Scene loaded = SceneManager.GetSceneByName(scene);
            if (loaded.IsValid() && loaded.isLoaded && !SceneLoads.ContainsKey(scene))
            {
                GameObject? source = MapLayoutSystem.Find(loaded, path);
                if (source != null)
                {
                    Harvest(source, scene, path);
                    return;
                }
            }

            if (!PathsByScene.TryGetValue(scene, out List<string>? paths))
            {
                paths = [];
                PathsByScene[scene] = paths;
            }

            if (!paths.Contains(path))
            {
                paths.Add(path);
            }

            if (SceneLoads.ContainsKey(scene))
            {
                return;
            }

            try
            {
                SceneLoads[scene] = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive);
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[MapEditor] Could not load {scene}: {e.Message}");
                PathsByScene.Remove(scene);
                foreach (string failed in paths)
                {
                    TemplatesByKey[scene + "|" + failed] = null;
                }
            }
        }
    }
}
