using Il2Cpp;
using Il2CppDrova.Routine;
using MelonLoader;
using System.Text;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    internal static class ReadOutDialogRoutinePoint
    {
        private const string DT_WorldDialogue_Template = "DT_WorldDialogue_Template";
        private static readonly HashSet<string> alreadyMapped = [];

        public static void GenerateDialogues(Dictionary<string, int> actorMapping, StringBuilder sb)
        {

            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<DialogRoutinePoint> points = UnityEngine.Object.FindObjectsOfType<DialogRoutinePoint>(true);

            for (int i = 0; i < points.Length; i++)
            {
                DialogRoutinePoint point = points[i];
                Il2CppDrova.DialogueNew.WorldDialogueStarterModule dialogues = point._worldDialogueStarterModule;

                if (dialogues._worldDialogueSettings.Args.Count == 0)
                {
                    MelonLogger.Msg("No dialogues found for: " + point.name + " "  + point.transform.parent.name + " " + point.transform.parent.parent.name);
                }
            

                for (int j = 0; j < dialogues._worldDialogueSettings.Args.Count; j++)
                {
                    Il2CppDrova.DialogueNew.WorldDialogueArg arg = dialogues._worldDialogueSettings.Args[j];
                    string actorName;
                    if (arg.Participant == null)
                    {
                        RoutinePlaceCondition place = point.GetComponentInParent<RoutinePlaceCondition>(true);
                        if (place == null)
                        {
                            DefaultRoutinePlace defaultRoutine = point.GetComponentInParent<DefaultRoutinePlace>(true);
                            if (defaultRoutine != null)
                            {
                                actorName = defaultRoutine._entity._locaName.GetLocalizedString(null);
                            }
                            else
                                actorName = "404 Not found :/";
                        }
                        else
                        {
                            actorName = place._entityInfo._locaName.GetLocalizedString(null);
                        }
                    }
                    else
                    {
                        actorName = arg.Participant._locaName.GetLocalizedString(null);
                    }
                    string id = AudioManager.GetUniqueIDStatementGeneric(DT_WorldDialogue_Template, arg.Path, "", actorName, arg.Key);
                    if (alreadyMapped.Contains(id)) { 
                        //MelonLogger.Msg("Already added: " + id);
                        continue;
                    }
                    alreadyMapped.Add(id);
                    sb
                              .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                              .Append(DialogueUtils.SEPERATOR)
                              .Append(new LocalizedString(arg.Path, arg.Key).GetLocalizedString(null))
                              .Append(DialogueUtils.SEPERATOR)
                              .Append(id)
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
