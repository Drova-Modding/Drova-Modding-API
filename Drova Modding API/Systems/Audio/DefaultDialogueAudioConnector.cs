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
                    taskToStatement.TryAdd(AudioProvider.GetAudioClip(statement.DLGTree.name, statement.statement.GlobalLocaPath, statement.statement.locaKey, statement.actorName, null), statement);
                    continue;
                }
                DS_MultipleChoiceNode mutlipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                if (mutlipleChoice != null)
                {
                    for (int j = 0; j < mutlipleChoice.availableChoices.Count; j++)
                    {
                        DS_MultipleChoiceNode.Choice choice = mutlipleChoice.availableChoices[j];
                        if (choice.statement.audio != null) return;

                        taskToChoice.TryAdd(AudioProvider.GetAudioClip(mutlipleChoice.DLGTree.name, choice.statement.GlobalLocaPath, choice.statement.locaKey, mutlipleChoice.actorName, j), choice);
                    }
                }
            }
            Task.WaitAll([.. taskToStatement.Keys]);
            for (int i = 0; i < taskToStatement.Count; i++)
            {
                var task = taskToStatement.Keys.ElementAt(i);
                if (task.IsCompleted)
                {
                    taskToStatement[task].statement.audio = task.Result;
                } else
                {
                    MelonLogger.Msg($"Failed to load audio for statement {taskToStatement[task].statement.locaKey}");
                }

            }
            Task.WaitAll([.. taskToChoice.Keys]);
            for (int i = 0; i < taskToChoice.Count; i++)
            {
                var task = taskToChoice.Keys.ElementAt(i);
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
    }
}
