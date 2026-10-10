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
        // What each spawned actor was made from, by pointer. A spawned instance does not remember its prefab.
        private static readonly Dictionary<IntPtr, string> SpawnedFrom = new();

        // The same, for handles rather than bodies.
        private static readonly Dictionary<IntPtr, string> LazySpawnedFrom = new();

        // Every creature identity this process invented. See IsRuntimeCreated for why a caller needs it.
        private static readonly HashSet<string> MintedGuids = new(StringComparer.Ordinal);

        /// <summary>
        /// Raised on the main thread for every actor spawned through this facade, with the asset it was made from,
        /// so a co-op mod can build the same creature on another machine.
        /// </summary>
        public static event Action<Actor, string>? OnActorSpawned;

        /// <summary>
        /// Raised on the main thread for every lazy handle created through this API, with the actor asset behind it.
        /// The body streams in and out with the player's distance, so watch <c>LazyActor.Actor</c> for it.
        /// The asset names the base prefab only, not equipment, cosmetics, talents or a name applied on top.
        /// </summary>
        public static event Action<LazyActor, string>? OnLazyActorSpawned;

        /// <summary>
        /// Whether this creature's identity was invented in this process rather than read out of a scene.
        /// Runtime handles sit in the same guid table as scene-authored ones, so looking it up cannot tell them apart.
        /// </summary>
        /// <param name="guid">The creature's guid.</param>
        /// <returns>True when nothing outside this process can be expected to know what this guid means.</returns>
        public static bool IsRuntimeCreated(string? guid)
        {
            return !string.IsNullOrEmpty(guid) && MintedGuids.Contains(guid);
        }

        /// <summary>
        /// What a lazy handle's creature is built from.
        /// </summary>
        /// <param name="lazyActor">The handle.</param>
        /// <param name="assetGuid">The addressable actor prefab behind it.</param>
        /// <returns>False for a handle this API did not create, including every one the game authored into
        /// a scene, which carries its own identity instead.</returns>
        public static bool TryGetLazySpawnAsset(LazyActor? lazyActor, [NotNullWhen(true)] out string? assetGuid)
        {
            assetGuid = string.Empty;

            if (lazyActor == null) return false;

            return LazySpawnedFrom.TryGetValue(lazyActor.Pointer, out assetGuid);
        }

        /// <summary>
        /// Records a lazy handle this API created and announces it. Called from
        /// <see cref="Systems.Spawning.LazyActorCreator"/>, and a mod that builds handles itself may call
        /// it too.
        /// </summary>
        /// <param name="lazyActor">The handle.</param>
        /// <param name="assetGuid">The addressable actor prefab behind it.</param>
        public static void RecordLazySpawn(LazyActor? lazyActor, string assetGuid)
        {
            if (lazyActor == null) return;

            string guid = lazyActor._guidstring ?? string.Empty;

            // Recorded even without an asset id. The identity is the part a shared world has to know about,
            // and it is minted whether or not the caller told us what the creature is made from.
            if (!string.IsNullOrEmpty(guid))
            {
                MintedGuids.Add(guid);
            }

            if (string.IsNullOrEmpty(assetGuid)) return;

            LazySpawnedFrom[lazyActor.Pointer] = assetGuid;

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
        /// What a spawned actor was made from.
        /// </summary>
        /// <param name="actor">The spawned actor.</param>
        /// <param name="assetGuid">The addressable asset it came from.</param>
        /// <returns>False for an actor this facade did not spawn, including everything the game itself
        /// placed in a scene, which carries its own identity instead.</returns>
        public static bool TryGetSpawnAsset(Actor? actor, [NotNullWhen(true)] out string? assetGuid)
        {
            assetGuid = string.Empty;

            if (actor == null) return false;

            return SpawnedFrom.TryGetValue(actor.Pointer, out assetGuid);
        }

        /// <summary>
        /// Records what an actor was spawned from, for callers that do their own instantiation.
        /// </summary>
        /// <param name="actor">The spawned actor.</param>
        /// <param name="assetGuid">The addressable asset it came from.</param>
        public static void RecordSpawn(Actor? actor, string assetGuid)
        {
            if (actor == null || string.IsNullOrEmpty(assetGuid)) return;

            SpawnedFrom[actor.Pointer] = assetGuid;

            Raise(actor, assetGuid);
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
        /// Removes a lazily-spawned creature and the body it created, as though it had never arrived.
        /// Nothing dies: no experience, no loot, no death events. Right for a timed event, wrong for a fight.
        /// </summary>
        /// <param name="lazyActor">What to remove. Null and already-destroyed are both accepted.</param>
        /// <returns>False when there was nothing to remove.</returns>
        public static bool TryDespawn(LazyActor? lazyActor)
        {
            if (lazyActor == null || lazyActor.IsDestroyed) return false;

            try
            {
                // The game's own path, and it is asynchronous: it waits out any load already in flight
                // before releasing the asset and destroying the body. That is exactly why the handle
                // cannot be destroyed in the same breath. The continuation reads the handle's own
                // transform, so tearing it down here would fault inside the game's code a moment later.
                lazyActor.Unload();

                // Deferred to the end of the frame by Unity, which is late enough for the unload to have
                // taken its reference to the transform.
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
        /// Kills a spawned creature through the game's own death path: loot drops, experience is awarded and death
        /// events fire. Prefer it whenever a player might have seen the creature.
        /// </summary>
        /// <param name="actor">Who to kill. Null and already-dead are both accepted.</param>
        /// <returns>False when there was nothing to kill.</returns>
        public static bool TryKill(Actor? actor)
        {
            if (actor == null) return false;

            try
            {
                Health health = actor.GetHealth();
                if (health == null || health.IsDead) return false;

                // Straight to zero rather than through a damage calculation: there is no attacker, and a
                // calculated blow would be answered by armor, blockers and the revive guard, all correct
                // for a fight and all wrong for a cleanup that has already been decided.
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
        /// <param name="lazyActor">What to clear up.</param>
        /// <returns>False when there was nothing to do.</returns>
        public static bool TryKillOrDespawn(LazyActor? lazyActor)
        {
            if (lazyActor == null || lazyActor.IsDestroyed) return false;

            Actor body = lazyActor.Actor;

            if (body != null && TryKill(body)) return true;

            return TryDespawn(lazyActor);
        }

        /// <summary>
        /// Spawns a creature named only by its addressable id, e.g. one received from another machine.
        /// See <see cref="TrySpawn"/>. Synchronous, so it stalls the frame if the asset is not resident yet.
        /// </summary>
        /// <param name="assetGuid">The addressable id.</param>
        /// <param name="position">Where to put it.</param>
        /// <param name="actor">The spawned actor, when it worked.</param>
        /// <returns>False when this installation does not have that asset, or it carries no actor.</returns>
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
        /// Puts a creature into the world from a prefab already in hand, the way the game's own spawner does:
        /// parented to the spawn root, cognition toggled during setup, and marked as just damaged to wake its AI.
        /// Not saved: the API's spawn root is excluded from the savegame.
        /// </summary>
        /// <param name="prefab">What to spawn.</param>
        /// <param name="position">Where to put it.</param>
        /// <param name="actor">The spawned actor, when it worked.</param>
        /// <param name="assetGuid">The addressable this prefab came from, when the caller knows it.
        /// Recorded so the creature can be named to other machines. See
        /// <see cref="OnActorSpawned"/>.</param>
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
                    // Not an actor is not a partial success. Leaving the object behind would be a leak
                    // the caller has no handle on, since it is being told this failed.
                    Object.Destroy(spawned);
                    return false;
                }

                // Under the API's spawn root, which is what keeps it out of the save and takes it away
                // with the scene.
                NpcCreator.TrackSpawnedObject(spawned.transform);

                // The nudge. Without it the creature stands still until something wanders into the
                // perception it has not been told to use yet.
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
