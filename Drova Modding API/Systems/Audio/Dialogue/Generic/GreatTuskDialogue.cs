using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class GreatTuskDialogue : IGenericDialogueHandler
    {
        private const string DIALOGUE_NAME = "DT_WorldDialogue_GreatTusk";
        private const string BY_PASS_NEMENTON = "Plh_71";
        private const string BY_PASS_RUINCAMP = "Plh_72";

        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name.StartsWith(DIALOGUE_NAME);
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            if (tree.name.EndsWith("Reactions"))
            {
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    DS_StatementNode statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        if (statement.statement.locaKey != BY_PASS_NEMENTON)
                        {
                            for (int j = 0; j < DialogueNameAndFactions.GREAT_TUSK_NEMENTON.Length; j++)
                            {
                                dialogStringBuilder
                                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.GREAT_TUSK_NEMENTON[j]))
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(statement.GetLocalizedString())
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.GREAT_TUSK_NEMENTON[j]))
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(DialogueUtils.EMOTION)
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(DialogueUtils.STYLE)
                                    .Append(DialogueUtils.SEPERATOR)
                                    .AppendLine();
                            }
                        }
                        if (statement.statement.locaKey != BY_PASS_RUINCAMP)
                        {
                            for (int j = 0; j < DialogueNameAndFactions.GREAT_TUSK_RUINCAMP.Length; j++)
                            {
                                dialogStringBuilder
                                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.GREAT_TUSK_RUINCAMP[j]))
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(statement.GetLocalizedString())
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.GREAT_TUSK_RUINCAMP[j]))
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
            else
            {
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    DS_StatementNode statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        for (int j = 0; j < DialogueNameAndFactions.GREAT_TUSK_DRUIDS.Length; j++)
                        {
                            dialogStringBuilder
                                .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.GREAT_TUSK_DRUIDS[j]))
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(statement.GetLocalizedString())
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.GREAT_TUSK_DRUIDS[j]))
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
}
