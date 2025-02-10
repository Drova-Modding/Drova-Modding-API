using Il2Cpp;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Dynamic
{
    internal class SleepDialogue
    {
        private static readonly LocalizedString[] localizedStrings = [new LocalizedString("Dialogues/Sleep", "Sleep_01"), new LocalizedString("Dialogues/Sleep", "Sleep_02"), new LocalizedString("Dialogues/Sleep", "Sleep_03")];
        private const string DT_Template_Sleep = "DT_Template_Sleep";

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
                        .Append(AudioManager.GetUniqueIDStatementGeneric(DT_Template_Sleep, localizedStrings[i].Path, "", actorName, localizedStrings[i].Key))
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
