using Drova_Modding_API.Systems.Audio;
using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;

[HarmonyPatch(typeof(Graph), nameof(Graph.SelfDeserialize))]
internal static class DialougeTreePatch
{
    private static void Postfix(Graph __instance)
    {
        var dialogueTree = __instance.TryCast<DialogueTree>();
        if (dialogueTree == null) return;
        AudioManager._dialogueAudioConnector.OnDialogueTreeLoaded(dialogueTree);
    }
}

