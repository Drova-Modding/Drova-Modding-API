using Drova_Modding_API.Systems.Morphing;
using HarmonyLib;
using Il2CppCustomFramework.InputHandling;
using Il2CppDrova;
using Il2CppDrova.ABI;
using Il2CppDrova.ActorActions;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.HitSystem;
using Il2CppDrova.InputModules;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppAction = Il2CppSystem.Action;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// The single gate of every item use, also asked each frame to grey out a spell slot.
    /// A false result can also mean "queued", so this only ever adds a refusal.
    /// </summary>
    [HarmonyPatch(typeof(Item), nameof(Item.CanBeUsedBy), [typeof(Actor), typeof(bool), typeof(Il2CppAction)])]
    internal static class ItemCanBeUsedByMorphPatch
    {
        static bool Prefix(Item __instance, Actor actor, ref bool __result)
        {
            if (MorphManager.MeetsRequirements(__instance, actor))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

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

    /// <summary>
    /// The single funnel of kill experience. It pays the registered player only if that body has player stats.
    /// </summary>
    [HarmonyPatch(typeof(PlayerAttributeStats), nameof(PlayerAttributeStats.CheckPlayerGainExp), [typeof(Actor), typeof(HealthChangeArgs), typeof(int)])]
    internal static class PlayerAttributeStatsMorphExpPatch
    {
        static void Prefix(out Actor? __state)
        {
            __state = MorphManager.PayCasterInstead();
        }

        static void Finalizer(Actor? __state)
        {
            if (__state != null)
            {
                MorphManager.RestorePlayer(__state);
            }
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

    /// <summary>
    /// The weapon swap key of a controlled creature cycles its weapon sets. Read where the key is, since the game's
    /// one-line <c>SwapSets</c> is inlined into this caller and a hook on it never runs.
    /// </summary>
    [HarmonyPatch(typeof(PlayerInput_StanceChange), nameof(PlayerInput_StanceChange.UpdateInput))]
    internal static class PlayerInputStanceChangeMorphPatch
    {
        static void Prefix(PlayerInput_StanceChange __instance)
        {
            Actor? body = MorphManager.Body;
            if (body != null && InputSystem.IsActionClicked(__instance._swapWeaponSetAction))
            {
                MorphManager.CycleWeaponSet(body);
            }
        }
    }

    /// <summary>
    /// An NPC entering a routine place leaves the player's group. While morphed the player is the creature, which has
    /// no group, and the throw would cut the NPC's routine entry short.
    /// </summary>
    [HarmonyPatch(typeof(AIActorModule), nameof(AIActorModule.RemoveActorsFromGroup))]
    internal static class AIActorModuleRemoveFromGroupMorphPatch
    {
        static bool Prefix(AIActorModule __instance)
        {
            return __instance._groupModule != null;
        }
    }

    /// <summary>
    /// The funnel of every dialogue start, also an NPC walking up to the player. A morphed player is not talked to.
    /// The NPC's intent stays set, so it talks once the morph ends.
    /// </summary>
    [HarmonyPatch(typeof(DialogueSystemGameHandler), nameof(DialogueSystemGameHandler.RegisterDialogue))]
    internal static class DialogueSystemRegisterDialogueMorphPatch
    {
        static bool Prefix(IDialogueActor? instigator)
        {
            Actor? body = MorphManager.Body;
            return body == null || instigator == null || instigator.Pointer != body.DialogueModule.GetDialogueActor().Pointer;
        }
    }

    /// <summary>
    /// A cave entrance only tests the player it cached at start, the hidden human while morphed. For that test it sees
    /// the creature instead. The teleport moves the creature, and the human follows it.
    /// </summary>
    [HarmonyPatch(typeof(HitReceiveHandler), nameof(HitReceiveHandler.ManagedUpdate))]
    internal static class HitReceiveHandlerMorphPatch
    {
        static void Prefix(HitReceiveHandler __instance, out Actor? __state)
        {
            __state = null;
            Actor? body = MorphManager.Body;
            if (body == null || !__instance._hasPlayerActor || !IsCaveEntrance(__instance))
            {
                return;
            }

            __state = __instance._playerActor;
            __instance._playerActor = body;
        }

        static void Finalizer(HitReceiveHandler __instance, Actor? __state)
        {
            if (__state != null)
            {
                __instance._playerActor = __state;
            }
        }

        private static bool IsCaveEntrance(HitReceiveHandler handler)
        {
            foreach (AHitReceiveBhvr behaviour in handler._receiveBhvrs)
            {
                if (behaviour.TryCast<HitReceiveBhvr_OW_Teleporter>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
