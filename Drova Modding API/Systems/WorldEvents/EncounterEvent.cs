using Drova_Modding_API.Access;
using Drova_Modding_API.Extensions;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// An event that spawns encounters.
    /// </summary>
    /// <param name="encountersToSpawn"><see cref="AddressableAccess"/>For possible AssetReferenceGameObjects and the int is for the amount to spawn</param>
    /// <param name="selfEndInSecond">A safety mechanism to end the event itself after a certain amount of time</param>
    public class EncounterEvent(Dictionary<AssetReferenceGameObject, int> encountersToSpawn, int selfEndInSecond = 180) : IWorldEvent
    {
        /// <summary>
        /// The locator to get a random position to spawn
        /// </summary>
        protected readonly ActorWorldLocator WorldLocator = new();
        private readonly List<GameObject> _spawnedEncounters = [];
        /// <summary>
        /// For possible AssetReferenceGameObjects and the int is for the amount to spawn
        /// </summary>
        protected readonly Dictionary<AssetReferenceGameObject, int> EncountersToSpawn = encountersToSpawn;
        /// <summary>
        /// A safety mechanism to end the event itself after a certain amount of time
        /// </summary>
        protected readonly int SelfEndInSecond = selfEndInSecond;
        private object? _melonCoroutineToken;
        private bool _isRunning = false;

        /// <summary>
        /// Called when the event starts. Checks if the player is alive and not teleporting.
        /// </summary>
        /// <returns></returns>
        public bool CanStartEvent()
        {
            if (PlayerAccess.TryGetPlayer(out Actor player))
            {
                return player.IsAlive() && !player.IsPlayerTeleporting();
            }
            return false;
        }

        /// <summary>
        /// Starts the event and spawns the encounters. Also sets a random min and max range for the encounters to spawn.
        /// </summary>
        public virtual void StartEvent()
        {
            _isRunning = true;
            if (!PlayerAccess.TryGetPlayer(out Actor player))
            {
                WorldEventSystemManager.Instance.EndEvent();
            }
            foreach (KeyValuePair<AssetReferenceGameObject, int> encounter in EncountersToSpawn)
            {
                for (int i = 0; i < encounter.Value; i++)
                {
                    Vector3 position = player.transform.position;

                    Vector2? randomPosition = WorldLocator.GetRandomFreePosition(new Vector2(position.x, position.y));
                    if (randomPosition.HasValue)
                    {
                        _spawnedEncounters.Add(encounter.Key.InstantiateAsync(randomPosition.Value, Quaternion.identity).WaitForCompletion());
                    }
                    else
                    {
                        MelonLogger.Warning("Couldn't find a free position to spawn the encounter.");
                        WorldEventSystemManager.Instance.EndEvent();
                        return;
                    }
                }
            }
            _melonCoroutineToken = MelonCoroutines.Start(SelfEnd());
        }

        /**
         * Coroutine to end the event after a certain amount of time as safety.
         */
        public virtual IEnumerator SelfEnd()
        {
            yield return new WaitForSeconds(SelfEndInSecond);
            if (_isRunning)
            {
                WorldEventSystemManager.Instance.EndEvent();
            }
        }

        /**
         * Called when the event ends and removes all the spawned encounters.
         */
        public virtual void EndEvent()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            if (_melonCoroutineToken != null)
            {
                MelonCoroutines.Stop(_melonCoroutineToken);
            }
            foreach (GameObject encounter in _spawnedEncounters)
            {
                if (encounter != null)
                {
                    UnityEngine.Object.Destroy(encounter);
                }
            }
            _spawnedEncounters.Clear();
            WorldEventSystemManager.Instance.EndEvent();

        }
    }
}
