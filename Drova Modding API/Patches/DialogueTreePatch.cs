using Drova_Modding_API.Systems;
using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Editor;
using HarmonyLib;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;

[HarmonyPatch(typeof(Graph), nameof(Graph.SelfDeserialize))]
internal static class DialogueTreePatch
{
    private static void Postfix(Graph __instance)
    {
#if DEBUG
        if (EditorManager.InEditor) return;
#endif
        DialogueTree? dialogueTree = __instance.TryCast<DialogueTree>();
        if (dialogueTree == null) return;
        if (MainThreadDispatcher.IsMainThread)
        {
            AudioManager._dialogueAudioConnector.OnDialogueTreeLoaded(dialogueTree);
        }
        else
        {
            // DS_DialogueTreeController.StartGraphAsync deserializes graphs on a UniTask
            // thread-pool thread. Unity APIs in the audio pipeline (AssetBundle/AudioClip)
            // fail there with a native "Graphics device is null" error.
            MainThreadDispatcher.Enqueue(() => AudioManager._dialogueAudioConnector.OnDialogueTreeLoaded(dialogueTree));
        }
    }
}

[HarmonyPatch(typeof(DialogueTree), nameof(DialogueTree.RequestSubtitles), [typeof(SubtitlesRequestInfo)])]
internal static class DialogueTreePatch_Subtitles
{
    private static void Prefix(SubtitlesRequestInfo info, DialogueTree __instance)
    {
        DS_DialogueActor? speaker = info.actor?.TryCast<DS_DialogueActor>();
        AudioLog.Msg($"Handling subtitle request in DialogueTree for: {(speaker != null ? speaker.name : info.actor?.name)}");
        if (__instance.OnSubtitlesRequest == null)
        {
            AudioLog.Msg("OnSubtitlesRequest is null, will not invoke");
        }
    }
}