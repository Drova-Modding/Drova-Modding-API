using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Drova_Modding_API.Access;
using Il2CppDrova;
using Il2CppDrova.ABI;
using Il2CppDrova.ActorActions;
using Il2CppDrova.AI.Logic;
using Il2CppDrova.AI.Modules;
using Il2CppDrova.Alignment;
using Il2CppDrova.HitSystem;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppOpenWorldSystem;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Morphing
{
    /// <summary>
    /// Morph spells that turn the player into a creature for a while. Built on the game's unused morph ability
    /// <c>ABI_Bhvr_Morph_Actor</c>, which swaps the controls through <c>MorphUtil</c> and blocks saving.
    /// While morphed <see cref="PlayerAccess.GetPlayer"/> returns the creature. <see cref="Caster"/> is the real player body.
    /// </summary>
    public static class MorphManager
    {
        private const float InitTimeout = 2f;

        /// <summary>The game's Insane-only weapon set. EquipIndex has no name for it.</summary>
        private const int InsaneSet = 14;

        private static readonly Dictionary<string, Actor> CreaturesByGuid = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<IntPtr, (float Duration, ABI_Bhvr_Morph_Actor Morph, string Guid)> MorphsByAbility = new();
        private static readonly Dictionary<IntPtr, (float HealthCost, int RequiredLevel)> CostsByItem = new();
        private static readonly Dictionary<IntPtr, float> HealthCostByAbility = new();
        private static readonly HashSet<string> Loading = new(StringComparer.OrdinalIgnoreCase);

        // Guids that named no actor prefab, so a caller asking every frame does not start a load every frame.
        private static readonly HashSet<string> Failed = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<IntPtr> HostileKinds = new();
        private static readonly Dictionary<IntPtr, Il2CppCollections.List<AI_Equipment.EquipData>> SetsByBody = new();
        private static ABI_Bhvr_Morph_Actor? _active;
        private static bool _bodyDied;
        private static Entity? _killer;

        /// <summary>
        /// A morph started, with the hidden caster and the creature the player controls. Raised once the creature is
        /// initialized and armed, so buffs and stat changes on it hold.
        /// </summary>
        public static event Action<Actor, Actor>? OnMorphStarted;

        /// <summary>
        /// A morph ended and the caster has the controls again. Not raised when a load ends it.
        /// When the creature died, the caster dies right after, unless <see cref="SpareCaster"/> says otherwise.
        /// </summary>
        public static event Action<Actor>? OnMorphEnded;

        /// <summary>
        /// Optional, asked when a morph starts: caster, creature guid and the spell's duration in, the duration to use out.
        /// </summary>
        public static Func<Actor, string, float, float>? AdjustDuration { get; set; }

        /// <summary>
        /// Optional, asked when the creature died, with the caster. True keeps the caster alive with the health it had.
        /// </summary>
        public static Func<Actor, bool>? SpareCaster { get; set; }

        /// <summary>
        /// Optional veto, asked when a morph spell is cast. False cancels the morph, the cast animation still plays.
        /// </summary>
        public static Func<bool>? CanMorph { get; set; }

        /// <summary>
        /// The hidden player body during a morph, or null.
        /// </summary>
        public static Actor? Caster { get; private set; }

        /// <summary>
        /// The creature the player controls during a morph, or null.
        /// </summary>
        public static Actor? Body { get; private set; }

        /// <summary>
        /// Addressable guid of the creature during a morph, or null.
        /// </summary>
        public static string? BodyGuid { get; private set; }

        /// <summary>
        /// The <c>EquipIndex</c> value of the controlled creature's weapon set, which the weapon swap key cycles. -1 when not morphed.
        /// </summary>
        public static int WeaponSet { get; private set; } = -1;

        /// <summary>
        /// Whether the player is morphed right now.
        /// </summary>
        public static bool IsMorphed => _active != null && Body != null;

        /// <summary>
        /// Ends the running morph early. False when the player is not morphed.
        /// </summary>
        public static bool EndMorph()
        {
            if (!IsMorphed || _active!.Controller == null)
            {
                return false;
            }

            _active.Controller.UnregisterAbility();
            return true;
        }

        /// <summary>
        /// Builds a copy of <paramref name="template"/> that morphs the caster into the creature.
        /// The creature loads in the background. A cast before it is loaded does nothing.
        /// </summary>
        /// <param name="template">An ability of the spell. Its data is kept, its controller replaced.</param>
        /// <param name="creature">Addressable guid of the creature, e.g. <c>AddressableAccess.Creatures.Ripper.AssetGUID</c>.</param>
        /// <param name="duration">Seconds the morph lasts. 0 lasts until the creature dies.</param>
        /// <param name="holder">An inactive parent for the controller prefab.</param>
        /// <returns>The ability, or null without a creature.</returns>
        public static ABI_Ability? CreateAbility(ABI_Ability template, string creature, float duration, Transform holder)
        {
            if (string.IsNullOrWhiteSpace(creature))
            {
                return null;
            }

            GameObject root = new("ABI_Morph_" + creature);
            root.transform.SetParent(holder, false);
            ABI_Controller controller = root.AddComponent<ABI_Controller>();
            ABI_Bhvr_Morph_Actor morph = root.AddComponent<ABI_Bhvr_Morph_Actor>();
            controller._abilityBhvrs = new Il2CppReferenceArray<ABI_ABhvr>([morph]);
            MelonCoroutines.Start(LoadCreature(creature, morph));

            ABI_Ability ability = Object.Instantiate(template);
            ability.name = root.name;
            ability.hideFlags = HideFlags.DontUnloadUnusedAsset;
            ability._weaponSpecificPrefabs.Clear();
            ability.SetAbilityPrefab(controller);
            MorphsByAbility[ability.Pointer] = (duration, morph, creature);
            return ability;
        }

        /// <summary>
        /// False for a morph ability whose creature is not loaded yet, that <see cref="CanMorph"/> vetoes, or that anybody
        /// but the player casts. The game's morph always takes the player's controls, also when a co-op puppet replays it.
        /// </summary>
        internal static bool MaySpawn(ABI_Ability? ability, Actor? caster)
        {
            if (ability == null || !MorphsByAbility.TryGetValue(ability.Pointer, out (float Duration, ABI_Bhvr_Morph_Actor Morph, string Guid) entry))
            {
                return true;
            }

            if (caster == null || !PlayerAccess.TryGetPlayer(out Actor? player) || player.Pointer != caster.Pointer)
            {
                return false;
            }

            if (entry.Morph == null || entry.Morph._targetActor == null || CanMorph?.Invoke() == false)
            {
                return false;
            }

            // Charged here, where the creature really spawns. Health can have dropped since the cast began.
            if (!HealthCostByAbility.TryGetValue(ability.Pointer, out float percent) || percent <= 0f)
            {
                return true;
            }

            Health health = caster.GetHealth();
            if (health == null)
            {
                return false;
            }

            int cost = HealthCostOf(health, percent);
            return health.CurHealth > cost && DamageAccess.TryGetPureDamageType(out DamageStatDesc? pure) && DamageAccess.TryApplyDamage(health, cost, pure);
        }

        /// <summary>
        /// Gives a morph item the cost and level its <see cref="Items.CustomMorphDefinition"/> asks for.
        /// </summary>
        internal static void SetCost(Item item, ABI_Ability ability, float healthCost, int requiredLevel)
        {
            CostsByItem[item.Pointer] = (healthCost, requiredLevel);
            HealthCostByAbility[ability.Pointer] = healthCost;
        }

        internal static void ClearCost(Item item)
        {
            CostsByItem.Remove(item.Pointer);
        }

        /// <summary>
        /// False while the player lacks the level or health a morph item asks for, which greys its spell slot out.
        /// Asked every frame for each slot, so it is one lookup for any other item.
        /// </summary>
        internal static bool MeetsRequirements(Item item, Actor? actor)
        {
            if (!CostsByItem.TryGetValue(item.Pointer, out (float HealthCost, int RequiredLevel) cost) || actor == null)
            {
                return true;
            }

            // A co-op puppet replays the owner's cast with its own stats. MaySpawn stops it from morphing anyway.
            if (!PlayerAccess.TryGetPlayer(out Actor? player) || player.Pointer != actor.Pointer)
            {
                return true;
            }

            if (cost.RequiredLevel > 0 && actor.GetStats().Level < cost.RequiredLevel)
            {
                return false;
            }

            Health health = actor.GetHealth();
            return cost.HealthCost <= 0f || (health != null && health.CurHealth > HealthCostOf(health, cost.HealthCost));
        }

        private static int HealthCostOf(Health health, float percent)
        {
            return Mathf.CeilToInt(health.MaxHealth * percent / 100f);
        }

        /// <summary>
        /// The creature prefab with that addressable guid. False until it is loaded, and the first call starts the load.
        /// </summary>
        public static bool TryGetCreature(string guid, [NotNullWhen(true)] out Actor? prefab)
        {
            if (CreaturesByGuid.TryGetValue(guid, out prefab) && prefab != null)
            {
                return true;
            }

            prefab = null;
            if (!string.IsNullOrWhiteSpace(guid) && !Loading.Contains(guid) && !Failed.Contains(guid))
            {
                MelonCoroutines.Start(LoadCreature(guid, null));
            }

            return false;
        }

        /// <summary>
        /// One load per creature, kept for the session. Not <c>WaitForCompletion</c>, which can hang while other loads run.
        /// </summary>
        private static IEnumerator LoadCreature(string guid, ABI_Bhvr_Morph_Actor? morph)
        {
            if (!CreaturesByGuid.TryGetValue(guid, out Actor? actor) || actor == null)
            {
                Loading.Add(guid);
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(guid);
                while (!handle.IsDone)
                {
                    yield return null;
                }

                Loading.Remove(guid);
                GameObject? prefab = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
                actor = prefab != null ? prefab.GetComponent<Actor>() : null;
                if (actor == null)
                {
                    Failed.Add(guid);
                    MelonLogger.Warning($"[Morph] '{guid}' is no addressable guid of an actor prefab.");
                    yield break;
                }

                CreaturesByGuid[guid] = actor;
            }

            if (morph != null)
            {
                morph._targetActor = actor;
                morph.gameObject.name = "ABI_Morph_" + actor.name;
            }
        }

        /// <summary>
        /// After the game spawned the creature and gave it the controls.
        /// </summary>
        internal static void Started(ABI_Bhvr_Morph_Actor morph)
        {
            ABI_Controller? controller = morph.Controller;
            Entity? owner = controller != null ? controller.AbilityInfo.GetOwner() : null;
            Actor? caster = owner != null ? owner.TryCast<Actor>() : null;
            Actor? body = morph._instance;
            if (controller == null || body == null || caster == null)
            {
                return;
            }

            AddReferencePoint(body);

            // Like a conjured creature: its kills pay the player, its own death pays nobody.
            body.CustomStats.SetGivesPlayerExp(true);
            body.GetStats().SetExpOnDead(0);
            body.GetStats().SetBrawlExp(0);
            _active = morph;
            Caster = caster;
            Body = body;
            ABI_Ability? ability = controller.AbilityInfo.Ability;
            float duration = 0f;
            if (ability != null && MorphsByAbility.TryGetValue(ability.Pointer, out (float Duration, ABI_Bhvr_Morph_Actor Morph, string Guid) entry))
            {
                duration = entry.Duration;
                BodyGuid = entry.Guid;
            }

            if (AdjustDuration != null && BodyGuid != null)
            {
                duration = Math.Max(0f, AdjustDuration(caster, BodyGuid, duration));
            }

            MelonCoroutines.Start(Follow(morph, caster, body, duration));
        }

        /// <summary>
        /// The creature died. Runs before the game ends the morph.
        /// </summary>
        internal static void BodyDied(ABI_Bhvr_Morph_Actor morph, HealthChangeArgs args)
        {
            if (_active != null && _active.Pointer == morph.Pointer)
            {
                _bodyDied = true;
                _killer = args.HealthChange != null ? args.HealthChange.Source : null;
            }
        }

        /// <summary>
        /// After the game gave the caster the controls back. The creature stays hidden until the scene unloads.
        /// NPC routines that looked at the player during the morph still hold it and throw once it is destroyed.
        /// </summary>
        internal static void Ended(ABI_Bhvr_Morph_Actor morph)
        {
            if (_active == null || _active.Pointer != morph.Pointer)
            {
                return;
            }

            Actor? caster = Caster;
            bool died = _bodyDied;
            Entity? killer = _killer;
            Clear();
            if (caster == null)
            {
                return;
            }

            OnMorphEnded?.Invoke(caster);
            if (died && SpareCaster?.Invoke(caster) != true)
            {
                MelonCoroutines.Start(KillCaster(caster, killer));
            }
        }

        /// <summary>
        /// The creature's death is the player's. A lethal blow from the same killer, one frame after the controls are
        /// back, so revive guards such as co-op's downed state catch it like any other.
        /// </summary>
        private static IEnumerator KillCaster(Actor caster, Entity? killer)
        {
            yield return null;
            if (caster == null || !DamageAccess.TryGetPureDamageType(out DamageStatDesc? pure))
            {
                yield break;
            }

            Health health = caster.GetHealth();
            if (health != null && !health.IsDead)
            {
                DamageAccess.TryApplyDamage(health, health.MaxHealth + 1, pure, killer);
            }
        }

        /// <summary>
        /// Carries the hidden caster along, so systems that cached the first player body follow the creature.
        /// Ends the morph after its duration in game time.
        /// </summary>
        private static IEnumerator Follow(ABI_Bhvr_Morph_Actor morph, Actor caster, Actor body, float duration)
        {
            IntPtr pointer = morph.Pointer;
            float endsAt = Time.time + duration;
            yield return null;

            // The creature initializes after the morph ability, and buffs and stats refuse an uninitialized actor.
            float initDeadline = Time.time + InitTimeout;
            while (body != null && !body.IsInitialized && Time.time < initDeadline)
            {
                yield return null;
            }

            if (body != null && _active != null && _active.Pointer == pointer)
            {
                EquipCreatureWeapon(body);
                try
                {
                    OnMorphStarted?.Invoke(caster, body);
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[Morph] an OnMorphStarted listener failed: " + e);
                }
            }

            while (morph != null && caster != null && body != null && _active != null && _active.Pointer == pointer)
            {
                caster.SetPos(body.GetFeetPosition());
                if (duration > 0f && Time.time >= endsAt && morph.Controller != null)
                {
                    morph.Controller.UnregisterAbility();
                    yield break;
                }

                yield return null;
            }

            if (_active != null && _active.Pointer == pointer)
            {
                Clear();
            }
        }

        /// <summary>
        /// Equips a creature's first weapon set, which binds its attacks to the attack keys, and its special attack to the
        /// dodge key. Its AI equips it only from the combat graph, and with the AI off <c>AI_Equipment</c> never starts.
        /// </summary>
        public static void EquipCreatureWeapon(Actor body)
        {
            if (!EquipCreatureWeaponSet(body, -1))
            {
                MelonLogger.Warning($"[Morph] '{body.name}' has no weapon it could equip, it cannot attack.");
            }
        }

        /// <summary>
        /// Equips the creature's weapon set with that <c>EquipIndex</c> value, or its first one for -1. The special set
        /// (<c>Custom_1</c>) goes on the dodge key instead, and the set the game uses only on Insane is left out.
        /// </summary>
        /// <returns>False when the creature has no such set, or the game refused an item of it for now (call again later).</returns>
        public static bool EquipCreatureWeaponSet(Actor body, int index)
        {
            Il2CppCollections.List<AI_Equipment.EquipData>? sets = GetWeaponSets(body);
            AI_Equipment.EquipData? set = null;
            if (sets != null)
            {
                foreach (AI_Equipment.EquipData data in sets)
                {
                    if (IsAttackSet(data) && (index < 0 || (int)data.Index == index))
                    {
                        set = data;
                        break;
                    }
                }
            }

            if (sets == null || set == null)
            {
                return false;
            }

            Actor.ActionModule actions = body.GetActionModule();
            IInventoryContainer inventory = body.GetInventoryActorModule().GetDefaultInventory();
            bool unbound = false;
            bool equipped = true;
            foreach (AI_Equipment.SlotData slot in set.Equipment)
            {
                Il2CppCollections.List<ActorEquipSlot>? slots = slot.Item != null ? body.GetEquipmentModule().GetEquipmentSlotsOfType(slot.SlotType) : null;
                if (slots == null || slots.Count == 0 || slots[0].GetItemOnSlot() == slot.Item)
                {
                    continue;
                }

                // A set without a secondary attack must not keep the last set's one. An item already worn keeps its binds.
                if (!unbound)
                {
                    actions.UnbindActionType(Actor.ActionModule.EActionBind.PrimaryAttack);
                    actions.UnbindActionType(Actor.ActionModule.EActionBind.SecondaryAttack);
                    unbound = true;
                }

                if (!inventory.HasAtLeastItemInInventory(slot.Item, 1))
                {
                    inventory.AddItem(new ItemStack(slot.Item, 1), false, false);
                }

                equipped &= slots[0].TryEquip(slot.Item);
            }

            // The harpy carries its only melee attack on the secondary key.
            if (!actions.IsActionBinded(Actor.ActionModule.EActionBind.PrimaryAttack) && actions.IsActionBinded(Actor.ActionModule.EActionBind.SecondaryAttack))
            {
                actions.BindActionType(actions._actionTypeToPrefabMap[Actor.ActionModule.EActionBind.SecondaryAttack], Actor.ActionModule.EActionBind.PrimaryAttack);
            }

            BindSpecialAttack(body, sets);
            if (equipped && Body != null && Body.Pointer == body.Pointer)
            {
                WeaponSet = (int)set.Index;
            }

            return equipped;
        }

        /// <summary>
        /// Moves the controlled creature on to its next weapon set, from the weapon swap key.
        /// </summary>
        internal static void CycleWeaponSet(Actor body)
        {
            Il2CppCollections.List<AI_Equipment.EquipData>? all = GetWeaponSets(body);
            if (all == null)
            {
                return;
            }

            List<int> sets = [];
            foreach (AI_Equipment.EquipData data in all)
            {
                if (IsAttackSet(data) && !sets.Contains((int)data.Index))
                {
                    sets.Add((int)data.Index);
                }
            }

            int next = sets.Count > 0 ? sets[(sets.IndexOf(WeaponSet) + 1) % sets.Count] : -1;
            if (sets.Count > 1)
            {
                EquipCreatureWeaponSet(body, next);
            }
        }

        /// <summary>
        /// The creature's weapon sets, kept from the first look: taking its AI away also takes its <c>AI_Equipment</c>.
        /// </summary>
        private static Il2CppCollections.List<AI_Equipment.EquipData>? GetWeaponSets(Actor body)
        {
            if (SetsByBody.TryGetValue(body.Pointer, out Il2CppCollections.List<AI_Equipment.EquipData>? sets))
            {
                return sets;
            }

            AI_Equipment? equipment = body.GetComponentInChildren<AI_Equipment>(true);
            if (equipment == null || equipment._equipmentData == null)
            {
                return null;
            }

            SetsByBody[body.Pointer] = equipment._equipmentData;
            return equipment._equipmentData;
        }

        private static bool IsAttackSet(AI_Equipment.EquipData data)
        {
            int index = (int)data.Index;
            return index != (int)EquipIndex.Custom_1 && index != InsaneSet && data.Equipment != null && data.Equipment.Count > 0;
        }

        /// <summary>
        /// Puts the special set's first attack (a bear's rush, a harpy's scream) on the dodge key, which a creature has no
        /// use for otherwise. Cast abilities of a creature's AI (a ripper's leap) stay the AI's: the game's jump only works under AI steering.
        /// </summary>
        private static void BindSpecialAttack(Actor body, Il2CppCollections.List<AI_Equipment.EquipData> sets)
        {
            Actor.ActionModule actions = body.GetActionModule();
            foreach (AI_Equipment.EquipData data in sets)
            {
                if ((int)data.Index != (int)EquipIndex.Custom_1)
                {
                    continue;
                }

                foreach (AI_Equipment.SlotData slot in data.Equipment)
                {
                    ItemBhvr_EquipEffect_ActionBinding? binding = slot.Item != null ? FindActionBinding(slot.Item) : null;
                    AA_ABase? special = binding != null && binding._actions.Count > 0 ? binding._actions[0].ActionPrefab : null;
                    if (special != null && actions.GetOrCreateActionInstanceFromPrefab(special, null, slot.Item) != null)
                    {
                        actions.BindActionType(special, Actor.ActionModule.EActionBind.Dodge);
                        return;
                    }
                }
            }
        }

        private static ItemBhvr_EquipEffect_ActionBinding? FindActionBinding(Item item)
        {
            for (int i = 0; i < item._itemBehaviours.Count; i++)
            {
                ItemBhvr_EquipEffect_ActionBinding? binding = item._itemBehaviours[i].TryCast<ItemBhvr_EquipEffect_ActionBinding>();
                if (binding != null)
                {
                    return binding;
                }
            }

            return null;
        }

        /// <summary>
        /// The creature keeps its faction, and friendly hit filters drop its blows. The first attack on a friendly kind
        /// makes the creature an enemy of that kind, so the blow lands and the victim's pack fights back.
        /// Melee hits carry the attacker as <c>ProjectileEntitySource</c>. Bodies touching do not count.
        /// </summary>
        internal static void BeforeAlignmentFilter(HitFilter_Alignment filter, HitTriggerArgs args)
        {
            if (_active == null || Body == null)
            {
                return;
            }

            Entity? attacker = args.ProjectileEntitySource;
            if (attacker == null && args.ABI != null)
            {
                attacker = args.Entity;
            }

            Entity? target = filter.GetEntity();
            if (attacker == null || target == null || attacker.Pointer != Body.Pointer || target.Pointer == Body.Pointer)
            {
                return;
            }

            EntityInfo? kind = target.GetEntityInfo();
            if (kind != null && HostileKinds.Add(kind.Pointer))
            {
                Body.AlignmentModule.AddDecoratorAlignment(new EnemyAlignment(kind).Cast<IAlignment>());
            }
        }

        /// <summary>
        /// Makes the caster the registered player for one kill payout, because only its stats hold experience.
        /// </summary>
        /// <returns>The creature to put back afterwards, or null when nothing was swapped.</returns>
        internal static Actor? PayCasterInstead()
        {
            EntityGameHandler handler = EntityGameHandler.TryGet();
            if (!IsMorphed || Caster == null || handler == null || handler._playerActor == null || handler._playerActor.Pointer != Body!.Pointer)
            {
                return null;
            }

            handler._playerActor = Caster;
            return Body;
        }

        internal static void RestorePlayer(Actor body)
        {
            EntityGameHandler handler = EntityGameHandler.TryGet();
            if (handler != null)
            {
                handler._playerActor = body;
            }
        }

        private static void Clear()
        {
            HostileKinds.Clear();
            if (Body != null)
            {
                SetsByBody.Remove(Body.Pointer);
            }

            WeaponSet = -1;
            _bodyDied = false;
            _killer = null;
            _active = null;
            Caster = null;
            Body = null;
            BodyGuid = null;
        }

        /// <summary>
        /// Creatures carry no reference point and the caster's is off while hidden, so the creature gets an essential one.
        /// It is set up inactive, because the point's <c>OnEnable</c> walks <c>_defaultRoots</c>, which starts out null.
        /// </summary>
        private static void AddReferencePoint(Actor body)
        {
            OW_Root? root = OW_Root.CurrentRoot;
            if (root == null)
            {
                MelonLogger.Warning("[Morph] no streaming root, the world will not stream around the creature.");
                return;
            }

            GameObject holder = new("Morph_ReferencePoint");
            holder.SetActive(false);
            holder.transform.SetParent(body.transform, false);
            OW_ReferencePoint point = holder.AddComponent<OW_ReferencePoint>();
            point._isEssential = true;
            point._autoFindRoot = false;
            point._disableUntilMoved = false;
            point._defaultRoots = new Il2CppCollections.List<OW_Root>();
            point._defaultRoots.Add(root);
            holder.SetActive(true);
        }
    }
}
