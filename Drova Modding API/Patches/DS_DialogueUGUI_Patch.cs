using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Editor;
using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;


[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.OnSubtitlesRequest), [typeof(SubtitlesRequestInfo)])]
internal static class DS_DialogueUGUI_Patch
{
    private static void Postfix(SubtitlesRequestInfo info, DS_DialogueUGUI __instance)
    {
#if DEBUG
        if (EditorManager.InEditor) return;
#endif
        if (info.statement.audio == null) return;
        AudioManager._audioHandler.HandleSubtitleRequest(info, __instance);
    }
}

