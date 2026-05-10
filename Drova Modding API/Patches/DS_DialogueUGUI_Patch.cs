using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Editor;
using HarmonyLib;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using MelonLoader;
using UnityEngine;

[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.OnSubtitlesRequest), [typeof(SubtitlesRequestInfo)])]
internal static class DS_DialogueUGUI_Patch
{
#if DEBUG
    private static void Prefix(SubtitlesRequestInfo info, DS_DialogueUGUI __instance)
    {
        AudioLog.Msg($"Handling subtitle request for in DS_DialogueUGUI.OnSubtitleRequest : {info.actor.name}");
    }
#endif
    private static void Postfix(SubtitlesRequestInfo info, DS_DialogueUGUI __instance)
    {
#if DEBUG
        if (EditorManager.InEditor) return;
#endif
        if (info.statement.audio == null) {
            AudioLog.Warning($"Audio for actor {info.actor.name} not found with text {info.statement.text}");
            return;
        };

        AudioManager._audioHandler.HandleSubtitleRequest(info, __instance);

        // Register the AudioSource with the distance manager so volume is updated every frame.
        AudioSource audioSource = __instance.localSource;
        DS_DialogueActor dialogueActor = info.actor.TryCast<DS_DialogueActor>();
        if (audioSource != null && dialogueActor != null)
        {
            AudioLog.Msg($"Registering audio source for actor {info.actor.name}");
            DialogueAudioDistanceManager.Instance?.Register(audioSource, dialogueActor.transform);
        }
    }
}

//[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.UnregisterDLGTreeEvents))]
//internal static class DS_DialogueUGUI_Patch_UnregisterDLGTreeEvents
//{
//    private static void Prefix()
//    {
//        AudioLog.Msg("Unregistering DLGTree events");
//    }
//}

//[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.RegisterDLGTreeEvents))]
//internal static class DS_DialogueUGUI_Patch_RegisterEvents
//{
//    private static void Prefix()
//    {
//        AudioLog.Msg("Registering DLGTree events");
//    }
//}