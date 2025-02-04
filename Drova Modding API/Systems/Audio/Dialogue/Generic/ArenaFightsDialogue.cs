using Il2CppNodeCanvas.DialogueTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class ArenaFightsDialogue : IGenericDialogueHandler
    {
        private const string DIALOGUE_NAME = "DT_WorldDialogue_DAS_Arena_MainFights_OnEnd_Winner";
        private const string DIALOGUE_NAME_1 = "DT_WorldDialogue_DAS_Arena_TrainingFights_OnStart";
        private const string DIALOGUE_NAME_2 = "DT_WorldDialogue_DAS_Arena_TrainingFights_OnEnd_Winner";
        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name == DIALOGUE_NAME || tree.name == DIALOGUE_NAME_1 || tree.name == DIALOGUE_NAME_2;
        }
        public void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping)
        {
            if (tree.name.StartsWith("DT_WorldDialogue_DAS_Arena_TrainingFights"))
            {
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    var statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        for (int j = 0; j < DialogueNameAndFactions.ARENA_TRAINING_FIGHTS.Length; j++)
                        {
                            dialogStringBuilder
                                .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.ARENA_TRAINING_FIGHTS[j]))
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(statement.statement)
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.ARENA_TRAINING_FIGHTS[j]))
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
            else
            {
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    var statement = tree.allNodes[i].TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        for (int j = 0; j < DialogueNameAndFactions.ARENA_MAIN_FIGHTS.Length; j++)
                        {
                            dialogStringBuilder
                                .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.ARENA_MAIN_FIGHTS[j]))
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(statement.statement)
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, DialogueNameAndFactions.ARENA_MAIN_FIGHTS[j]))
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
