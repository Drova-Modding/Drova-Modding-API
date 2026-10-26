using Drova_Modding_API.Systems.Spawning;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;

namespace Drova_Modding_API.Patches;

/// <summary>
/// Runs <see cref="LazyActorPreInitRegistry.Register"/> callbacks for a lazy-spawned actor before the game's own
/// initialization, so module changes take effect. Such an actor's parent is its <see cref="LazyActor"/>.
/// </summary>
[HarmonyPatch(typeof(Actor), nameof(Actor.StartInitAsync))]
internal static class ActorStartInitAsyncPatch
{
    static void Prefix(Actor __instance)
    {
        if (__instance == null || !__instance.IsControlledViaLazy)
            return;

        var parent = __instance.transform.parent;
        if (parent == null)
            return;

        var lazyActor = parent.GetComponent<LazyActor>();
        if (lazyActor == null)
            return;

        LazyActorPreInitRegistry.Invoke(lazyActor, __instance);
    }
}


