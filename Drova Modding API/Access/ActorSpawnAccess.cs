using System.Diagnostics.CodeAnalysis;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Putting a creature into the world and taking it out again.
    /// Destroying a <c>LazyActor</c> destroys only the component and leaves the creature it spawned standing.
    /// Use <see cref="TryDespawn"/> for "this was never really here", <see cref="TryKill"/> for "this died".
    /// </summary>
    public static class ActorSpawnAccess
    {
        // A spawned instance does not remember its prefab. The wrapper is kept so a destroyed entry can be pruned.
        private static readonly Dictionary<IntPtr, (Actor Actor, string Asset)> SpawnedFrom = new();

        private static readonly Dictionary<IntPtr, (LazyActor Handle, string Asset)> LazySpawnedFrom = new();

        private static readonly HashSet<string> MintedGuids = new(StringComparer.Ordinal);

        /// <summary>
        /// Raised on the main thread for every actor spawned through this facade, with the asset it was made from,
        /// so a co-op mod can build the same creature on another machine.
        /// </summary>
        public static event Action<Actor, string>? OnActorSpawned;

        /// <summary>
        /// Raised on the main thread for every lazy handle created through this API, with the base actor asset behind it.
        /// The body streams in and out with distance, so watch <c>LazyActor.Actor</c> for it.
        /// </summary>
        public static event Action<LazyActor, string>? OnLazyActorSpawned;

        /// <summary>
        /// Whether this creature's guid was minted in this process rather than authored in a scene.
        /// The game's guid table cannot tell them apart.
        /// </summary>
        public static bool IsRuntimeCreated(string? guid)
        {
            return !string.IsNullOrEmpty(guid) && MintedGuids.Contains(guid);
        }

        /// <summary>
        /// The addressable actor prefab behind a lazy handle. False for handles this API did not create.
        /// </summary>
        public static bool TryGetLazySpawnAsset(LazyActor? lazyActor, [NotNullWhen(true)] out string? assetGuid)
        {
            assetGuid = string.Empty;

            if (lazyActor == null) return false;

            if (!LazySpawnedFrom.TryGetValue(lazyActor.Pointer, out (LazyActor Handle, string Asset) spawned))
            {
                return false;
            }

            assetGuid = spawned.Asset;
            return true;
        }

        /// <summary>
        /// Records a lazy handle and raises <see cref="OnLazyActorSpawned"/>. Called by <see cref="Systems.Spawning.LazyActorCreator"/>.
        /// </summary>
        /// <param name="lazyActor">The handle.</param>
        /// <param name="assetGuid">The addressable actor prefab behind it.</param>
        /// <param name="minted">False when the guid is fixed (derived from a JSON id), so every machine knows it.</param>
        public static void RecordLazySpawn(LazyActor? lazyActor, string assetGuid, bool minted = true)
        {
            if (lazyActor == null) return;

            string guid = lazyActor._guidstring ?? string.Empty;

            if (minted && !string.IsNullOrEmpty(guid))
            {
                MintedGuids.Add(guid);
            }

            if (string.IsNullOrEmpty(assetGuid)) return;

            LazySpawnedFrom[lazyActor.Pointer] = (lazyActor, assetGuid);

            RaiseLazy(lazyActor, assetGuid);
        }

        private static void RaiseLazy(LazyActor lazyActor, string assetGuid)
        {
            if (OnLazyActorSpawned == null) return;

            foreach (Delegate subscriber in OnLazyActorSpawned.GetInvocationList())
            {
                try
                {
                    ((Action<LazyActor, string>)subscriber)(lazyActor, assetGuid);
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[Spawn] a subscriber to OnLazyActorSpawned failed: " + e);
                }
            }
        }

        /// <summary>
        /// The addressable asset a spawned actor came from. False for actors this facade did not spawn.
        /// </summary>
        public static bool TryGetSpawnAsset(Actor? actor, [NotNullWhen(true)] out string? assetGuid)
        {
            assetGuid = string.Empty;

            if (actor == null) return false;

            if (!SpawnedFrom.TryGetValue(actor.Pointer, out (Actor Actor, string Asset) spawned))
            {
                return false;
            }

            assetGuid = spawned.Asset;
            return true;
        }

        /// <summary>
        /// Records what an actor was spawned from, for callers that do their own instantiation.
        /// </summary>
        public static void RecordSpawn(Actor? actor, string assetGuid)
        {
            if (actor == null || string.IsNullOrEmpty(assetGuid)) return;

            SpawnedFrom[actor.Pointer] = (actor, assetGuid);

            Raise(actor, assetGuid);
        }

        /// <summary>
        /// Drops the records of actors and handles that were destroyed. Called when a world loads.
        /// </summary>
        internal static void PruneDestroyed()
        {
            foreach (IntPtr pointer in SpawnedFrom.Where(entry => entry.Value.Actor == null).Select(entry => entry.Key).ToList())
            {
                SpawnedFrom.Remove(pointer);
            }

            foreach (IntPtr pointer in LazySpawnedFrom.Where(entry => entry.Value.Handle == null).Select(entry => entry.Key).ToList())
            {
                LazySpawnedFrom.Remove(pointer);
            }
        }

        private static void Raise(Actor actor, string assetGuid)
        {
            if (OnActorSpawned == null) return;

            foreach (Delegate subscriber in OnActorSpawned.GetInvocationList())
            {
                try
                {
                    ((Action<Actor, string>)subscriber)(actor, assetGuid);
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[Spawn] a subscriber to OnActorSpawned failed: " + e);
                }
            }
        }

        /// <summary>
        /// Removes a lazily-spawned creature and its body without a death: no experience, loot or death events.
        /// </summary>
        /// <returns>False when there was nothing to remove.</returns>
        public static bool TryDespawn(LazyActor? lazyActor)
        {
            if (lazyActor == null || lazyActor.IsDestroyed) return false;

            try
            {
                // Unload is async and reads the handle's transform, so the handle must not be destroyed immediately.
                lazyActor.Unload();

                Object.Destroy(lazyActor.gameObject);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Spawn] could not despawn a lazy actor: " + e);
                return false;
            }
        }

        /// <summary>
        /// Kills a creature through the game's death path, so loot, experience and death events happen.
        /// </summary>
        /// <returns>False when there was nothing to kill.</returns>
        public static bool TryKill(Actor? actor)
        {
            if (actor == null) return false;

            try
            {
                Health health = actor.GetHealth();
                if (health == null || health.IsDead) return false;

                // Not a damage calculation, which armor, blockers and the revive guard could absorb.
                health.SetHealth(0, null);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Spawn] could not kill a spawned actor: " + e);
                return false;
            }
        }

        /// <summary>
        /// Kills the body a lazy handle spawned, if it has one, and otherwise removes the handle.
        /// </summary>
        public static bool TryKillOrDespawn(LazyActor? lazyActor)
        {
            if (lazyActor == null || lazyActor.IsDestroyed) return false;

            Actor body = lazyActor.Actor;

            if (body != null && TryKill(body)) return true;

            return TryDespawn(lazyActor);
        }

        /// <summary>
        /// Spawns a creature from its addressable id via <see cref="TrySpawn"/>.
        /// Synchronous, so it stalls the frame if the asset is not loaded yet.
        /// </summary>
        public static bool TrySpawnFromAsset(string? assetGuid, Vector2 position, [NotNullWhen(true)] out Actor? actor)
        {
            actor = null;

            if (string.IsNullOrEmpty(assetGuid)) return false;

            try
            {
                AssetReferenceGameObject reference = new(assetGuid);

                GameObject prefab = reference.LoadAssetAsync<GameObject>().WaitForCompletion();
                if (prefab == null) return false;

                return TrySpawn(prefab, position, out actor, assetGuid);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[Spawn] could not spawn from asset '{assetGuid}': " + e);
                return false;
            }
        }

        /// <summary>
        /// Spawns a creature from a prefab under the API's spawn root, which is not saved, and wakes its AI.
        /// </summary>
        /// <param name="prefab">What to spawn.</param>
        /// <param name="position">Where to put it.</param>
        /// <param name="actor">The spawned actor, when it worked.</param>
        /// <param name="assetGuid">The addressable this prefab came from, if known. Passed to <see cref="OnActorSpawned"/>.</param>
        /// <returns>False when the prefab is missing or carries no actor.</returns>
        public static bool TrySpawn(GameObject? prefab, Vector2 position, [NotNullWhen(true)] out Actor? actor, string assetGuid = "")
        {
            actor = null;

            if (prefab == null) return false;

            try
            {
                GameObject spawned = Object.Instantiate(prefab, position, Quaternion.identity);
                if (spawned == null) return false;

                Actor spawnedActor = spawned.GetComponent<Actor>();
                if (spawnedActor == null)
                {
                    Object.Destroy(spawned);
                    return false;
                }

                NpcCreator.TrackSpawnedObject(spawned.transform);

                // Without it the creature stands idle until something enters its perception.
                Health health = spawnedActor.GetHealth();
                if (health != null)
                {
                    health.SetReceivedDamageLastFrame();
                }

                actor = spawnedActor;

                if (!string.IsNullOrEmpty(assetGuid))
                {
                    RecordSpawn(spawnedActor, assetGuid);
                }

                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Spawn] could not spawn an actor: " + e);
                return false;
            }
        }
    }
}
