using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;

[HarmonyPatch(typeof(DS_MultipleChoiceNode), nameof(DS_MultipleChoiceNode.OnOptionSelected), [typeof(int)])]
internal static class DS_MultipleChoiceNodePatch
{
    private static bool Prefix(int index, DS_MultipleChoiceNode __instance)
    {
        // NO audio don't do anything
        if (__instance.availableChoices[index].statement.audio == null) return true;
        DS_MultipleChoiceNode.__c__DisplayClass26_0 __c__DisplayClass26_ = new()
        {
            index = index,
            __4__this = __instance
        };
        __instance.status = Il2CppNodeCanvas.Framework.Status.Running;
        Action callback = new(() =>
        {
            __c__DisplayClass26_._OnOptionSelected_b__0();
        });
        string localizedString = __instance.GetLocalizedString(__instance.availableChoices[index].statement);
        IStatement statement = __instance.availableChoices[index].statement.BlackboardReplace(__instance.graphBlackboard, localizedString);
        __instance.DLGTree.rootGraph.TryCast<DialogueTree>().RequestSubtitles(new SubtitlesRequestInfo(__instance.finalActor, statement, callback));
        return false;
    }
}