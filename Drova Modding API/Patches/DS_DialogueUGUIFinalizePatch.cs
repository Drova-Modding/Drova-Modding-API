using Drova_Modding_API.Systems.Audio;
using HarmonyLib;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;

/// <summary>
/// Makes a choice node say a voiced answer for this pick, so the game shows it as the player's line and plays its clip,
/// and switches it back off for an answer without a clip. Patched by parameters, since by name this is Object.Finalize.
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
/// A hub whose sub-menu holds a single answer shows that answer in its place, but picking it runs through the hub, which
/// only knows its placeholder. A voiced shown answer is said first, and the hub's selection resumes silently once it ends.
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
