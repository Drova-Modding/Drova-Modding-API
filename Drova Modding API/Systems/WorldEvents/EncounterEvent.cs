using Drova_Modding_API.Access;
using Drova_Modding_API.Extensions;
using Il2CppDrova;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace Drova_Modding_API.Systems.WorldEvents
{
    /// <summary>
    /// An event that spawns encounters.
    /// </summary>
    /// <param name="encountersToSpawn"><see cref="AddressableAccess"/>For possible AssetReferenceGameObjects and the int is for the amount</param>
    public class EncounterEvent(Dictionary<AssetReferenceGameObject, int> encountersToSpawn) : IWorldEvent
    {
        /// <summary>
        /// Called when the event starts. Checks if the player is alive and not teleporting.
        /// </summary>
        /// <returns></returns>
        public bool CanStartEvent()
        {
            if(PlayerAccess.TryGetPlayer(out var player))
            {
                return player.IsAlive() && !player.IsTelporting();
            }
            return false;
        }

        public void StartEvent()
        {
            if(!PlayerAccess.TryGetPlayer(out var player))
            {
                WorldEventSystemManager.Instance.EndEvent();
            }
            foreach (var encounter in encountersToSpawn)
            {
                for (int i = 0; i < encounter.Value; i++)
                {
                    var position = player.GetPositionOf(Entity.EPosition.CurrentTransformPosition);
                    // add some randomization to the position
                    position += new Vector2(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5));
                    encounter.Key.InstantiateAsync(position, Quaternion.identity);
                }
            }
        }

        public void EndEvent()
        {
            throw new NotImplementedException();
        }

        
    }
}
