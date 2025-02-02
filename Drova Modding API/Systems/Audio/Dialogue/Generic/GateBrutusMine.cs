using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class GateBrutusMine : IGenericDialogueHandler
    {
        private const string GATE_BRUTUS_MINE = "DT_Gate_BrutusMine";
        private const string MALIS_DIALOGUE = "DT_Gate_BrutusMine_Mali_RelicArea";
        private const string KOLINA_DIALOGUE = "DT_Gate_BrutusMine_Kolina";
        private const string GUARD_DIALOGUE = "DT_Gate_BrutusMine_Guard";
        private const string MININGCHEF_DIALOGUE1 = "DT_Gate_BrutusMine_MineChefLastWall 1";
        private const string MININGCHEF_DIALOGUE = "DT_Gate_BrutusMine_MineChefLastWall";
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name.StartsWith(GATE_BRUTUS_MINE);
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            if (tree.name == MALIS_DIALOGUE)
            {
                HandleBrutusMineDialogue(tree, dialogStringBuilder, actorMapping, DialogueNameAndFactions.BRUTUS_MINE_MALI);
            }
            else if (tree.name == KOLINA_DIALOGUE)
            {
                HandleBrutusMineDialogue(tree, dialogStringBuilder, actorMapping, DialogueNameAndFactions.BRUTUS_MINE_KOLINA);
            }
            else if (tree.name == GUARD_DIALOGUE)
            {
                HandleBrutusMineDialogue(tree, dialogStringBuilder, actorMapping, DialogueNameAndFactions.BRUTUS_MINE_GUARD);
            }
            else if (tree.name == MININGCHEF_DIALOGUE || tree.name == MININGCHEF_DIALOGUE1)
            {
                HandleBrutusMineDialogue(tree, dialogStringBuilder, actorMapping, DialogueNameAndFactions.BRUTUS_MINE_LEADER);
            }
        }

        private static void HandleBrutusMineDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping, string actorName)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statementNode = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statementNode == null) continue;
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
