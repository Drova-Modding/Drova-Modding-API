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

        /// <summary>
        /// Called when a statement is executed in the world from a generic dialogue tree, the audio needs to be loaded instantly
        /// </summary>
        /// <param name="statementNode"></param>
        void OnWorldDialogueStatement(DS_StatementNode statementNode);
    }
}
