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
        DialogueTree dialogueTree = __instance.TryCast<DialogueTree>();
        if (dialogueTree == null) return;
        AudioManager._dialogueAudioConnector.OnDialogueTreeLoaded(dialogueTree);
    }
}

[HarmonyPatch(typeof(DialogueTree), nameof(DialogueTree.RequestSubtitles), [typeof(SubtitlesRequestInfo)])]
internal static class DialogueTreePatch_Subtitles
{
    private static void Prefix(SubtitlesRequestInfo info, DialogueTree __instance)
    {
        MelonLoader.MelonLogger.Msg($"Handling subtitle request in DialogueTree for: {info.actor.TryCast<DS_DialogueActor>().name}");
        if (__instance.OnSubtitlesRequest == null)
        {
            MelonLoader.MelonLogger.Msg("OnSubtitlesRequest is null, will not invoke");
        }
    }
}