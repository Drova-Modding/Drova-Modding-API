using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class FrogDialogue : IGenericDialogueHandler
    {
        private const string DIALOGUE_NAME = "DT_Frog_Friendly";
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name == DIALOGUE_NAME;
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statement != null)
                {
                    dialogStringBuilder
                        .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.FROG))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(statement.GetLocalizedString())
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.FROG))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.EMOTION)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.STYLE)
                        .Append(DialogueUtils.SEPERATOR)
                        .AppendLine();
                }
            }
        }
    }
}
