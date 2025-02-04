using System.Text;
using UnityEngine;
using Il2CppDrova.Routine;
using Il2Cpp;

namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    internal static class ReadOutDialogRoutinePoint
    {
        private const string DT_WorldDialogue_Template = "DT_WorldDialogue_Template";
        private static readonly HashSet<string> alreadyMapped = [];


        public static void GenerateDialogues(Dictionary<string, int> actorMapping, StringBuilder sb)
        {

            var points = UnityEngine.Object.FindObjectsByType<DialogRoutinePoint>(FindObjectsSortMode.None);

            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                var dialogues = point._worldDialogueStarterModule;
                for (int j = 0; j < dialogues._worldDialogueSettings.Args.Count; j++)
                {
                    var arg = dialogues._worldDialogueSettings.Args[j];
                    string actorName;
                    if (arg.Participant == null)
                    {
                        var place = point.GetComponentInParent<RoutinePlaceCondition>(true);
                        if (place == null)
                        {
                            var defaultRoutine = point.GetComponentInParent<DefaultRoutinePlace>(true);
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
                    var id = AudioManager.GetUniqueIDStatementGeneric(DT_WorldDialogue_Template, arg.Key, "", actorName, arg.Path);
                    if (alreadyMapped.Contains(id))
                        continue;
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
