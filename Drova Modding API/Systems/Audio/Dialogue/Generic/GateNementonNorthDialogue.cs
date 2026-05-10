using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class GateNementonNorthDialogue : IGenericDialogueHandler
    {
        private const string GATE_NEMENTON_NORTH = "Gate_Nemeton_North";
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name.Contains(GATE_NEMENTON_NORTH);
        }
        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statementNode = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statementNode == null) continue;
                string actorName = DialogueNameAndFactions.NEMENTON_GATE_NORTH;
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
