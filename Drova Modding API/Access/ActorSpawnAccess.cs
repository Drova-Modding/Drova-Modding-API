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
    ///
    /// **Taking it out is the half every mod gets wrong**, and it is wrong in a way that does not look
    /// like a bug: destroying a <c>LazyActor</c> destroys the component and nothing else. The creature it
    /// spawned is a child transform registered separately as an entity, and the component's own
    /// <c>OnDestroy</c> unregisters the handle, nulls its reference to the body and destroys none of it.
    /// So a mod that cleans up after a timed event leaves live, hostile actors standing in the world for
    /// the rest of the session, and its own cleanup code reports success.
    ///
    /// The game never does this. <c>LazyActor.Unload</c> is the teardown, returning the addressable,
    /// calling <c>PreDestroy</c> on the body and destroying the body's own object. The game's bulk cleanup
    /// does not destroy at all, it sets health to zero and lets the death path release everything.
    ///
    /// Which of the two a mod wants is a real choice rather than a detail, so both are here:
    /// <see cref="TryDespawn"/> for "this was never really here", <see cref="TryKill"/> for "this died".
    /// </summary>
    public static class ActorSpawnAccess
    {
        // What each spawned actor was made from, by pointer. A spawned instance does not remember its own
        // prefab, since Unity does not record it and the game has no reason to, so the only place that
        // knowledge exists is the call that did the spawning.
        private static readonly Dictionary<IntPtr, string> _spawnedFrom = new();

        // The same, for handles rather than bodies.
        private static readonly Dictionary<IntPtr, string> _lazySpawnedFrom = new();

        // Every creature identity this process invented. See IsRuntimeCreated for why a caller needs it.
        private static readonly HashSet<string> _mintedGuids = new(StringComparer.Ordinal);

        /// <summary>
        /// Raised for every actor spawned through this facade, with the asset it was made from.
        ///
        /// **This exists so a shared world can exist.** A creature put into the world at runtime is
        /// invisible to everyone else: it carries no scene-authored identity, so nothing else can name it,
        /// and a co-op mod cannot replicate what it cannot name. Announcing the spawn with the asset behind
        /// it is what makes a second machine able to produce the same creature.
        ///
        /// On the main thread, and each subscriber is isolated.
        /// </summary>
        public static event Action<Actor, string>? OnActorSpawned;

        /// <summary>
        /// Raised for every lazy handle created through this API, with the actor asset behind it.
        ///
        /// **The handle, not the body, because the body may be hours away.** A lazy actor is a promise: the
        /// streaming system builds the creature when a player comes near and destroys it again when they
        /// leave, over and over. So there is no single spawn moment to announce, and a subscriber that wants
        /// the creature itself watches <c>LazyActor.Actor</c> rather than waiting for an event that will
        /// never come.
        ///
        /// The asset id is the same kind of id <see cref="OnActorSpawned"/> carries and is what a second
        /// machine can build from. It names the base prefab only. Anything a creator applied on top of it,
        /// such as equipment, cosmetics, talents or a name, is not in the asset and does not travel with
        /// it.
        ///
        /// On the main thread, and each subscriber is isolated.
        /// </summary>
        public static event Action<LazyActor, string>? OnLazyActorSpawned;

        /// <summary>
        /// Whether this creature's identity was invented in this process rather than read out of a scene.
        ///
        /// **The question every shared-world mod has to ask, and the one that cannot be answered by
        /// looking.** A creature authored into a scene carries a guid both machines read out of the same
        /// asset, so naming it to somebody else works. A creature this API created gets its guid from
        /// <c>Guid.NewGuid</c>, and the handle for it is registered in exactly the same table the game's own
        /// handles live in, so the obvious test, "is there a lazy actor for this guid", answers yes for
        /// both and a co-op mod ends up sending an identity nobody else can resolve. That failure is silent:
        /// the far side holds the binding forever, waiting for a body that will never exist.
        /// </summary>
        /// <param name="guid">The creature's guid.</param>
        /// <returns>True when nothing outside this process can be expected to know what this guid means.</returns>
        public static bool IsRuntimeCreated(string? guid)
        {
            return !string.IsNullOrEmpty(guid) && _mintedGuids.Contains(guid);
        }

        /// <summary>
        /// What a lazy handle's creature is built from.
        /// </summary>
        /// <param name="lazyActor">The handle.</param>
        /// <param name="assetGuid">The addressable actor prefab behind it.</param>
        /// <returns>False for a handle this API did not create, including every one the game authored into
        /// a scene, which carries its own identity instead.</returns>
        public static bool TryGetLazySpawnAsset(LazyActor? lazyActor, out string assetGuid)
        {
            assetGuid = string.Empty;

            if (lazyActor == null) return false;

            return _lazySpawnedFrom.TryGetValue(lazyActor.Pointer, out assetGuid!);
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
                _mintedGuids.Add(guid);
            }

            if (string.IsNullOrEmpty(assetGuid)) return;

            _lazySpawnedFrom[lazyActor.Pointer] = assetGuid;

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
        public static bool TryGetSpawnAsset(Actor? actor, out string assetGuid)
        {
            assetGuid = string.Empty;

            if (actor == null) return false;

            return _spawnedFrom.TryGetValue(actor.Pointer, out assetGuid!);
        }

        /// <summary>
        /// Records what an actor was spawned from, for callers that do their own instantiation.
        ///
        /// Offered because the two spawn paths in this API predate it and a mod may have its own. Without
        /// a way to register after the fact, those creatures stay unshareable.
        /// </summary>
        /// <param name="actor">The spawned actor.</param>
        /// <param name="assetGuid">The addressable asset it came from.</param>
        public static void RecordSpawn(Actor? actor, string assetGuid)
        {
            if (actor == null || string.IsNullOrEmpty(assetGuid)) return;

            _spawnedFrom[actor.Pointer] = assetGuid;

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
        ///
        /// **Silent.** Nothing dies, so no experience is awarded, no loot drops, no quest counter moves and
        /// nothing that was watching for a death hears one. That is what makes it right for cleaning up a
        /// timed event and wrong for anything a player was fighting.
        ///
        /// The body goes first, through the game's own unload, which also gives the addressable back. A
        /// mod that destroys the object instead keeps the bundle resident for the rest of the session.
        /// The handle follows, because a handle left behind is a creature the streaming system will bring
        /// back the next time the player walks past.
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
        /// Kills a spawned creature through the game's own death path.
        ///
        /// **This is what the game does to clear creatures it no longer wants.** It writes health to zero
        /// and lets everything downstream happen. Loot drops, experience is awarded, the corpse appears,
        /// and anything listening for a death hears one.
        ///
        /// Prefer it whenever a player might have seen the creature. A body that vanishes mid-fight reads
        /// as a bug, while a body that dies reads as the fight ending.
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
        ///
        /// The case a mod cleaning up a group actually has: some of what it spawned has materialized and
        /// some has not, and the two need opposite treatment. A creature the player has seen should die,
        /// while a handle that never loaded should simply go.
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
        /// Spawns a creature named only by its addressable id. See <see cref="TrySpawn"/> for the recipe
        /// it runs and why each step matters.
        ///
        /// For the caller that has an id rather than a prefab, which is what an id looks like when it has
        /// come from somewhere else, such as another machine in a shared session.
        ///
        /// **A fresh reference every time, never a cached one.** Loading the same <c>AssetReference</c>
        /// twice fails outright with "AssetReference that has already been loaded", so a shared instance
        /// works for the first creature and silently produces none of the rest.
        ///
        /// Synchronous, and it will stall the frame if the asset is not already resident.
        /// </summary>
        /// <param name="assetGuid">The addressable id.</param>
        /// <param name="position">Where to put it.</param>
        /// <param name="actor">The spawned actor, when it worked.</param>
        /// <returns>False when this installation does not have that asset, or it carries no actor.</returns>
        public static bool TrySpawnFromAsset(string? assetGuid, Vector2 position, out Actor actor)
        {
            actor = null!;

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
        /// Puts a creature into the world from a prefab already in hand, the way the game itself does.
        ///
        /// **The steps are not interchangeable and the order is not a preference.** The game's own spawner
        /// parents the new object to the scene's spawn root, switches cognition off while it is being set
        /// up, switches it back on, and then nudges the actor by marking it as having just been damaged,
        /// which is what actually wakes its AI. A mod that skips the parent leaves an object that a scene
        /// unload will not take with it, and one that skips the nudge gets a creature standing inert until
        /// something happens to walk into its perception.
        ///
        /// **It is also not saved, deliberately.** A spawned actor generates a fresh guid, which keys a new
        /// dynamic save object, so a mod that spawns without excluding it writes creatures into the
        /// player's save file that will be there forever. The spawn root this parents to is the API's own,
        /// which is already excluded.
        /// </summary>
        /// <param name="prefab">What to spawn.</param>
        /// <param name="position">Where to put it.</param>
        /// <param name="actor">The spawned actor, when it worked.</param>
        /// <param name="assetGuid">The addressable this prefab came from, when the caller knows it.
        /// Recorded so the creature can be named to other machines. See
        /// <see cref="OnActorSpawned"/>.</param>
        /// <returns>False when the prefab is missing or carries no actor.</returns>
        public static bool TrySpawn(GameObject? prefab, Vector2 position, out Actor actor, string assetGuid = "")
        {
            actor = null!;

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
