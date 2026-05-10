using Drova_Modding_API.Access;
using System.Collections;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Class that helps creating NPCs
    /// </summary>
    public class NpcCreator
    {
        /// <summary>
        /// Create an NPC at the given position
        /// </summary>
        /// <param name="name">The name of the Actor</param>
        /// <param name="spawnPosition">Where to spawn it</param>
        /// <returns></returns>
        public static IEnumerator CreateNpc(string name, Vector2 spawnPosition)
        {
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> operation = AddressableAccess.NPCs.Human_Template.InstantiateAsync(spawnPosition, Quaternion.identity);
            while (!operation.IsDone)
            {
                yield return new WaitForSeconds(0.5f);
            }

            GameObject npc = operation.Result;
            npc.name = name;
        }
    }
}
