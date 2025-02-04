using Il2CppNodeCanvas.DialogueTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class PlayerOustideSoftCircleDialogue : IGenericDialogueHandler
    {
        private const string DIALOGUE_NAME = "DT_WorldDialogue_PlayerOutsideSoftCircleDuringBrawl";

        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name == DIALOGUE_NAME;
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                var statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statement != null)
                {
                    for (int j = 0; j < DialogueNameAndFactions.NPCs.Length; j++)
                    {
                        dialogStringBuilder
                            .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.NPCs[j]))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(statement.GetLocalizedString())
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.NPCs[j]))
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
}
