using Drova_Modding_API.Systems.Audio;
using HarmonyLib;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;

/// <summary>
/// The player says a voiced answer. Where the window settles the answer picked - a plain choice or a hub's, both pass
/// here - a choice node that would not say it is told to for this pick: the game then shows it as the player's line and
/// plays its clip, as it does for nodes authored to say their answers. A node switched on here is switched back off for
/// an answer without a clip. A hub answer standing in for a nested one is left to <see cref="DS_HubNodeOptionSelectedPatch"/>.
/// By its parameters: by name alone this is Il2CppSystem.Object.Finalize.
/// </summary>
[HarmonyPatch(typeof(DS_DialogueUGUI), "Finalize", [typeof(MultipleChoiceRequestInfo), typeof(int)])]
internal static class DS_DialogueUGUIFinalizePatch
{
    private static readonly HashSet<IntPtr> SwitchedOn = [];

    private static void Prefix(DS_DialogueUGUI __instance, int index)
    {
        DS_MultipleChoiceNode? node = AudioManager.ChoiceNode(__instance);
        if (node == null) return;

        bool nested = DialogueSystemGameHandler.TryGet(out DialogueSystemGameHandler handler) && handler.GetHubHandlerChoice(index) != null;
        bool voiced = !nested && AudioManager.AnswerClip(__instance, index) != null;
        if (voiced && !node.saySelection && SwitchedOn.Add(node.Pointer)) node.saySelection = true;
        else if (!voiced && SwitchedOn.Remove(node.Pointer)) node.saySelection = false;
    }
}

/// <summary>
/// The player says a hub answer that stands in for a nested one. A hub whose sub-menu ("About quests...") would hold a
/// single answer shows that answer in its place ("Where was Jendrik again?"), and picking it runs the selection through
/// the hub, which only knows its own placeholder. When the shown answer - the last of the nested choices the hub
/// recorded - has a clip, it is said first and the hub's selection runs once it ends, without the hub saying anything.
/// </summary>
[HarmonyPatch(typeof(DS_HubNode), nameof(DS_HubNode.OnHubOptionSelected))]
internal static class DS_HubNodeOptionSelectedPatch
{
    private static bool _resuming;

    private static bool Prefix(DS_HubNode __instance, Il2CppSystem.Collections.Generic.List<DS_DialogueHubHandler.HubNodeChoice> hubNodeChoiceStack)
    {
        if (_resuming || hubNodeChoiceStack == null || hubNodeChoiceStack.Count == 0) return true;

        DS_DialogueHubHandler.HubNodeChoice shown = hubNodeChoiceStack[hubNodeChoiceStack.Count - 1];
        DS_MultipleChoiceNode node = shown.TargetNode;
        if (node == null || AudioManager.ChoiceClip(node, shown.ChoiceID) == null) return true;

        DS_Statement statement = node.availableChoices[shown.ChoiceID].statement;
        IStatement line = statement.BlackboardReplace(node.graphBlackboard, node.GetLocalizedString(statement));
        Action resume = () =>
        {
            bool say = __instance.saySelection;
            _resuming = true;
            __instance.saySelection = false;
            try
            {
                __instance.OnHubOptionSelected(hubNodeChoiceStack);
            }
            finally
            {
                __instance.saySelection = say;
                _resuming = false;
            }
        };
        __instance.DLGTree.rootGraph.Cast<DialogueTree>().RequestSubtitles(new SubtitlesRequestInfo(__instance.finalActor, line, resume));
        return false;
    }
}
