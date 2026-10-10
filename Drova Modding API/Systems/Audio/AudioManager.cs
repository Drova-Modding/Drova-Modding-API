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
        public static readonly string DEFAULT_CAVE_AUDIO_PREFIX = "_CAVE";
        internal static IAudioConnector _dialogueAudioConnector = new DefaultDialogueAudioConnector(new AssetBundleAudioProvider());
        internal static IAudioHandler _audioHandler = new DefaultAudioHandler();

        /// <summary>
        /// The clip of an answer the player can pick in a dialogue window, by the index the window settles it with: from the
        /// choice node the window stands on (inside a running sub-dialogue if there is one), loaded on demand and kept on the
        /// choice. The clips loaded with a tree asset never reach the copy of the tree a conversation runs on, so an answer
        /// is looked up when it is picked, as a spoken line is when it is said.
        /// </summary>
        /// <returns>The clip, or null when the answer has none, there is no choice node, or another connector is in place.</returns>
        public static UnityEngine.AudioClip? AnswerClip(Il2CppNodeCanvas.DialogueTrees.UI.DS_DialogueUGUI window, int index)
        {
            DS_MultipleChoiceNode? node = ChoiceNode(window);
            return node == null ? null : ChoiceClip(node, index);
        }

        /// <summary>
        /// The clip of a choice node's answer by its index in the node, loaded on demand and kept on the choice.
        /// </summary>
        /// <returns>The clip, or null when the answer has none, the index is out of range, or another connector is in place.</returns>
        public static UnityEngine.AudioClip? ChoiceClip(DS_MultipleChoiceNode node, int index)
        {
            if (index < 0 || index >= node.availableChoices.Count) return null;

            DS_Statement statement = node.availableChoices[index].statement;
            if (statement == null) return null;
            if (statement.audio != null) return statement.audio;
            if ((_dialogueAudioConnector as DefaultDialogueAudioConnector)?.AudioProvider is not IAudioProvider provider) return null;

            Task<UnityEngine.AudioClip> task = provider.GetAudioClip(node.DLGTree.name,
                statement.useGlobalLoca ? statement.GlobalLocaPath : node.DLGTree.LocaPath, statement.locaKey, node.actorName, index);
            MainThreadDispatcher.WaitForTasks(task);
            if (task.IsCompletedSuccessfully) statement.audio = task.Result;
            return statement.audio;
        }

        /// <summary>The choice node a dialogue window stands on, the way the window itself finds it.</summary>
        internal static DS_MultipleChoiceNode? ChoiceNode(Il2CppNodeCanvas.DialogueTrees.UI.DS_DialogueUGUI? window)
        {
            DialogueTree? tree = window == null ? null : window.DLGTree;
            while (tree != null && tree.currentNode?.TryCast<SubDialogueTree>() is SubDialogueTree sub) tree = sub.subGraph;
            return tree == null ? null : tree.currentNode?.TryCast<DS_MultipleChoiceNode>();
        }

        /// <summary>
        /// A dialogue clip by its name, from the default provider's bundles: see <see cref="AssetBundleAudioProvider.GetClipByName"/>.
        /// Null when another provider is in place, the clip is unknown, or dialogue audio is off.
        /// </summary>
        public static UnityEngine.AudioClip? FindDialogueClip(string actorName, string clipName) =>
            (_dialogueAudioConnector as DefaultDialogueAudioConnector)?.AudioProvider is AssetBundleAudioProvider provider
                ? provider.GetClipByName(actorName, clipName) : null;

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
        public static string GetUniqueIDStatement(string treeName, string locaKey, string path)
        {
            return $"{treeName}_{locaKey}_{path.Replace('/', '_')}";
        }

        /// <summary>
        /// Get a unique ID for a generic statement node
        /// </summary>
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
