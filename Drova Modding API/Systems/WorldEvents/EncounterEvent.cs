using Drova_Modding_API.Access;
using Drova_Modding_API.Extensions;
using Il2CppDrova;
using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Collections;
using MelonLoader;
using Drova_Modding_API.Systems.Spawning;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// An event that spawns encounters.
    /// </summary>
    /// <param name="encountersToSpawn"><see cref="AddressableAccess"/>For possible AssetReferenceGameObjects and the int is for the amount to spawn</param>
    /// <param name="selfEndInSecond">A safety mechanism to end the event itself after a certain amount of time</param>
    public class EncounterEvent(Dictionary<AssetReferenceGameObject, int> encountersToSpawn, int selfEndInSecond = 180) : IWorldEvent
    {
        private readonly ActorWorldLocator _worldLocator = new();
        private readonly List<GameObject> _spawnedEncounters = [];
        private object _melonCouroutineToken;
        private bool _isRunning = false;


        /// <summary>
        /// Called when the event starts. Checks if the player is alive and not teleporting.
        /// </summary>
        /// <returns></returns>
        public bool CanStartEvent()
        {
            if (PlayerAccess.TryGetPlayer(out var player))
            {
                return player.IsAlive() && !player.IsTelporting();
            }
            return false;
        }

        /// <summary>
        /// Starts the event and spawns the encounters. Also sets a random min and max range for the encounters to spawn.
        /// </summary>
        public virtual void StartEvent()
        {
            _isRunning = true;
            if (!PlayerAccess.TryGetPlayer(out var player))
            {
                WorldEventSystemManager.Instance.EndEvent();
            }
            foreach (var encounter in encountersToSpawn)
            {
                for (int i = 0; i < encounter.Value; i++)
                {
                    var position = player.transform.position;

                    var randomPosition = _worldLocator.GetRandomFreePosition(new Vector2(position.x, position.y));
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
            _melonCouroutineToken = MelonCoroutines.Start(SelfEnd());
        }

        /**
         * Coroutine to end the event after a certain amount of time as safety.
         */
        public virtual IEnumerator SelfEnd()
        {
            yield return new WaitForSeconds(selfEndInSecond);
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
            _isRunning = false;
            if (_melonCouroutineToken != null)
            {
                MelonCoroutines.Stop(_melonCouroutineToken);
            }
            foreach (var encounter in _spawnedEncounters)
            {
                if (encounter != null)
                {
                    UnityEngine.Object.Destroy(encounter);
                }
            }
            _spawnedEncounters.Clear();

        }
    }
}
