using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal class CrimeDialogues : IGenericDialogueHandler
    {
        private const string CrimeDialogueTreeName = "Crime";
        #region Special Dialogue
        private const string SpecialDialogueTreeName = "DT_Dialogue_PlayerHasCrime";
        private const string LineLeader = "Plh_65";
        private const string LineGerdwin = "Plh_9";
        private const string LineGerdwin2 = "Plh_28";
        private const string LineGerdwin3 = "Plh_62";

        private const string LineBounded = "Plh_42";
        private const string LineNementon = "Plh_41";
        #endregion Special Dialogue

        public bool CanHandleDialogue(DialogueTree tree)
        {
            return tree.name.Contains(CrimeDialogueTreeName);
        }

        public void HandleDialogue(DialogueTree tree, StringBuilder stringBuilder, Dictionary<string, int> actorMapping)
        {
            if (tree.name == SpecialDialogueTreeName)
            {
                HandleSpecialDialogue(tree, stringBuilder, actorMapping);
                return;
            }
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statementNode = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statementNode == null) continue;
                for (int j = 0; j < DialogueNameAndFactions.NPCs.Length; j++)
                {
                    string actorName = DialogueNameAndFactions.NPCs[j];
                    stringBuilder
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

        private static void HandleSpecialDialogue(DialogueTree tree, StringBuilder stringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DS_StatementNode statementNode = tree.allNodes[i].TryCast<DS_StatementNode>();
                if (statementNode == null) continue;
                switch (statementNode.statement.locaKey)
                {
                    case LineLeader:
                        HandleLeaderDialogue(statementNode, stringBuilder, actorMapping);
                        break;
                    case LineGerdwin:
                    case LineGerdwin2:
                    case LineGerdwin3:
                        HandleGerdwinDialogue(statementNode, stringBuilder, actorMapping);
                        break;
                    case LineBounded:
                        HandleBoundedDialogue(statementNode, stringBuilder, actorMapping);
                        break;
                    case LineNementon:
                        HandleNementonDialogue(statementNode, stringBuilder, actorMapping);
                        break;
                }

            }
        }

        private static void HandleNementonDialogue(DS_StatementNode statementNode, StringBuilder stringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < DialogueNameAndFactions.NEMENTON.Length; i++)
            {
                string actorName = DialogueNameAndFactions.NEMENTON[i];
                stringBuilder
                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(statementNode.GetLocalizedString())
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(AudioManager.GetUniqueIDStatementGeneric(statementNode.DLGTree, statementNode, actorName))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(DialogueUtils.EMOTION)
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(DialogueUtils.STYLE)
                    .Append(DialogueUtils.SEPERATOR)
                    .AppendLine();
            }
        }

        private static void HandleBoundedDialogue(DS_StatementNode statementNode, StringBuilder stringBuilder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < DialogueNameAndFactions.BOUNDED.Length; i++)
            {
                string actorName = DialogueNameAndFactions.BOUNDED[i];
                stringBuilder
                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(statementNode.GetLocalizedString())
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(AudioManager.GetUniqueIDStatementGeneric(statementNode.DLGTree, statementNode, actorName))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(DialogueUtils.EMOTION)
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(DialogueUtils.STYLE)
                    .Append(DialogueUtils.SEPERATOR)
                    .AppendLine();
            }
        }

        private static void HandleGerdwinDialogue(DS_StatementNode statementNode, StringBuilder stringBuilder, Dictionary<string, int> actorMapping)
        {
            stringBuilder
                .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.NEMENTON_LEADER))
                .Append(DialogueUtils.SEPERATOR)
                .Append(statementNode.GetLocalizedString())
                .Append(DialogueUtils.SEPERATOR)
                .Append(AudioManager.GetUniqueIDStatementGeneric(statementNode.DLGTree, statementNode, DialogueNameAndFactions.NEMENTON_LEADER))
                .Append(DialogueUtils.SEPERATOR)
                .Append(DialogueUtils.EMOTION)
                .Append(DialogueUtils.SEPERATOR)
                .Append(DialogueUtils.STYLE)
                .Append(DialogueUtils.SEPERATOR)
                .AppendLine();
        }

        private static void HandleLeaderDialogue(DS_StatementNode statementNode, StringBuilder stringBuilder, Dictionary<string, int> actorMapping)
        {
            stringBuilder
               .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.NEMENTON_LEADER))
               .Append(DialogueUtils.SEPERATOR)
               .Append(statementNode.GetLocalizedString())
               .Append(DialogueUtils.SEPERATOR)
               .Append(AudioManager.GetUniqueIDStatementGeneric(statementNode.DLGTree, statementNode, DialogueNameAndFactions.NEMENTON_LEADER))
               .Append(DialogueUtils.SEPERATOR)
               .Append(DialogueUtils.EMOTION)
               .Append(DialogueUtils.SEPERATOR)
               .Append(DialogueUtils.STYLE)
               .Append(DialogueUtils.SEPERATOR)
               .AppendLine();
            stringBuilder
               .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.BOUNDED_LEADER))
               .Append(DialogueUtils.SEPERATOR)
               .Append(statementNode.GetLocalizedString())
               .Append(DialogueUtils.SEPERATOR)
               .Append(AudioManager.GetUniqueIDStatementGeneric(statementNode.DLGTree, statementNode, DialogueNameAndFactions.NEMENTON_LEADER))
               .Append(DialogueUtils.SEPERATOR)
               .Append(DialogueUtils.EMOTION)
               .Append(DialogueUtils.SEPERATOR)
               .Append(DialogueUtils.STYLE)
               .Append(DialogueUtils.SEPERATOR)
               .AppendLine();
        }
    }
}
