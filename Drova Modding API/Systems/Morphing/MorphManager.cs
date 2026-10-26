using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Drova_Modding_API.Access;
using Il2CppDrova;
using Il2CppDrova.ABI;
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
        private static readonly Dictionary<string, Actor> CreaturesByGuid = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<IntPtr, (float Duration, ABI_Bhvr_Morph_Actor Morph, string Guid)> MorphsByAbility = new();
        private static readonly HashSet<string> Loading = new(StringComparer.OrdinalIgnoreCase);

        // Guids that named no actor prefab, so a caller asking every frame does not start a load every frame.
        private static readonly HashSet<string> Failed = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<IntPtr> HostileKinds = new();
        private static ABI_Bhvr_Morph_Actor? _active;
        private static bool _bodyDied;
        private static Entity? _killer;

        /// <summary>
        /// A morph started, with the hidden caster and the creature the player controls.
        /// </summary>
        public static event Action<Actor, Actor>? OnMorphStarted;

        /// <summary>
        /// A morph ended and the caster has the controls again. Not raised when a load ends it.
        /// When the creature died, the caster dies right after.
        /// </summary>
        public static event Action<Actor>? OnMorphEnded;

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

            return entry.Morph != null && entry.Morph._targetActor != null && CanMorph?.Invoke() != false;
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

            MelonCoroutines.Start(Follow(morph, caster, body, duration));
            OnMorphStarted?.Invoke(caster, body);
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
            if (died)
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
            if (body != null)
            {
                EquipCreatureWeapon(body);
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
        /// Equips a creature's first weapon set, which binds its attacks to the attack keys. Its AI equips it only from
        /// the combat graph, and with the AI off <c>AI_Equipment</c> never starts. For any creature whose AI is off.
        /// </summary>
        public static void EquipCreatureWeapon(Actor body)
        {
            AI_Equipment? equipment = body.GetComponentInChildren<AI_Equipment>(true);
            if (equipment == null || equipment._equipmentData == null || equipment._equipmentData.Count == 0)
            {
                MelonLogger.Warning($"[Morph] '{body.name}' has no weapon to equip, it cannot attack.");
                return;
            }

            IInventoryContainer inventory = body.GetInventoryActorModule().GetDefaultInventory();
            foreach (AI_Equipment.SlotData slot in equipment._equipmentData[0].Equipment)
            {
                Il2CppCollections.List<ActorEquipSlot>? slots = slot.Item != null ? body.GetEquipmentModule().GetEquipmentSlotsOfType(slot.SlotType) : null;
                if (slots == null || slots.Count == 0)
                {
                    continue;
                }

                if (!inventory.HasAtLeastItemInInventory(slot.Item, 1))
                {
                    inventory.AddItem(new ItemStack(slot.Item, 1), false, false);
                }

                slots[0].TryEquip(slot.Item);
            }
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

        private static void Clear()
        {
            HostileKinds.Clear();
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
