using Il2Cpp;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Dynamic
{
    internal class BaldoDialogue
    {
        private static readonly LocalizedString[] localizedStrings = [new LocalizedString("Dialog/Corpses/Baldo", "Plh_10"), new LocalizedString("Dialog/Corpses/Baldo", "Plh_11")];
        private const string DT_WorldDialogue_Template = "DT_WorldDialogue_Template";

        public static void GenerateDialogue(StringBuilder builder, Dictionary<string, int> actorMapping)
        {
            for (int i = 0; i < localizedStrings.Length; i++)
            {
                builder
                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, DialogueNameAndFactions.BALDO_AS_ALDO))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(localizedStrings[i].GetLocalizedString(null))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(AudioManager.GetUniqueIDStatementGeneric(DT_WorldDialogue_Template, localizedStrings[i].Path, "", DialogueNameAndFactions.BALDO_AS_ALDO, localizedStrings[i].Key))
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
