using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// A connector for the dialogue system to connect to the audio system
    /// </summary>
    /// <remarks>
    /// Creates a new instance of the <see cref="DefaultDialogueAudioConnector"/>
    /// </remarks>
    /// <param name="audioProvider">Audio provider to use</param>
    public class DefaultDialogueAudioConnector(IAudioProvider audioProvider) : IAudioConnector
    {
        /// <summary>
        /// The audio provider to use for the dialogue system
        /// </summary>
        public IAudioProvider AudioProvider = audioProvider;

        internal static HashSet<string> _loadedAudioTress = [];

        /// <inheritdoc/>
        public void OnDialogueTreeLoaded(DialogueTree dialogueTree)
        {
            if (_loadedAudioTress.Contains(dialogueTree.name)) return;
            _loadedAudioTress.Add(dialogueTree.name);
            Dictionary<Task<AudioClip>, DS_StatementNode> taskToStatement = [];
            Dictionary<Task<AudioClip>, DS_MultipleChoiceNode.Choice> taskToChoice = [];
            for (int i = 0; i < dialogueTree.allNodes.Count; i++)
            {
                Il2CppNodeCanvas.Framework.Node node = dialogueTree.allNodes[i];
                DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                if (statement != null)
                {
                    taskToStatement.TryAdd(AudioProvider.GetAudioClip(statement.DLGTree.name, statement.statement.useGlobalLoca ? statement.statement.GlobalLocaPath : dialogueTree.LocaPath, statement.statement.locaKey, statement.actorName, null), statement);
                    continue;
                }
                DS_MultipleChoiceNode mutlipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                if (mutlipleChoice != null)
                {
                    for (int j = 0; j < mutlipleChoice.availableChoices.Count; j++)
                    {
                        DS_MultipleChoiceNode.Choice choice = mutlipleChoice.availableChoices[j];
                        taskToChoice.TryAdd(AudioProvider.GetAudioClip(mutlipleChoice.DLGTree.name, choice.statement.useGlobalLoca ? choice.statement.GlobalLocaPath : dialogueTree.LocaPath, choice.statement.locaKey, mutlipleChoice.actorName, j), choice);
                    }
                }
            }
            Task.WaitAll([.. taskToStatement.Keys]);
            for (int i = 0; i < taskToStatement.Count; i++)
            {
                Task<AudioClip> task = taskToStatement.Keys.ElementAt(i);
                if (task.IsCompleted)
                {
                    taskToStatement[task].statement.audio = task.Result;
                }
                else
                {
                    MelonLogger.Msg($"Failed to load audio for statement {taskToStatement[task].statement.locaKey}");
                }
            }
            Task.WaitAll([.. taskToChoice.Keys]);
            for (int i = 0; i < taskToChoice.Count; i++)
            {
                Task<AudioClip> task = taskToChoice.Keys.ElementAt(i);
                if (task.IsCompleted)
                {
                    taskToChoice[task].statement.audio = task.Result;
                }
                else
                {
                    MelonLogger.Msg($"Failed to load audio for choice {taskToChoice[task].statement.locaKey}");
                }
            }
        }

        /// <inheritdoc/>
        public void OnWorldDialogueStatement(DS_StatementNode statementNode)
        {
            MelonLogger.Msg($"Loading generic Audio for " + statementNode.DLGTree.name);
            Task<AudioClip> task = AudioProvider.GetAudioClip(statementNode.DLGTree.name, statementNode.statement.useGlobalLoca ? statementNode.statement.GlobalLocaPath : statementNode.DLGTree.LocaPath, statementNode.statement.locaKey, statementNode.finalActor.name, null);
            task.Wait();
            if (task.IsCompleted)
            {
                statementNode.statement.audio = task.Result;
            }
            else
            {
                MelonLogger.Msg($"Failed to load audio for statement {statementNode.statement.locaKey}");
            }
        }
    }
}
