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
    /// Gameplay events bridged out of Drova via Harmony hooks, so mods subscribe instead of re-patching.
    /// All events fire on the main thread.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>
        /// An entity killed another. Raised from <c>Entity.OnKilledOther</c>, the once-per-kill callback on the killer.
        /// The victim is <c>args.TargetHealth</c>.
        /// </summary>
        public static event Action<Entity, HealthChangeArgs>? OnEntityKilled;

        /// <summary>
        /// An entity died, with or without a killer. Raised after <c>Health.CheckEvents</c>, the one place that sets <c>_isDead</c>,
        /// once it really flipped, so a patch keeping the entity alive in between is no death.
        /// </summary>
        public static event Action<Entity>? OnEntityDied;

        /// <summary>
        /// The player killed an actor (not a prop, not themselves). Indirect kills (summons, damage over time) do not fire.
        /// </summary>
        public static event Action<Actor>? OnPlayerKilledActor;

        /// <summary>
        /// The player died. Hooks <c>EntityGameHandler.PlayerActorDiedListener</c> because the game's
        /// PlayerActorDiedEvent passes a non-blittable struct that Il2CppInterop cannot marshal. Only the body the player
        /// controls counts, so a dying morph creature is no player death.
        /// </summary>
        public static event Action? OnPlayerDied;

        /// <summary>
        /// The player bought attribute points at a teacher. The int is the net points of that session.
        /// Level-up allocation and perma-potions do not fire.
        /// </summary>
        public static event Action<LearnService, int>? OnAttributePointsLearned;

        /// <summary>
        /// An actor learned a talent it did not know, via teacher, dialogue or LearnUtil.
        /// <c>ForceLearnTalent</c> does not fire.
        /// </summary>
        public static event Action<Actor, TalentContainer>? OnTalentLearned;

        /// <summary>
        /// A quest's state variable changed. The string is the owning GVarList's name.
        /// </summary>
        public static event Action<string, QuestState>? OnQuestStateChanged;

        /// <summary>
        /// A quest reached <see cref="QuestState.IsCompleted"/>. The string is the owning GVarList's name.
        /// </summary>
        public static event Action<string>? OnQuestCompleted;

        /// <summary>
        /// True when <see cref="OnQuestStateChanged"/> hooks <c>AGVar&lt;QuestState&gt;.SetValue</c>.
        /// False when that generic has no AOT body and the fallback <c>GQuestStateOperation.OperateIntern</c>
        /// only sees quest-graph and dialogue writes.
        /// </summary>
        public static bool QuestSetValueHookActive { get; private set; }

        /// <summary>
        /// A loot inventory (chest, container, knocked-out NPC) was opened. The bool is true when an NPC opened it.
        /// </summary>
        public static event Action<Interact_Bhvr_LootInventory, bool>? OnLootInventoryOpened;

        /// <summary>
        /// A runtime-dropped ground pickup (currency, ore) was collected.
        /// Scene-placed pickups fire <see cref="OnPickUpOnceLooted"/> instead.
        /// </summary>
        public static event Action<PickupInteraction>? OnPickupCollected;

        /// <summary>
        /// A scene-placed pick-once object (berries, mushrooms, loose items) was collected.
        /// </summary>
        public static event Action<Saveable_PickUp_Once>? OnPickUpOnceLooted;

        /// <summary>
        /// A generic loot-all interaction on a world object ran.
        /// </summary>
        public static event Action<Interact_Bhvr_LootAll>? OnLootedAll;

        /// <summary>
        /// A loot-table cache (breakable, hidden stash) dropped its loot.
        /// The spawned pickups get runtime ids, so persist against the spawner's guid.
        /// </summary>
        public static event Action<SpawnFromLootTable>? OnCacheLooted;

        /// <summary>
        /// The player finished a resource-spot minigame (mining vein, fishing spot). NPC harvesting never fires this.
        /// </summary>
        public static event Action<Interact_Bhvr_ResourceSpot, MinigameFinishedArgs>? OnResourceMinigameFinished;

        /// <summary>
        /// An item stack left a trader adapter's inventory, once per traded stack.
        /// Also fires on the player-side adapter when the player sells.
        /// </summary>
        public static event Action<TraderActorAdapter, ItemTraderStack>? OnTraderItemRemoved;

        private static bool _initialized;

        private static bool _pendingTalentLearn;
        private static Actor? _pendingTalentActor;
        private static TalentContainer? _pendingTalentContainer;

        // ApplyData can fire twice for one confirm, so dedup within the same frame only.
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
            Hooking.TryPrefix(harmony, typeof(Health), nameof(Health.CheckEvents),
                typeof(GameEvents), nameof(CheckEventsPrefix));
            Hooking.TryPostfix(harmony, typeof(Health), nameof(Health.CheckEvents),
                typeof(GameEvents), nameof(CheckEventsPostfix));
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

        // Patching a closed generic without an AOT body hard-crashes, so check first and fall back otherwise.
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

        private static void CheckEventsPrefix(Health __instance, out bool __state)
        {
            __state = OnEntityDied != null && __instance != null && !__instance._isDead;
        }

        private static void CheckEventsPostfix(Health __instance, bool __state)
        {
            try
            {
                if (!__state || !__instance._isDead || __instance.OwnerEntity == null) return;

                OnEntityDied?.Invoke(__instance.OwnerEntity);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GameEvents] Health.CheckEvents postfix failed: " + e);
            }
        }

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
                if (!PlayerAccess.TryGetPlayer(out Actor? player) || __instance.Pointer != player.Pointer)
                {
                    return;
                }
                Health victimHealth = changeArgs.TargetHealth;
                if (!victimHealth)
                {
                    return;
                }
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

        private static void PlayerActorDiedListenerPostfix(EntityGameHandler __instance, HealthChangeArgs __0)
        {
            try
            {
                if (__0?.TargetHealth == null || __0.TargetHealth.OwnerEntity != __instance.PlayerActor)
                {
                    return;
                }

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
                // The shared generic body is also hit by other enum instantiations.
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
            GVarList? list = gvar.GetParent();
            UnityEngine.Object? parent = list == null ? null : list.TryCast<UnityEngine.Object>();
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
