using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.Editor;
using HarmonyLib;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using UnityEngine;

/// <summary>
/// Hands a voiced line to the audio handler and the distance falloff. The speaker's dialogue source keeps its last clip, and
/// with playOnAwake Unity replayed it at full volume whenever that speaker's dialogue UI was enabled again (a goodbye heard
/// again far away after the talk), so it is switched off.
/// </summary>
[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.OnSubtitlesRequest), [typeof(SubtitlesRequestInfo)])]
internal static class DSDialogueUGUIPatch
{
    private static void Postfix(SubtitlesRequestInfo info, DS_DialogueUGUI __instance)
    {
#if DEBUG
        if (EditorManager.InEditor) return;
#endif
        if (info.statement.audio == null) {
            AudioLog.Warning($"Audio for actor {info.actor.name} not found with text {info.statement.text}");
            return;
        }

        AudioManager._audioHandler.HandleSubtitleRequest(info, __instance);

        AudioSource audioSource = __instance.localSource;
        if (audioSource != null) audioSource.playOnAwake = false;
        DS_DialogueActor dialogueActor = info.actor.TryCast<DS_DialogueActor>();
        if (audioSource != null && dialogueActor != null)
        {
            DialogueAudioDistanceManager? distanceManager = DialogueAudioDistanceManager.Instance;
            if (distanceManager != null) distanceManager.Register(audioSource, dialogueActor.transform);
        }
    }
}

/// <summary>
/// A voiced line only plays while its bubble is on screen. An answer that ends the talk fades the window out before it
/// is said, and its clip played on with no bubble while the player walked away. A line without a bubble is skipped
/// silently instead.
/// </summary>
[HarmonyPatch(typeof(DS_DialogueUGUI._HandleAudio_d__50), nameof(DS_DialogueUGUI._HandleAudio_d__50.MoveNext))]
internal static class DSDialogueUGUIHandleAudioPatch
{
    /// <summary>
    /// Whether the current dialogue window shows its subtitles on screen: active, not fading out, not hidden, and
    /// inside the camera view. A world dialogue far from the player keeps its bubble above the NPCs, off screen.
    /// </summary>
    internal static bool BubbleShown(DS_DialogueUGUI ui)
    {
        // Without a window manager there is nothing to judge; the line plays as it would unpatched.
        if (ui == null || ui._windowMgr == null) return true;

        Il2CppDrova.GUI.Dialogue.DS_DialogueWindow window = ui._windowMgr.GetCurrentWindow();
        if (window == null || window.IsFadingOut || !window.gameObject.activeInHierarchy || !ui._windowMgr.AreSubtitlesActive()) return false;

        Camera camera = Camera.main;
        if (camera == null) return true;
        Vector3 point = camera.WorldToViewportPoint(window.transform.position);
        return point.x is >= 0f and <= 1f && point.y is >= 0f and <= 1f;
    }

    /// <summary>
    /// Patched on the coroutine's first step: IL2CPP inlines HandleAudio itself, so a patch on it never runs.
    /// </summary>
    private static bool Prefix(DS_DialogueUGUI._HandleAudio_d__50 __instance, ref bool __result)
    {
        if (__instance.__1__state != 0 || BubbleShown(__instance.__4__this)) return true;

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.OnMultipleChoiceRequest), [typeof(MultipleChoiceRequestInfo)])]
internal static class DSDialogueUGUIChoicePatch
{
    private static void Postfix(MultipleChoiceRequestInfo info, DS_DialogueUGUI __instance)
    {
        AudioManager._audioHandler.HandleMultipleChoiceRequest(info, __instance);
    }
}

