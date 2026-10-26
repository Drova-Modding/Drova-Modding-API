using Drova_Modding_API.Systems.Audio;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using UnityEngine;

[HarmonyPatch(typeof(DS_StatementNode), nameof(DS_StatementNode.OnExecute), [typeof(Component), typeof(IBlackboard)])]
internal static class DS_StatementNodePatch
{
    private static void Prefix(Component agent, DS_StatementNode __instance)
    {
        if (__instance.finalActor == null)
        {
            DS_DialogueTreeController component = agent.GetComponent<DS_DialogueTreeController>();
            DS_StatementNode.LoadActorIfNecessary(__instance.DLGTree, __instance.actorName, __instance.finalActor, component);
        }
        if (__instance.statement.audio != null && !DefaultDialogueAudioConnector.HasOwnVoice(__instance.finalActor)) return;
        // A lazy speaker, common for a teammate's scene in co-op, has no body, and the game's IsDead dereferences it.
        DS_DialogueActor? dialogueActor = __instance.finalActor?.TryCast<DS_DialogueActor>();
        Actor? speaker = dialogueActor != null ? dialogueActor.Actor : null;
        if (speaker == null || speaker.IsDead())
        {
            MelonLoader.MelonLogger.Warning("finalActor is null or dead, cannot play audio for DS_StatementNode");
            return;
        }
        AudioManager._dialogueAudioConnector.OnWorldDialogueStatement(__instance);

    }
}

