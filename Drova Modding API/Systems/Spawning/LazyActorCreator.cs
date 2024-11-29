using Drova_Modding_API.Access;
using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections;
using MelonLoader;

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
        public const string LAZY_ACTOR_NAME = "Modding_API_Lazy_Actor";

        internal static void RestoreLazyActor(List<LazyActorSaveData> lazyActorSaveDatas)
        {
            for (int i = 0; i < lazyActorSaveDatas.Count; i++)
            {
                LazyActorSaveData lazyActorSaveData = lazyActorSaveDatas[i];
                GameObject gameObject = new(lazyActorSaveData.ActorName);
                gameObject.SetActive(false);
                var lazyActor = gameObject.AddComponent<LazyActor>();
                var guidComponent = gameObject.AddComponent<GuidComponent>();
                guidComponent._guid = new Il2CppSystem.Guid(lazyActorSaveData.ActorGuid);
                guidComponent._guidString = lazyActorSaveData.ActorGuid;
                lazyActor._guidComponent = guidComponent;
                lazyActor._guid = guidComponent._guid;
                lazyActor._guidstring = guidComponent._guidString;
                lazyActor._actorReference = new AssetReferenceGameObject(lazyActorSaveData.ActorReferenceString);
                MelonCoroutines.Start(LoadEntityInfo(lazyActor, new AssetReference(lazyActorSaveData.ActorEnitityInfoReferenceString)));
            }
        }

        /// <summary>
        /// Creates a lazy actor for a creature
        /// </summary>
        /// <param name="actorParams">The params for how to create a lazy actor</param>
        /// <returns>The created lazy actor, it is not fully initalized on the return, because we need to load things in the background</returns>
        public static LazyActor CreateLazyActorCreature(LazyActorParams actorParams)
        {
            GameObject gameObject = new(LAZY_ACTOR_NAME);
            gameObject.SetActive(false);
            var lazyActor = gameObject.AddComponent<LazyActor>();
            lazyActor._actorReference = actorParams.AssetReference;
            lazyActor._health = new CustomActorHealth();
            lazyActor.transform.position = actorParams.Position;
            lazyActor._spawnPos = actorParams.Position;
            var guidComponent = gameObject.AddComponent<GuidComponent>();
            guidComponent.CreateGuid();
            lazyActor._guidComponent = guidComponent;
            lazyActor._guid = guidComponent._guid;
            lazyActor._guidstring = guidComponent._guidString;

            if (actorParams.HasCustomHealth)
            {
                lazyActor._health._maxHealth.IsActive = true;
                lazyActor._health._maxHealth.Value = actorParams.MaxHealth ?? 0;
                lazyActor._health._currentHealth.Value = actorParams.CurrentHealth ?? 0;
                lazyActor._health._currentHealth.IsActive = true;
            }
            MelonCoroutines.Start(LoadEntityInfo(lazyActor, actorParams.EntityInfo));
            return lazyActor;
        }

        /**
         * Load the entity info and activate the LazyActor
         */
        public static IEnumerator LoadEntityInfo(LazyActor lazyActor, AssetReference assetReference)
        {
            var handle = Addressables.LoadAssetAsync<EntityInfo>(assetReference);
            while (!handle.IsDone)
                yield return null;
            lazyActor._entityInfo = handle.Result;
            lazyActor.gameObject.SetActive(true);
            lazyActor._playerActor = PlayerAccess.GetPlayer();
            lazyActor.enabled = true;
            lazyActor._guidComponent.enabled = true;
            var toDestroy = lazyActor.GetComponents<GuidComponent>();
            for (int i = 0; i < toDestroy.Count; i++)
            {
                GuidComponent component = toDestroy[i];
                if (component._guidString != lazyActor._guidstring)
                    UnityEngine.Object.Destroy(component);
            }

            ProviderAccess.GetEntityGameHandler().RegisterLazyActor(lazyActor);
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
             * Entity Info <see cref="AddressableAccess.EntityInfos"/>"/>
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
        }
    }
}
