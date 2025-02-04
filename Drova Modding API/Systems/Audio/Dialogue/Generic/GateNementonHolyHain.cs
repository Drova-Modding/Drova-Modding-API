using Il2CppNodeCanvas.DialogueTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class GateNementonHolyHain : IGenericDialogueHandler
    {
        private const string DIALOGUE_NAME = "DT_Gate_Nemeton_HolyHain";
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name.StartsWith(DIALOGUE_NAME);
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            if (tree.name.EndsWith("_02"))
            {
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    var statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        dialogStringBuilder
                            .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.NEMENTON_GATE_HAIN_NORTH_2))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(statement.statement)
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.NEMENTON_GATE_HAIN_NORTH_2))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(DialogueUtils.EMOTION)
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(DialogueUtils.STYLE)
                            .Append(DialogueUtils.SEPERATOR)
                            .AppendLine();
                    }
                }
            }
            else if (tree.name.EndsWith("North"))
            {
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    var statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        dialogStringBuilder
                            .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.NEMENTON_GATE_HAIN_NORTH))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(statement.statement)
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.NEMENTON_GATE_HAIN_NORTH))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(DialogueUtils.EMOTION)
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(DialogueUtils.STYLE)
                            .Append(DialogueUtils.SEPERATOR)
                            .AppendLine();
                    }
                }
            }
            else
            {
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    var statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        dialogStringBuilder
                            .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.NEMENTON_GATE_HAIN_SOUTH))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(statement.statement)
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.NEMENTON_GATE_HAIN_SOUTH))
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
