using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Editor;
using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using MelonLoader;


[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.OnSubtitlesRequest), [typeof(SubtitlesRequestInfo)])]
internal static class DS_DialogueUGUI_Patch
{
    private static void Prefix(SubtitlesRequestInfo info, DS_DialogueUGUI __instance)
    {
        MelonLogger.Msg($"Handling subtitle request for in DS_DialogueUGUI.OnSubtitleRequest : {info.actor.name}");
    }

    private static void Postfix(SubtitlesRequestInfo info, DS_DialogueUGUI __instance)
    {
#if DEBUG
        if (EditorManager.InEditor) return;
#endif
        if (info.statement.audio == null) return;
        AudioManager._audioHandler.HandleSubtitleRequest(info, __instance);
    }
}

[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.UnregisterDLGTreeEvents))]
internal static class DS_DialogueUGUI_Patch_UnregisterDLGTreeEvents
{
    private static void Prefix()
    {
        MelonLogger.Msg("Unregistering DLGTree events");
    }
}

[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.RegisterDLGTreeEvents))]
internal static class DS_DialogueUGUI_Patch_RegisterEvents
{
    private static void Prefix()
    {
        MelonLogger.Msg("Registering DLGTree events");
    }
}