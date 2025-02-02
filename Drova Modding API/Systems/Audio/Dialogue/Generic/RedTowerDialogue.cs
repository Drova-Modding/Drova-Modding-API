using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class RedTowerDialogue : IGenericDialogueHandler
    {
        private const string RED_TOWER_RUINCAMP = "_Ruinenlager";
        private const string RED_TOWER_NEMENTON = "_Nemeton";

        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name.Contains("RedTower");
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            if (tree.name.Contains(RED_TOWER_RUINCAMP))
            {
                HandleDialogueRedTower(tree, dialogStringBuilder, actorMapping, DialogueNameAndFactions.RED_TOWER_RUINCAMP);
            }
            else if (tree.name.Contains(RED_TOWER_NEMENTON))
            {
                HandleDialogueRedTower(tree, dialogStringBuilder, actorMapping, DialogueNameAndFactions.RED_TOWER_NEMENTON);
            }
        }

        private static void HandleDialogueRedTower(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping, string[] npcs)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statementNode = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statementNode == null) continue;
                for (int j = 0; j < npcs.Length; j++)
                {
                    string actorName = npcs[j];
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
