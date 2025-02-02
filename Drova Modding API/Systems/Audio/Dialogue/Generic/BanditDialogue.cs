using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class BanditDialogue : IGenericDialogueHandler
    {
        private const string BANDIT_DIALOGUE = "DT_WorldDialogue_BanditMineCatch";
        private const string BANDIT_DIALOGUE_1 = "DT_KasernBandit_Worlddialog";
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name == BANDIT_DIALOGUE || tree.name == BANDIT_DIALOGUE_1;
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statementNode = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statementNode == null) continue;
                string actorName = DialogueNameAndFactions.BANDIT;
                dialogStringBuilder
                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(statementNode.GetLocalizedString())
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statementNode, actorName))
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
