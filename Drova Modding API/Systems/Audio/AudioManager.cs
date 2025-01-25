using Il2CppNodeCanvas.DialogueTrees;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Manager for the audio system
    /// </summary>
    public static class AudioManager
    {
        internal static IAudioConnector _dialogueAudioConnector = new DefaultDialogueAudioConnector(new FileAudioProvider());

        /// <summary>
        /// Replace the current DialougeAudioConnector with a new one
        /// </summary>
        /// <param name="dialogueAudioConnector"></param>
        public static void SetDialogueAudioConnector(IAudioConnector dialogueAudioConnector)
        {
            _dialogueAudioConnector = dialogueAudioConnector;
        }

        /// <summary>
        /// Get a unique ID for a statement node
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetUniqueIDStatement(DialogueTree tree, DS_StatementNode node)
        {
            return $"{tree.name}_{node.statement.locaKey}_{node.statement.GlobalLocaPath.Replace('/', '_')}";
        }

        /// <summary>
        /// Get a unique ID for a generic statement node
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="node"></param>
        /// <param name="actorName"></param>
        /// <returns></returns>
        public static string GetUniqueIDStatementGeneric(DialogueTree tree, DS_StatementNode node, string actorName)
        {
            return $"{tree.name}_{node.statement.locaKey}_{node.statement.GlobalLocaPath.Replace('/', '_')}_{actorName}";
        }

        /// <summary>
        /// Get a unique ID for a choice node
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="choice"></param>
        /// <returns></returns>
        public static string GetUniqueIDChoice(DialogueTree tree, DS_MultipleChoiceNode.Choice choice)
        {
            return $"{tree.name}_{choice.statement.locaKey}_{choice.statement.GlobalLocaPath.Replace('/', '_')}";
        }
    }
}
