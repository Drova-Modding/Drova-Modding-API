using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Represents a module that can be applied to an NPC during creation.
    /// </summary>
    public interface INpcModule
    {
        /// <summary>
        /// Applies the module's logic to the given NPC GameObject.
        /// </summary>
        /// <param name="npc">The instantiated NPC GameObject</param>
        void Apply(GameObject npc);
    }
}

