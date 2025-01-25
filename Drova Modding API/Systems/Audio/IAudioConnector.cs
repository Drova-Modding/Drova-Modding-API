using Il2CppNodeCanvas.DialogueTrees;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Interface for audio connectors
    /// </summary>
    public interface IAudioConnector
    {
        /// <summary>
        /// Called when a dialogue tree is loaded, to connect the audio system to the dialogue system
        /// </summary>
        /// <param name="dialogueTree"></param>
        void OnDialogueTreeLoaded(DialogueTree dialogueTree);
    }
}
