using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;


[HarmonyPatch(typeof(DialogueTree), nameof(DialogueTree.RequestSubtitles), [typeof(SubtitlesRequestInfo)])]
internal static class DialogueTreePatch
{
    private static void Prefix(SubtitlesRequestInfo info, DialogueTree __instance)
    {
        MelonLoader.MelonLogger.Msg($"Handling subtitle request in DialogueTree for: {info.actor.name}");
        if (__instance.OnSubtitlesRequest != null)
        {
            MelonLoader.MelonLogger.Msg("Invoking OnSubtitlesRequest");
        }
        else
        {
            MelonLoader.MelonLogger.Msg("OnSubtitlesRequest is null, will not invoke");
        }
    }
}
