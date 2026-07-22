using System.Reflection;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.Crafting;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.GUI.LearnGUI;
using Il2CppDrova.InteractionSystem;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.Saveables;
using Il2CppDrova.Talent;
using Il2CppDrova.TradingSystem;
using Il2CppTradingSystem.ItemContainers;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Gameplay events bridged out of Drova via Harmony chokepoints, so mods can subscribe to
    /// plain C# events instead of re-discovering (and re-patching) the same game methods.
    ///
    /// All events fire on the main thread. Subscribe from anywhere; the hooks are applied once
    /// at melon init. Each event's XML doc states which method it hooks and why that method is
    /// the right chokepoint, since most are not the obvious candidate.
    /// </summary>
    public static class GameEvents
    {
        // ------------------------------------------------------------------ combat

        /// <summary>
        /// An entity killed another. Raised from <c>Entity.OnKilledOther</c>, the game's
        /// once-per-kill callback on the KILLER (its only caller is the _isDead-guarded death
        /// block in Health.CheckEvents). The killer is the first argument; the victim is
        /// reachable via <c>args.TargetHealth</c>.
        /// </summary>
        public static event Action<Entity, HealthChangeArgs>? OnEntityKilled;

        /// <summary>
        /// The player killed an actor (not a destructible prop, not themselves). Kills dealt
        /// indirectly (summons, damage-over-time, environment) carry a non-player source and do
        /// not fire; that undercounts rather than misattributes.
        /// </summary>
        public static event Action<Actor>? OnPlayerKilledActor;

        /// <summary>
        /// The player died. Raised from a postfix on
        /// <c>EntityGameHandler.PlayerActorDiedListener</c>. Subscribing to the game's
        /// PlayerActorDiedEvent directly is NOT possible: its argument
        /// <c>EntityGameHandler.EventArgs&lt;Actor&gt;</c> is a non-blittable struct, and
        /// Il2CppInterop's delegate marshaling rejects those outright (verified at runtime).
        /// The postfix needs no delegate marshaling, so it works where the event cannot.
        /// </summary>
        public static event Action? OnPlayerDied;

        // ------------------------------------------------------------------ learning

        /// <summary>
        /// The player bought attribute points at a teacher. Raised from
        /// <c>LearnService.ApplyData</c>, the teacher menu's single commit point; the int is the
        /// net points bought in that session (undo clicks inside the menu cancel out). The
        /// sleep-menu level-up allocation and perma-potions go through other paths and never fire.
        /// </summary>
        public static event Action<LearnService, int>? OnAttributePointsLearned;

        /// <summary>
        /// An actor genuinely learned a talent it did not know. Raised around
        /// <c>TalentActorModule.LearnTalent</c>, whose only callers are the teacher menu,
        /// dialogue-taught talents and LearnUtil. <c>ForceLearnTalent</c> (used by mods granting
        /// talents directly) never fires. LearnTalent silently no-ops on an already-known talent;
        /// a prefix snapshot of CanLearnTalent filters those out.
        /// </summary>
        public static event Action<Actor, TalentContainer>? OnTalentLearned;

        // ------------------------------------------------------------------ quests

        /// <summary>
        /// A quest's state variable changed. The string is the owning GVarList's name.
        /// Primary detector: postfix on <c>AGVar&lt;QuestState&gt;.SetValue</c>, gated on a
        /// preflight (<see cref="QuestStateReader.HasNativeBody"/>) that proves the closed-generic
        /// body was AOT-compiled - patching a body-less generic hard-crashes instead of throwing.
        /// When the preflight fails, the concrete <c>GQuestStateOperation.OperateIntern</c> is
        /// hooked instead, which catches quest-graph and dialogue driven writes.
        /// </summary>
        public static event Action<string, QuestState>? OnQuestStateChanged;

        /// <summary>
        /// A quest reached <see cref="QuestState.IsCompleted"/>. Convenience filter over
        /// <see cref="OnQuestStateChanged"/>; the string is the owning GVarList's name.
        /// </summary>
        public static event Action<string>? OnQuestCompleted;

        /// <summary>
        /// True when <see cref="OnQuestStateChanged"/> is driven by the primary
        /// <c>AGVar&lt;QuestState&gt;.SetValue</c> hook; false when the AOT preflight failed and it
        /// falls back to <c>GQuestStateOperation.OperateIntern</c> (quest-graph and dialogue writes
        /// only). Set once at melon init.
        /// </summary>
        public static bool QuestSetValueHookActive { get; private set; }

        // ------------------------------------------------------------------ looting

        /// <summary>
        /// A loot inventory (chest, container, corpse) was opened. Raised from
        /// <c>Interact_Bhvr_LootInventory.InventoryOpened</c>; the bool is true when an NPC
        /// opened it. <c>Interact_Bhvr_LootKnockout</c> (knocked-out NPC loot) inherits this
        /// method and overrides nothing, so corpse loot funnels through the same event.
        /// </summary>
        public static event Action<Interact_Bhvr_LootInventory, bool>? OnLootInventoryOpened;

        /// <summary>
        /// A runtime-dropped ground pickup (currency, ore) was collected. Raised from
        /// <c>PickupInteraction.InteractionEndedEventListener</c>. Scene-placed pickups such as
        /// plants never reach this path - subscribe to <see cref="OnPickUpOnceLooted"/> for those.
        /// </summary>
        public static event Action<PickupInteraction>? OnPickupCollected;

        /// <summary>
        /// A scene-placed pick-once object (berries, mushrooms, loose world items) was collected.
        /// Raised from <c>Saveable_PickUp_Once.LootAllListener</c> - the component that writes
        /// the persistent "WasPicked" record.
        /// </summary>
        public static event Action<Saveable_PickUp_Once>? OnPickUpOnceLooted;

        /// <summary>
        /// A loot-all interaction ran (the generic "take everything" interaction on world
        /// objects). Raised from <c>Interact_Bhvr_LootAll.LootAll</c>.
        /// </summary>
        public static event Action<Interact_Bhvr_LootAll>? OnLootedAll;

        /// <summary>
        /// A loot-table cache (breakable, hidden stash) dropped its loot for the player. Raised
        /// from <c>SpawnFromLootTable.OnInteractionUsedEventListener</c>. The spawner sits on the
        /// same GameObject as the cache's GuidComponent; the spawned pickups carry runtime
        /// SaveRoot_Dynamic ids, so persist against the SPAWNER's guid, never the pickups'.
        /// </summary>
        public static event Action<SpawnFromLootTable>? OnCacheLooted;

        /// <summary>
        /// The player finished a resource-spot minigame (mining vein, fishing spot). Raised from
        /// <c>Interact_Bhvr_ResourceSpot.MinigameFinishedEventListener</c>.
        /// <c>Interact_Bhvr_ResourceSpot_AI</c> is a separate class on a separate subtree, so NPC
        /// harvesting can never fire this.
        /// </summary>
        public static event Action<Interact_Bhvr_ResourceSpot, MinigameFinishedArgs>? OnResourceMinigameFinished;

        // ------------------------------------------------------------------ trading

        /// <summary>
        /// An item stack left a trader's inventory. Raised from
        /// <c>TraderActorAdapter.RemoveItemFromTrader</c>, which fires exactly once per bought
        /// stack inside MarketPlace.CompleteTrading and never on hover/preview/drag. The same
        /// method also fires on the PLAYER-side adapter when the player sells - distinguish a
        /// purchase by the adapter's actor not being the player.
        /// </summary>
        public static event Action<TraderActorAdapter, ItemTraderStack>? OnTraderItemRemoved;

        // ------------------------------------------------------------------ wiring

        private static bool _initialized;

        // Set by the LearnTalent prefix when the call will actually learn something; consumed by
        // the postfix. Main-thread only, no reentry: LearnTalent does not call itself.
        private static bool _pendingTalentLearn;
        private static Actor? _pendingTalentActor;
        private static TalentContainer? _pendingTalentContainer;

        // ApplyData can fire more than once for a single confirm on the same service instance.
        // Dedup only within the same frame, so repeat purchases on later teacher visits (a new
        // frame, possibly the same reused instance) still register.
        private static IntPtr _lastLearnServiceHandled = IntPtr.Zero;
        private static int _lastLearnServiceFrame = -1;

        internal static void Initialize(HarmonyLib.Harmony harmony)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            Hooking.TryPostfix(harmony, typeof(Entity), nameof(Entity.OnKilledOther),
                typeof(GameEvents), nameof(OnKilledOtherPostfix));
            Hooking.TryPostfix(harmony, typeof(EntityGameHandler), nameof(EntityGameHandler.PlayerActorDiedListener),
                typeof(GameEvents), nameof(PlayerActorDiedListenerPostfix));

            Hooking.TryPostfix(harmony, typeof(LearnService), nameof(LearnService.ApplyData),
                typeof(GameEvents), nameof(LearnApplyDataPostfix));
            Hooking.TryPrefix(harmony, typeof(TalentActorModule), nameof(TalentActorModule.LearnTalent),
                typeof(GameEvents), nameof(LearnTalentPrefix));
            Hooking.TryPostfix(harmony, typeof(TalentActorModule), nameof(TalentActorModule.LearnTalent),
                typeof(GameEvents), nameof(LearnTalentPostfix));

            InitializeQuestHook(harmony);

            Hooking.TryPostfix(harmony, typeof(Interact_Bhvr_LootInventory), nameof(Interact_Bhvr_LootInventory.InventoryOpened),
                typeof(GameEvents), nameof(InventoryOpenedPostfix));
            Hooking.TryPostfix(harmony, typeof(PickupInteraction), nameof(PickupInteraction.InteractionEndedEventListener),
                typeof(GameEvents), nameof(PickupCollectedPostfix));
            Hooking.TryPostfix(harmony, typeof(Saveable_PickUp_Once), nameof(Saveable_PickUp_Once.LootAllListener),
                typeof(GameEvents), nameof(PickUpOnceLootedPostfix));
            Hooking.TryPostfix(harmony, typeof(Interact_Bhvr_LootAll), nameof(Interact_Bhvr_LootAll.LootAll),
                typeof(GameEvents), nameof(LootAllPostfix));
            Hooking.TryPostfix(harmony, typeof(SpawnFromLootTable), nameof(SpawnFromLootTable.OnInteractionUsedEventListener),
                typeof(GameEvents), nameof(CacheLootedPostfix));
            Hooking.TryPostfix(harmony, typeof(Interact_Bhvr_ResourceSpot), nameof(Interact_Bhvr_ResourceSpot.MinigameFinishedEventListener),
                typeof(GameEvents), nameof(ResourceMinigameFinishedPostfix));

            Hooking.TryPostfix(harmony, typeof(TraderActorAdapter), nameof(TraderActorAdapter.RemoveItemFromTrader),
                typeof(GameEvents), nameof(TraderItemRemovedPostfix));
        }

        /// <summary>
        /// The quest state chokepoint is a closed generic; patch it only when its native body
        /// provably exists, otherwise fall back to the concrete operation node (which covers
        /// quest-graph and dialogue writes - the overwhelming majority).
        /// </summary>
        private static void InitializeQuestHook(HarmonyLib.Harmony harmony)
        {
            MethodBase? setValue = null;
            string detail = "could not resolve AGVar<QuestState>.SetValue";
            try
            {
                setValue = AccessTools.Method(typeof(AGVar<QuestState>), nameof(AGVar<QuestState>.SetValue));
            }
            catch (Exception e)
            {
                detail = "resolving AGVar<QuestState>.SetValue threw: " + e.Message;
            }

            bool viable = setValue != null && QuestStateReader.HasNativeBody(setValue, out detail);
            bool patched = viable && Hooking.TryPostfix(harmony, setValue, typeof(GameEvents),
                nameof(QuestSetValuePostfix), "AGVar<QuestState>.SetValue");
            QuestSetValueHookActive = patched;

            if (!patched)
            {
                MelonLogger.Warning("[GameEvents] AGVar<QuestState>.SetValue not patchable (" + detail +
                    "); quest events fall back to GQuestStateOperation.OperateIntern.");
                Hooking.TryPostfix(harmony, typeof(GQuestStateOperation), nameof(GQuestStateOperation.OperateIntern),
                    typeof(GameEvents), nameof(QuestOperateInternPostfix));
            }
        }

        // ------------------------------------------------------------------ patch bodies

        private static void OnKilledOtherPostfix(Entity __instance, HealthChangeArgs changeArgs)
        {
            try
            {
                if (__instance == null || changeArgs == null)
                {
                    return;
                }

                OnEntityKilled?.Invoke(__instance, changeArgs);

                if (OnPlayerKilledActor == null)
                {
                    return;
                }
                if (!PlayerAccess.TryGetPlayer(out Actor player) || __instance.Pointer != player.Pointer)
                {
                    return;
                }
                Health victimHealth = changeArgs.TargetHealth;
                if (!victimHealth)
                {
                    return;
                }
                // A real actor, not a destructible prop, and not the player killing themselves.
                Entity ownerEntity = victimHealth.OwnerEntity;
                Actor? victim = ownerEntity ? ownerEntity.TryCast<Actor>() : null;
                if (victim == null || victim.IsPlayer)
                {
                    return;
                }
                OnPlayerKilledActor.Invoke(victim);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] OnKilledOther postfix failed: " + e);
            }
        }

        private static void PlayerActorDiedListenerPostfix()
        {
            try
            {
                OnPlayerDied?.Invoke();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] PlayerActorDiedListener postfix failed: " + e);
            }
        }

        private static void LearnApplyDataPostfix(LearnService __instance)
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }
                int frame = UnityEngine.Time.frameCount;
                if (__instance.Pointer == _lastLearnServiceHandled && frame == _lastLearnServiceFrame)
                {
                    return;
                }
                _lastLearnServiceHandled = __instance.Pointer;
                _lastLearnServiceFrame = frame;

                int learned = __instance._totalStatsChanged;
                if (learned > 0)
                {
                    OnAttributePointsLearned?.Invoke(__instance, learned);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] LearnService.ApplyData postfix failed: " + e);
            }
        }

        private static void LearnTalentPrefix(TalentActorModule __instance, TalentContainer container)
        {
            _pendingTalentLearn = false;
            try
            {
                if (__instance == null || container == null)
                {
                    return;
                }
                Actor? actor = __instance.GetActor();
                if (actor == null)
                {
                    return;
                }
                _pendingTalentLearn = __instance.CanLearnTalent(container.GUID);
                _pendingTalentActor = actor;
                _pendingTalentContainer = container;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] LearnTalent prefix failed: " + e);
            }
        }

        private static void LearnTalentPostfix()
        {
            try
            {
                if (!_pendingTalentLearn)
                {
                    return;
                }
                _pendingTalentLearn = false;

                if (_pendingTalentActor != null && _pendingTalentContainer != null)
                {
                    OnTalentLearned?.Invoke(_pendingTalentActor, _pendingTalentContainer);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] LearnTalent postfix failed: " + e);
            }
            finally
            {
                _pendingTalentActor = null;
                _pendingTalentContainer = null;
            }
        }

        /// <summary>
        /// The parameter name 't' must match the interop proxy signature.
        /// </summary>
        private static void QuestSetValuePostfix(QuestState t, AGVar<QuestState> __instance)
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }
                // Only handle QuestState variables; the shared generic body can be hit by other
                // enum instantiations, so confirm the concrete type before raising.
                if (__instance.TryCast<GQuestState>() == null)
                {
                    return;
                }
                RaiseQuestChanged(__instance.Cast<AGVarBase>(), t);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] AGVar<QuestState>.SetValue postfix failed: " + e);
            }
        }

        private static void QuestOperateInternPostfix(GQuestState variable, QuestState value)
        {
            try
            {
                if (variable == null)
                {
                    return;
                }
                RaiseQuestChanged(variable.Cast<AGVarBase>(), value);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] GQuestStateOperation.OperateIntern postfix failed: " + e);
            }
        }

        private static void RaiseQuestChanged(AGVarBase gvar, QuestState state)
        {
            // The parent list is a ScriptableObject; go through UnityEngine.Object so a
            // destroyed or foreign parent degrades to "no event" instead of throwing.
            UnityEngine.Object? parent = gvar.GetParent()?.TryCast<UnityEngine.Object>();
            if (!parent)
            {
                return;
            }
            string name = parent!.name;
            OnQuestStateChanged?.Invoke(name, state);
            if (state == QuestState.IsCompleted)
            {
                OnQuestCompleted?.Invoke(name);
            }
        }

        private static void InventoryOpenedPostfix(Interact_Bhvr_LootInventory __instance, bool openedbyNpc)
        {
            try
            {
                if (__instance != null)
                {
                    OnLootInventoryOpened?.Invoke(__instance, openedbyNpc);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] InventoryOpened postfix failed: " + e);
            }
        }

        private static void PickupCollectedPostfix(PickupInteraction __instance)
        {
            try
            {
                if (__instance != null)
                {
                    OnPickupCollected?.Invoke(__instance);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] PickupInteraction postfix failed: " + e);
            }
        }

        private static void PickUpOnceLootedPostfix(Saveable_PickUp_Once __instance)
        {
            try
            {
                if (__instance != null)
                {
                    OnPickUpOnceLooted?.Invoke(__instance);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] Saveable_PickUp_Once postfix failed: " + e);
            }
        }

        private static void LootAllPostfix(Interact_Bhvr_LootAll __instance)
        {
            try
            {
                if (__instance != null)
                {
                    OnLootedAll?.Invoke(__instance);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] LootAll postfix failed: " + e);
            }
        }

        private static void CacheLootedPostfix(SpawnFromLootTable __instance)
        {
            try
            {
                if (__instance != null)
                {
                    OnCacheLooted?.Invoke(__instance);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] SpawnFromLootTable postfix failed: " + e);
            }
        }

        private static void ResourceMinigameFinishedPostfix(Interact_Bhvr_ResourceSpot __instance, MinigameFinishedArgs arg0)
        {
            try
            {
                if (__instance != null)
                {
                    OnResourceMinigameFinished?.Invoke(__instance, arg0);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] ResourceSpot postfix failed: " + e);
            }
        }

        private static void TraderItemRemovedPostfix(TraderActorAdapter __instance, ItemTraderStack itemStack)
        {
            try
            {
                if (__instance != null && itemStack != null)
                {
                    OnTraderItemRemoved?.Invoke(__instance, itemStack);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] RemoveItemFromTrader postfix failed: " + e);
            }
        }
    }
}
