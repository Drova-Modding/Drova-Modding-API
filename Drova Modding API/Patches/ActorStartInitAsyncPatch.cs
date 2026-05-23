using Drova_Modding_API.Systems.Spawning;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;

namespace Drova_Modding_API.Patches;

/// <summary>
/// Harmony prefix on <c>Actor.StartInitAsync</c>. When the actor was spawned via a
/// <see cref="LazyActor"/> (<c>IsControlledViaLazy == true</c>), the parent transform is
/// the LazyActor GameObject. Any callbacks registered via
/// <see cref="LazyActorPreInitRegistry.Register"/> are invoked here — before the game's
/// own initialization runs — so module changes (cosmetics, equipment, stats…) take effect.
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


