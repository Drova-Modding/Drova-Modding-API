using Drova_Modding_API.Systems;
using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Dialogues.Store;
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
            // StartGraphAsync deserializes off the main thread, where the audio pipeline fails with "Graphics device is null".
            MainThreadDispatcher.Enqueue(() => AudioManager._dialogueAudioConnector.OnDialogueTreeLoaded(dialogueTree));
        }
    }
}

/// <summary>
/// <c>GraphOwner.GetInstance</c> copies the tree asset on the main thread right before a conversation deserializes the copy
/// on the thread pool, so a stored override patched into the asset here is already in that copy.
/// </summary>
[HarmonyPatch(typeof(GraphOwner), nameof(GraphOwner.GetInstance))]
internal static class GraphOwnerGetInstancePatch
{
    private static void Prefix(Graph originalGraph)
    {
#if DEBUG
        if (EditorManager.InEditor) return;
#endif
        DialogueTree? tree = originalGraph != null ? originalGraph.TryCast<DialogueTree>() : null;
        if (tree != null) DialogueStore.PatchOnFirstUse(tree);
    }
}

/// <summary>
/// Sub-dialogue trees are not copied through <c>GetInstance</c> but through <c>Graph.ClaimFromPoolOrClone</c>, a generic
/// that asks the graph pool first. The pool's non-generic claim is where the template is patched before it is copied.
/// </summary>
[HarmonyPatch(typeof(GraphPoolHandler), nameof(GraphPoolHandler.ClaimGraph))]
internal static class GraphPoolClaimPatch
{
    private static void Prefix(Graph graph)
    {
#if DEBUG
        if (EditorManager.InEditor) return;
#endif
        DialogueTree? tree = graph != null ? graph.TryCast<DialogueTree>() : null;
        if (tree != null) DialogueStore.PatchOnFirstUse(tree);
    }
}

#if DEBUG
[HarmonyPatch(typeof(DialogueTree), nameof(DialogueTree.RequestSubtitles), [typeof(SubtitlesRequestInfo)])]
internal static class DialogueTreePatch_Subtitles
{
    private static void Prefix(SubtitlesRequestInfo info, DialogueTree __instance)
    {
        if (!AudioLog.ShouldLog())
        {
            return;
        }

        DS_DialogueActor? speaker = info.actor?.TryCast<DS_DialogueActor>();
        AudioLog.Msg($"Handling subtitle request in DialogueTree for: {(speaker != null ? speaker.name : info.actor?.name)}");
        if (__instance.OnSubtitlesRequest == null)
        {
            AudioLog.Msg("OnSubtitlesRequest is null, will not invoke");
        }
    }
}
#endif
