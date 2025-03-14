using Il2Cpp;
using Il2CppDrova.HitSystem;
using System.Text;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    internal class ReadOutDialogueHitBehaviour
    {
        private const string DT_WorldDialogue_Template = "DT_WorldDialogue_Template";
        private static readonly HashSet<string> alreadyMapped = [];

        internal static void GenerateHitBvhrDialogues(Dictionary<string, int> actorMapping, StringBuilder sb)
        {
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<HitReceiveBhvr_WorldDialogue> bhvrs = UnityEngine.Object.FindObjectsByType<HitReceiveBhvr_WorldDialogue>(FindObjectsSortMode.None);
            for (int i = 0; i < bhvrs.Count; i++)
            {
                var bvhr = bhvrs[i];

                Il2CppDrova.DialogueNew.WorldDialogueStarterModule dialogues = bvhr._dialogueStarterModule;
                if(dialogues._worldDialogueSettings.)
                for (int j = 0; j < dialogues._worldDialogueSettings.Args.Count; j++)
                {
                    Il2CppDrova.DialogueNew.WorldDialogueArg arg = dialogues._worldDialogueSettings.Args[j];
                    string actorName;
                    if (arg.Participant == null)
                    {
                        var entity = bvhr._dialogueOwnerEntity;
                        if (entity != null)

                            actorName = entity._locaName.GetLocalizedString(null);
                        else
                            actorName = "404 Not found :/";
                    }
                    else
                    {
                        actorName = arg.Participant._locaName.GetLocalizedString(null);
                    }
                    string id = AudioManager.GetUniqueIDStatementGeneric(DT_WorldDialogue_Template, arg.Key, "", actorName, arg.Path);
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
