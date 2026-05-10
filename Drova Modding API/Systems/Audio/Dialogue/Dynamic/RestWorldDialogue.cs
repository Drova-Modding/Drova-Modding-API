using Il2Cpp;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Dynamic
{
    internal class RestWorldDialogue
    {
        private const string DT_WorldDialogue_Template = "DT_WorldDialogue_Template";
        private static readonly LocalizedString[] localizedStringsForHadewin = [new LocalizedString("RestWorldDialogue", "Working_62"), new LocalizedString("RestWorldDialogue", "Working_63")];
        private static readonly LocalizedString[] localizedStringsForFlunka = [new LocalizedString("RestWorldDialogue", "Working_60"), new LocalizedString("RestWorldDialogue", "Working_59")];
        private static readonly LocalizedString[] localizedStringsForMiranda = [new LocalizedString("RestWorldDialogue", "Working_61")];
        private static readonly LocalizedString[] localizedStringsForMorvin = [new LocalizedString("RestWorldDialogue", "FreetimeWorking_74"), new LocalizedString("RestWorldDialogue", "FreetimeWorking_75"), new LocalizedString("RestWorldDialogue", "FreetimeWorking_76"), new LocalizedString("RestWorldDialogue", "FreetimeWorking_77")];
        private static readonly LocalizedString[] localizedStringsForBaldo = [new LocalizedString("RestWorldDialogue", "Freetime_50"), new LocalizedString("RestWorldDialogue", "Freetime_51")];

        public static void GenerateDialogue(StringBuilder builder, Dictionary<string, int> actorMapping)
        {
            GenerateDialogue(builder, actorMapping, DialogueNameAndFactions.RUINCAMP_HADEWIN, localizedStringsForHadewin);
            GenerateDialogue(builder, actorMapping, DialogueNameAndFactions.RUINCAMP_FLUNKA, localizedStringsForFlunka);
            GenerateDialogue(builder, actorMapping, DialogueNameAndFactions.RUINCAMP_MIRANDA, localizedStringsForMiranda);
            GenerateDialogue(builder, actorMapping, DialogueNameAndFactions.RUINCAMP_PROXIMUS, localizedStringsForMorvin);
            GenerateDialogue(builder, actorMapping, DialogueNameAndFactions.BALDO_AS_ALDO, localizedStringsForBaldo);
        }

        private static void GenerateDialogue(StringBuilder builder, Dictionary<string, int> actorMapping, string npc, LocalizedString[] localizedStrings)
        {
            for (int i = 0; i < localizedStrings.Length; i++)
            {
                builder
                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, npc))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(localizedStrings[i].GetLocalizedString(null))
                    .Append(DialogueUtils.SEPERATOR)
                    .Append(AudioManager.GetUniqueIDStatementGeneric(DT_WorldDialogue_Template, localizedStrings[i].Path, "", npc, localizedStrings[i].Key))
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
