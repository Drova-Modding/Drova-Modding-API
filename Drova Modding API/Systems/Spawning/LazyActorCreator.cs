using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.SaveGame;
using Il2CppDrova;
using Il2CppDrova.AI.Modules;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.MapDatabases;
using Il2CppDrova.Routine;
using Il2CppDrova.Utilities.LazyLoading;
using Il2CppNodeCanvas.BehaviourTrees;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Creates a lazy actor
    /// </summary>
    public static class LazyActorCreator
    {
        /**
         * The name of the lazy actor game object
         */
        public const string LazyActorName = "Modding_API_Lazy_Actor";

        private const string RoutineTreeName = "AI_Routine_General";

        private static readonly Dictionary<string, EntityInfo> EntityInfosByGuid = new(StringComparer.OrdinalIgnoreCase);
        private static BehaviourTree? _routineTree;

        /// <summary>
        /// A guid derived from a scope and an id, the same on every load and every machine.
        /// The game keys an object's save state by its guid, so a stable guid keeps that state.
        /// </summary>
        public static string StableGuid(string scope, string id)
            => new Guid(MD5.HashData(Encoding.UTF8.GetBytes(scope + "/" + id))).ToString();

        internal static void RestoreLazyActor(List<LazyActorSaveData> lazyActorSaveDatas)
        {
            for (int i = 0; i < lazyActorSaveDatas.Count; i++)
            {
                LazyActorSaveData lazyActorSaveData = lazyActorSaveDatas[i];
                bool minted = string.IsNullOrEmpty(lazyActorSaveData.ExternalDefinitionId);
                GameObject gameObject = new(lazyActorSaveData.ActorName);
                gameObject.SetActive(false);
                NpcCreator.TrackSpawnedObject(gameObject.transform);
                LazyActor lazyActor = gameObject.AddComponent<LazyActor>();
                GuidComponent guidComponent = gameObject.AddComponent<GuidComponent>();
                guidComponent._guid = new Il2CppSystem.Guid(lazyActorSaveData.ActorGuid);
                guidComponent._guidString = lazyActorSaveData.ActorGuid;
                lazyActor._guidComponent = guidComponent;
                lazyActor._guid = guidComponent._guid;
                lazyActor._guidstring = guidComponent._guidString;
                lazyActor._actorReference = new AssetReferenceGameObject(lazyActorSaveData.ActorReferenceString);

                if (lazyActorSaveData.HasCustomEntityInfo)
                {
                    EntityInfo customEntityInfo = EntityInfo.CreateUndefined();
                    if (!string.IsNullOrEmpty(lazyActorSaveData.CustomEntityInfoGuid))
                    {
                        customEntityInfo._guid = lazyActorSaveData.CustomEntityInfoGuid;
                    }
                    ActivateLazyActor(lazyActor, customEntityInfo, lazyActorSaveData.IsNpc, minted);
                }
                else
                {
                    MelonCoroutines.Start(LoadEntityInfo(lazyActor, new AssetReference(lazyActorSaveData.ActorEnitityInfoReferenceString), lazyActorSaveData.IsNpc, minted));
                }
            }
        }

        /// <summary>
        /// Creates a generic lazy actor and supports either an entity-info reference or a preloaded entity-info instance.
        /// </summary>
        /// <param name="actorName">Name for the spawned lazy actor game object.</param>
        /// <param name="actorReference">Addressable actor prefab reference. Only its GUID is used, each actor gets its own copy
        /// because a shared reference fails to load a second time.</param>
        /// <param name="position">World position to spawn at.</param>
        /// <param name="entityInfoReference">Optional entity-info addressable reference.</param>
        /// <param name="customEntityInfo">Optional preloaded entity info. Takes precedence over <paramref name="entityInfoReference"/>.</param>
        /// <param name="noSave">Whether the lazy actor is left out of the save.</param>
        /// <param name="isNpc">Whether the actor is an NPC.</param>
        /// <param name="guid">Optional fixed guid. The game keys the actor's save state by it, so a fixed guid keeps an actor's death across loads.</param>
        public static LazyActor CreateLazyActor(
            string actorName,
            AssetReferenceGameObject actorReference,
            Vector2 position,
            AssetReference? entityInfoReference = null,
            EntityInfo? customEntityInfo = null,
            bool noSave = false,
            bool isNpc = true,
            string? guid = null)
        {
            GameObject gameObject = new(actorName);
            gameObject.SetActive(false);

            // Under the actor scene's spawn root, so the actor goes with its world. The active scene outlives a save load.
            NpcCreator.TrackSpawnedObject(gameObject.transform);

            LazyActor lazyActor = gameObject.AddComponent<LazyActor>();
            lazyActor._actorReference = new AssetReferenceGameObject(actorReference.AssetGUID);
            lazyActor.transform.position = position;
            lazyActor._spawnPos = position;

            var routineHandler = gameObject.AddComponent<RoutineHandler>();
            var aiLazyFactory = gameObject.AddComponent<AI_LazyActorFactory>();
            aiLazyFactory._routineHandler = routineHandler;
            aiLazyFactory._actor = lazyActor;
            aiLazyFactory.enabled = true;
            lazyActor._actorFactory = aiLazyFactory;

            Blackboard blackboard = gameObject.AddComponent<Blackboard>();
            blackboard._identifier = actorName;

            BehaviourTreeOwner behaviourTreeOwner = gameObject.AddComponent<BehaviourTreeOwner>();
            behaviourTreeOwner._blackboard = blackboard;
            behaviourTreeOwner._firstActivation = GraphOwner.FirstActivation.Lazy;
            behaviourTreeOwner._boundGraphObjectReferences = new Il2CppSystem.Collections.Generic.List<Object>();
            behaviourTreeOwner._boundGraphSerialization = "";
            behaviourTreeOwner._graph = GetRoutineTree();

            LazyGraphUpdater lazyGraphUpdater = gameObject.AddComponent<LazyGraphUpdater>();
            lazyGraphUpdater._actor = lazyActor;
            lazyGraphUpdater._graph = behaviourTreeOwner;
            lazyGraphUpdater._actorFactory = aiLazyFactory;
            lazyGraphUpdater._entityHandler = ProviderAccess.GetEntityGameHandler();

            GuidComponent guidComponent = gameObject.AddComponent<GuidComponent>();
            if (guid != null)
            {
                guidComponent._guid = new Il2CppSystem.Guid(guid);
                guidComponent._guidString = guid;
            }
            else
            {
                guidComponent.CreateGuid();
            }
            lazyActor._guidComponent = guidComponent;
            lazyActor._guid = guidComponent._guid;
            lazyActor._guidstring = guidComponent._guidString;
            
            ActorGuidComponent actorGuidComponent = gameObject.AddComponent<ActorGuidComponent>();
            actorGuidComponent._guidComponent = guidComponent;

            DS_LazyDialogueActor dialogueActor = gameObject.AddComponent<DS_LazyDialogueActor>();
            dialogueActor._lazyActor = lazyActor;
            lazyActor._lazyDialogueActor = dialogueActor;

            if (noSave)
            {
                string noSaveGuid = guidComponent._guidString;
                NoSaveGuidRegistry.Register(noSaveGuid);
                lazyActor.LazyActorDestroyedEvent.AddEventListener(new Action<LazyActor>(_ => NoSaveGuidRegistry.Unregister(noSaveGuid)));
            }

            if (customEntityInfo != null)
            {
                ActivateLazyActor(lazyActor, customEntityInfo, isNpc, guid == null);
            }
            else
            {
                AssetReference referenceToLoad = entityInfoReference ?? AddressableAccess.EntityInfos.EntityInfo_Default;
                MelonCoroutines.Start(LoadEntityInfo(lazyActor, referenceToLoad, isNpc, guid == null));
            }

            return lazyActor;
        }

        /// <summary>
        /// Creates a lazy actor for a creature
        /// </summary>
        /// <returns>The created lazy actor, not fully initialized yet, the entity info still loads in the background</returns>
        public static LazyActor CreateLazyActorCreature(LazyActorParams actorParams)
        {
            LazyActor lazyActor = CreateLazyActor(LazyActorName, actorParams.AssetReference, actorParams.Position, actorParams.EntityInfo, guid: actorParams.Guid);
            lazyActor._health = new CustomActorHealth();

            if (actorParams.HasCustomHealth)
            {
                lazyActor._health._maxHealth.IsActive = true;
                lazyActor._health._maxHealth.Value = actorParams.MaxHealth ?? 0;
                lazyActor._health._currentHealth.Value = actorParams.CurrentHealth ?? 0;
                lazyActor._health._currentHealth.IsActive = true;
            }

            return lazyActor;
        }

        /**
         * Load the entity info and activate the LazyActor
         */
        public static IEnumerator LoadEntityInfo(LazyActor lazyActor, AssetReference assetReference, bool isNpc, bool minted = true)
        {
            // One load per entity info, kept: a load per actor would add a reference that is never released.
            string key = assetReference.AssetGUID;
            if (!EntityInfosByGuid.TryGetValue(key, out EntityInfo? entityInfo) || entityInfo == null)
            {
                UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<EntityInfo> handle = Addressables.LoadAssetAsync<EntityInfo>(key);
                while (!handle.IsDone)
                    yield return null;

                entityInfo = handle.Result;
                if (EntityInfosByGuid.TryGetValue(key, out EntityInfo? loaded) && loaded != null)
                {
                    Addressables.Release(handle);
                    entityInfo = loaded;
                }
                else
                {
                    EntityInfosByGuid[key] = entityInfo;
                }
            }

            ActivateLazyActor(lazyActor, entityInfo, isNpc, minted);
        }

        /// <summary>
        /// The vanilla routine tree every lazy actor starts with, found once instead of per actor.
        /// </summary>
        private static BehaviourTree? GetRoutineTree()
        {
            if (_routineTree != null)
            {
                return _routineTree;
            }

            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<BehaviourTree> trees = Resources.FindObjectsOfTypeAll<BehaviourTree>();
            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i].name == RoutineTreeName)
                {
                    _routineTree = trees[i];
                    break;
                }
            }

            return _routineTree;
        }

        private static void ActivateLazyActor(LazyActor lazyActor, EntityInfo entityInfo, bool isNpc, bool minted)
        {
            lazyActor._entityInfo = entityInfo;
            lazyActor._playerActor = PlayerAccess.GetPlayer();
            lazyActor._isNpc = isNpc;
            lazyActor.gameObject.SetActive(true);
            lazyActor.enabled = true;
            lazyActor._guidComponent.enabled = true;


            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<GuidComponent> toDestroy = lazyActor.GetComponents<GuidComponent>();
            for (int i = 0; i < toDestroy.Count; i++)
            {
                GuidComponent component = toDestroy[i];
                if (component._guidString != lazyActor._guidstring)
                    Object.Destroy(component);
            }

            EntityGameHandler? entities = ProviderAccess.GetEntityGameHandler();
            if (entities == null) return;

            entities.RegisterLazyActor(lazyActor);

            // Here because both the create and the restore-from-save paths pass through. See ActorSpawnAccess.IsRuntimeCreated.
            ActorSpawnAccess.RecordLazySpawn(lazyActor, lazyActor._actorReference?.AssetGUID ?? string.Empty, minted);
        }

        /**
          * Params for a Lazy Actor to create, currently only supports creatures
          */
        public struct LazyActorParams
        {
            /**
             * What you want to spawn <see cref="AddressableAccess.Creatures"/> or <see cref="AddressableAccess.Bandits"/>
             */
            public AssetReferenceGameObject AssetReference;
            /**
             * Entity Info <see cref="AddressableAccess.EntityInfos"/>
             */
            public AssetReference EntityInfo;
            /**
             * Position of the actor
             */
            public Vector2 Position;
            /**
             * If you want to have custom health
             */
            public bool HasCustomHealth;
            /**
             * Optional MaxHealth. Must be set when HasCustomHealth is true
             */
            public int? MaxHealth;
            /**
             * Optional CurrentHealth. Must be set when HasCustomHealth is true
             */
            public int? CurrentHealth;

            /**
             * Whether this actor uses custom EntityInfo that should be saved
             */
            public bool HasCustomEntityInfoParams;
            /**
             * Custom EntityInfo GUID
             */
            public string CustomEntityInfoGuid;

            /**
             * Optional fixed actor guid, see <see cref="CreateLazyActor"/>
             */
            public string? Guid;
        }
    }
}