using Il2CppNodeCanvas.DialogueTrees;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Manager for the audio system
    /// </summary>
    public static class AudioManager
    {
        /// <summary>
        /// The cached CAVE prefix for audios
        /// </summary>
        public static string DEFAULT_CAVE_AUDIO_PREFIX = "_CAVE";
        internal static IAudioConnector _dialogueAudioConnector = new DefaultDialogueAudioConnector(new FileAudioProvider());
        internal static IAudioHandler _audioHandler = new DefaultAudioHandler();

        /// <summary>
        /// Replace the current DialogueAudioConnector with a new one
        /// </summary>
        /// <param name="dialogueAudioConnector">new DialogueAudioConnector</param>
        public static void ReplaceDialogueAudioConnector(IAudioConnector dialogueAudioConnector)
        {
            _dialogueAudioConnector = dialogueAudioConnector;
        }

        /// <summary>
        /// Replace the current AudioHandler with a new one
        /// </summary>
        /// <param name="audioHandler">new AudioHandler</param>
        public static void ReplaceAudioHandler(IAudioHandler audioHandler)
        {
            _audioHandler = audioHandler;
        }

        /// <summary>
        /// Get a unique ID for a statement node
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetUniqueIDStatement(DialogueTree tree, DS_StatementNode node)
        {
            if (node.statement.useGlobalLoca)
            {
                return $"{tree.name}_{node.statement.locaKey}_{node.statement.GlobalLocaPath.Replace('/', '_')}";
            }
            else
            {
                return $"{tree.name}_{node.statement.locaKey}_{node.DLGTree.LocaPath.Replace('/', '_')}";
            }
        }

        /// <summary>
        /// Get a unique ID for a statement node
        /// </summary>
        /// <param name="treeName"></param>
        /// <param name="locaKey"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetUniqueIDStatement(string treeName, string locaKey, string path)
        {
            return $"{treeName}_{locaKey}_{path.Replace('/', '_')}";
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
            if (node.statement.useGlobalLoca)
            {
                return $"{tree.name}_{node.statement.locaKey}_{node.statement.GlobalLocaPath.Replace('/', '_')}_{actorName}";
            }
            else
            {
                return $"{tree.name}_{node.statement.locaKey}_{node.DLGTree.LocaPath.Replace('/', '_')}_{actorName}";
            }
        }

        /// <summary>
        /// Get a unique ID for a generic statement node
        /// </summary>
        /// <returns></returns>
        public static string GetUniqueIDStatementGeneric(string treeName, string locaKey, string globalLocaPath, string actorName, string locaPath, bool useGlobalLoca = false)
        {
            if (useGlobalLoca)
            {
                return $"{treeName}_{locaKey}_{globalLocaPath.Replace('/', '_')}_{actorName}";
            }
            else
            {
                return $"{treeName}_{locaKey}_{locaPath.Replace('/', '_')}_{actorName}";
            }
        }

        /// <summary>
        /// Get a unique ID for a choice node
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="choice"></param>
        /// <returns></returns>
        public static string GetUniqueIDChoice(DialogueTree tree, DS_MultipleChoiceNode.Choice choice)
        {
            if (choice.statement.useGlobalLoca)
            {
                return $"{tree.name}_{choice.statement.locaKey}_{choice.statement.GlobalLocaPath.Replace('/', '_')}";
            }
            else
            {
                return $"{tree.name}_{choice.statement.locaKey}_{tree.LocaPath.Replace('/', '_')}";
            }
        }
    }
}
