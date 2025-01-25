using Il2CppNodeCanvas.DialogueTrees;

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

        /// <inheritdoc/>
        public void OnDialogueTreeLoaded(DialogueTree dialogueTree)
        {
            for (int i = 0; i < dialogueTree.allNodes.Count; i++)
            {
                Il2CppNodeCanvas.Framework.Node node = dialogueTree.allNodes[i];
                StatementNode statement = node.TryCast<StatementNode>();
                if (statement != null)
                {
                    SetAudioForStatement(statement);
                    continue;
                }
                DS_MultipleChoiceNode mutlipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                if (mutlipleChoice != null)
                {
                    SetAudioForMultipleChoice(mutlipleChoice);
                    continue;
                }
                SubDialogueTree subDialogue = node.TryCast<SubDialogueTree>();
                if (subDialogue != null)
                {
                    subDialogue.subGraph.SelfDeserialize();
                    subDialogue.subGraph.DeserializeIfNotDoneYet();
                }
            }
        }

        private async void SetAudioForStatement(StatementNode statement)
        {
            if (statement.statement.audio != null) return;
            statement.statement.audio = await AudioProvider.GetAudioClip(statement.DLGTree.name, statement.UID, statement.actorName, null);
        }

        private async void SetAudioForMultipleChoice(DS_MultipleChoiceNode multipleChoiceNode)
        {
            for (int i = 0; i < multipleChoiceNode.availableChoices.Count; i++)
            {
                DS_MultipleChoiceNode.Choice choice = multipleChoiceNode.availableChoices[i];
                if (choice.statement.audio != null) return;

                choice.statement.audio = await AudioProvider.GetAudioClip(multipleChoiceNode.DLGTree.name, choice.UID, multipleChoiceNode.actorName, i);
            }
        }
    }
}
