using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Dynamic
{
    /// <summary>
    /// Exports the drinking comments for the NPCs that drink. <c>DT_Comment_DrinkWater</c> is authored for a "Player" slot,
    /// but an actor drinking at a water source fills it (<c>IdleLogic_TriggerActionBind</c> with <c>InteractDrink</c>), so
    /// those NPCs say these lines too and each needs a clip keyed with its own name, as the runtime looks it up.
    /// </summary>
    internal class DrinkWaterDialogue
    {
        private const string DT_Comment_DrinkWater = "DT_Comment_DrinkWater";
        private static readonly string[] Drinkers = ["Cengiz"];

        public static void GenerateDialogue(StringBuilder builder, Dictionary<string, int> actorMapping, DialogueTree[] dialogueTrees)
        {
            DialogueTree? tree = dialogueTrees.FirstOrDefault(t => t.name == DT_Comment_DrinkWater);
            if (tree == null) return;

            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                Node node = tree.allNodes[i];
                DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                if (statement == null || (node.inConnections.Count == 0 && node.outConnections.Count == 0)) continue;

                string loca = statement.GetLocalizedString();
                for (int j = 0; j < Drinkers.Length; j++)
                {
                    string actorName = Drinkers[j];
                    builder
                        .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(loca)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(AudioManager.GetUniqueIDStatementGeneric(tree, statement, actorName))
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
