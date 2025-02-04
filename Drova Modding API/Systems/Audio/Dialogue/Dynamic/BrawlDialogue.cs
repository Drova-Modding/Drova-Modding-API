using Il2Cpp;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Dynamic
{
    internal class BrawlDialogue
    {
        private static readonly LocalizedString[] localizedStrings = [new LocalizedString("BrawlReactions", "haudrauf"), new LocalizedString("BrawlReactions", "reaction01"), new LocalizedString("BrawlReactions", "reaction02"), new LocalizedString("BrawlReactions", "reaction03"), new LocalizedString("BrawlReactions", "reaction04"), new LocalizedString("BrawlReactions", "reaction05"), new LocalizedString("BrawlReactions", "reaction06"), new LocalizedString("BrawlReactions", "reaction07"), new LocalizedString("BrawlReactions", "reaction08"), new LocalizedString("BrawlReactions", "reaction09")];
        private const string DT_WorldDialogue_Template = "DT_WorldDialogue_Template";
        private const string DT_WorldDialogue_PATH = "BrawlReactions";

        public static void GenerateDialogue(StringBuilder builder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < localizedStrings.Length; i++)
            {
                for (int j = 0; j < DialogueNameAndFactions.NPCs.Length; j++)
                {
                    string actorName = DialogueNameAndFactions.NPCs[j];
                    builder
                        .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(localizedStrings[i].GetLocalizedString(null))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(AudioManager.GetUniqueIDStatementGeneric(DT_WorldDialogue_Template, DT_WorldDialogue_PATH, "", actorName, localizedStrings[i].Key))
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
