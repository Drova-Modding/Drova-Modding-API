using Drova_Modding_API.Systems.Audio;
using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;

[HarmonyPatch(typeof(DialogueTree), nameof(DialogueTree.OnAfterDeserialize))]
internal static class DialougeTreePatch
{
    private static void Postfix(DialogueTree __instance)
    {
        AudioManager._dialogueAudioConnector.OnDialogueTreeLoaded(__instance);

    }
}

