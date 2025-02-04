using Il2CppNodeCanvas.DialogueTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class AfterCombatPlayerDialogue : IGenericDialogueHandler
    {
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name.StartsWith("DT_Dialogue_AfterCombat_Player");
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statementNode = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statementNode == null) continue;
                for (int j = 0; j < DialogueNameAndFactions.NPCs.Length; j++)
                {
                    string actorName = DialogueNameAndFactions.NPCs[j];
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
}
