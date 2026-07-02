using Drova_Modding_API.Systems.Spawning;
using HarmonyLib;
using Il2CppCysharp.Threading.Tasks;
using Il2CppDrova;
using MelonLoader;

[HarmonyPatch(typeof(Actor), nameof(Actor.AddLoadingComp))]
internal static class ActorAddLoadingCompPatch
{
    private const int MaxDiagnosticLogs = 10;
    private static readonly HashSet<string> SeenMessages = [];
    private static int _diagnosticCount;

    // Reimplements Actor.AddLoadingComp to suppress the game warning while preserving behavior.
#pragma warning disable IDE0051 // Harmony method
    private static bool Prefix(Actor __instance, ActorCompLoading compLoading)
#pragma warning restore IDE0051
    {
        if (__instance._isDestroyed)
        {
            return false;
        }

        if (__instance._isInitialized)
        {
            if (!__instance.IsPlayer)
            {
                LogSuppressedWarning(__instance);
            }

            if (!NpcCreator.IsTrackedSpawn(__instance.transform))
            {
                compLoading.LoadingFunction.Invoke().Forget();
            }
            return false;
        }

        __instance._actorCompLoadingList.Add(compLoading);
        return false;
    }

    private static void LogSuppressedWarning(Actor actor)
    {
        if (_diagnosticCount >= MaxDiagnosticLogs || actor == null)
        {
            return;
        }

        string message = $"Actor already initialized {actor.name}. There shouldn't be any loading component left";
        if (!SeenMessages.Add(message))
        {
            return;
        }

        _diagnosticCount++;
        MelonLogger.Msg($"[AddLoadingComp] Suppressed warning for '{actor.name}': {message}");
    }
}
