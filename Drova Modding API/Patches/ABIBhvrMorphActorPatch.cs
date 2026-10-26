using Drova_Modding_API.Systems.Morphing;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.ABI;
using Il2CppDrova.ActorActions;
using Il2CppDrova.HitSystem;
using Il2CppDrova.InputModules;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(ABI_Bhvr_Morph_Actor), nameof(ABI_Bhvr_Morph_Actor.OnAbilityInit))]
    internal static class ABIBhvrMorphActorInitPatch
    {
        static void Postfix(ABI_Bhvr_Morph_Actor __instance)
        {
            MorphManager.Started(__instance);
        }
    }

    [HarmonyPatch(typeof(ABI_Bhvr_Morph_Actor), nameof(ABI_Bhvr_Morph_Actor.DiedListener))]
    internal static class ABIBhvrMorphActorDiedPatch
    {
        static void Prefix(ABI_Bhvr_Morph_Actor __instance, HealthChangeArgs arg0)
        {
            MorphManager.BodyDied(__instance, arg0);
        }
    }

    [HarmonyPatch(typeof(ABI_Bhvr_Morph_Actor), nameof(ABI_Bhvr_Morph_Actor.OnAbilityUnregister))]
    internal static class ABIBhvrMorphActorUnregisterPatch
    {
        static void Postfix(ABI_Bhvr_Morph_Actor __instance)
        {
            MorphManager.Ended(__instance);
        }
    }

    [HarmonyPatch(typeof(HitFilter_Alignment), nameof(HitFilter_Alignment.Check))]
    internal static class HitFilterAlignmentMorphPatch
    {
        static void Prefix(HitFilter_Alignment __instance, HitTriggerArgs triggerArgs)
        {
            MorphManager.BeforeAlignmentFilter(__instance, triggerArgs);
        }
    }

    /// <summary>
    /// A morphed player cannot interact. What the creature picks up would be lost with its inventory.
    /// </summary>
    [HarmonyPatch(typeof(PlayerInput_Interactions), nameof(PlayerInput_Interactions.UpdateInput))]
    internal static class PlayerInputInteractionsMorphPatch
    {
        static bool Prefix()
        {
            return !MorphManager.IsMorphed;
        }
    }

    [HarmonyPatch(typeof(AA_Trigger_Ability), nameof(AA_Trigger_Ability.TriggerInternal))]
    internal static class AATriggerAbilityMorphPatch
    {
        static bool Prefix(AA_Trigger_Ability __instance)
        {
            return __instance._chargeLevels is not { Count: > 0 } || MorphManager.MaySpawn(__instance._chargeLevels[0], __instance.GetActor());
        }
    }
}
