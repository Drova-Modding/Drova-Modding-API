using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class CombatDialogues : IGenericDialogueHandler
    {
        private const string COMBAT_DIALOGUE = "DT_WorldDialogue_PlayerLoseFight";
        private const string COMBAT_DIALOGUE_1 = "DT_Generic_Combat_WorldDialogues";
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name == COMBAT_DIALOGUE || tree.name == COMBAT_DIALOGUE_1;
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
