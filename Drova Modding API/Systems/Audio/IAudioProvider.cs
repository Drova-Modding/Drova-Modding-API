using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Interface for providing audio clips for the dialogue system
    /// </summary>
    public interface IAudioProvider
    {
        /// <summary>
        /// Get the audio clip for the given dialoge node
        /// </summary>
        /// <param name="dialogeName">The name of the dialogueTree</param>
        /// <param name="filePath">filePath of the given node</param>
        /// <param name="locaKey">locaKey of the given node</param>
        /// <param name="actorName">actor name of the node</param>
        /// <param name="choiceId">if its multiple choice, the choice id</param>
        /// <returns>AudioClip to be used for this node</returns>
        public Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId);
    }
}
