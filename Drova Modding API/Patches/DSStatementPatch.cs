using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;

[HarmonyPatch(typeof(DS_Statement), nameof(DS_Statement.BlackboardReplace), [typeof(IBlackboard), typeof(string)])]
internal static class DSStatementPatch
{
    private static void Postfix(IBlackboard bb, string text, ref IStatement __result, DS_Statement __instance)
    {
        __result.Cast<DS_Statement>().audio = __instance.audio;
    }
}


