using System.Globalization;
using System.Text;
using Drova_Modding_API.Access;
using Il2CppDrova;
using Il2CppDrova.ActorActions;
using UnityEngine;

namespace Drova_Modding_API.Systems.Testing
{
    /// <summary>
    /// Combat and action commands: press what the player presses, and record what a body - this player, or a
    /// teammate's copy of theirs - actually plays, so a scenario can compare the two.
    /// </summary>
    public static partial class TestAgent
    {
        /// <summary>Binds held by a <c>press</c>, released when their time runs out.</summary>
        private static readonly List<(Actor.ActionModule.EActionBind Bind, float At)> _releases = new();

        /// <summary>Presses waiting for their start time: a burst of swings is spaced by the game's clock, not the socket's.</summary>
        private static readonly List<(Actor.ActionModule.EActionBind Bind, float Hold, float At)> _presses = new();

        /// <summary>Item uses held by a <c>useitem</c>, ended when their time runs out.</summary>
        private static readonly List<(Il2CppDrova.Items.Item Item, float At)> _itemEnds = new();

        private static Actor? _logged;
        private static readonly List<string> ActionLog = new();
        private static (IntPtr Instance, int Usage, int Combo) _loggedInstance;
        private static string? _loggedToken;
        private static bool _loggedCharged;
        private static string _loggedPhases = string.Empty;
        private static float _loggedHold;
        private static string? _loggedMode;

        /// <summary>
        /// <c>press &lt;bind&gt; [hold] [delay]</c>: starts an action bind the way the input does and lets go after
        /// <c>hold</c> seconds (default 0.05, a tap). With a <c>delay</c> the press waits that long first, so a
        /// burst of lines queues swings at exact spacing. Answers with the game's start result, or "queued".
        /// </summary>
        private static string Press(string bindName, float hold, float delay)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");
            if (!Enum.TryParse(bindName, true, out Actor.ActionModule.EActionBind bind) || bind == Actor.ActionModule.EActionBind.None)
                throw new InvalidOperationException("no bind " + bindName + "; one of " + string.Join(",", Enum.GetNames<Actor.ActionModule.EActionBind>()));

            if (delay > 0f)
            {
                _presses.Add((bind, hold, Time.unscaledTime + delay));
                return "queued";
            }

            return StartPress(player, bind, hold).ToString();
        }

        private static Actor.ActionModule.EStartResult StartPress(Actor player, Actor.ActionModule.EActionBind bind, float hold)
        {
            Actor.ActionModule.EStartResult result = player.GetActionModule().TryStartAction(bind);
            _releases.Add((bind, Time.unscaledTime + hold));
            return result;
        }

        /// <summary><c>stance &lt;mode&gt;</c>: asks for an interaction mode - Combat draws the weapon, Squat crouches.</summary>
        private static string Stance(string modeName)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");
            if (!Enum.TryParse(modeName, true, out Actor.EInteractionMode mode))
                throw new InvalidOperationException("no mode " + modeName + "; one of " + string.Join(",", Enum.GetNames<Actor.EInteractionMode>()));

            player.GetInteractionModule().RequestMode(mode);
            return "requested " + mode;
        }

        /// <summary>
        /// <c>useitem &lt;item&gt; [hold]</c>: uses a carried item the way the quick slots do - a spell, a potion,
        /// food - and ends the use after <c>hold</c> seconds. A spell must be one of this player's abilities.
        /// </summary>
        private static string UseItem(string id, float hold)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Il2CppDrova.Items.Item item = ResolveItem(id);

            // A spell is cast from an ability slot: put it on one and select it, as the quick slots do.
            Inventory_Ability? abilities = player.GetInventory().GetAbilityModule();
            if (abilities != null && abilities.CanPlaceAbility(item))
            {
                bool placed = false;
                foreach (Inventory_Ability.Entry entry in abilities.GetAbilitiesEntries())
                {
                    if (entry?.Item != null && entry.Item.Pointer == item.Pointer) { placed = true; break; }
                }

                if (!placed) abilities.PlaceAbilityOnFreeSlot(item, 1);
                abilities.SetSelectedAbility(item);
            }

            if (!item.CanBeUsedBy(player, false))
            {
                // Which of the item's conditions said no - flow, stance, a talent - or that the body is busy.
                StringBuilder why = new("refused:");
                if (item._conditions != null)
                {
                    foreach (Il2CppDrova.Items.ItemBhvr_IUseCondition condition in item._conditions)
                    {
                        if (condition != null && !condition.CanBeUsedBy(player, true)) why.Append(' ').Append(condition.Cast<Il2CppSystem.Object>().GetIl2CppType().Name);
                    }
                }

                if (player.GetActionModule().HasCurrentAction) why.Append(" busy");
                return why.ToString();
            }

            item.StartUseItem(player);
            _itemEnds.Add((item, Time.unscaledTime + hold));
            return "used " + item.ReadableId;
        }

        /// <summary><c>equip &lt;item&gt;</c>: gives the item if it is not carried and wears it in the active set, replacing what is there.</summary>
        private static string Equip(string id)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Il2CppDrova.Items.Item item = ResolveItem(id);
            if (player.GetInventory().GetAmount(item) <= 0) player.GetInventory().AddItem(new Il2CppDrova.Items.ItemStack(item, 1));

            Actor.EquipmentModule equipment = player.GetEquipmentModule();
            if (equipment.EquipOnFirstEmptySlotInActiveSet(item, true)) return "equipped " + item.ReadableId;

            // Both hands full - a sword and a shield leave no slot a dagger takes. Empty the hands and ask again.
            EmptyHands(equipment);

            return equipment.EquipOnFirstEmptySlotInActiveSet(item, true) ? "equipped " + item.ReadableId + " (hands emptied)" : "refused";
        }

        /// <summary><c>unequip</c>: takes weapons, shields and tools out of the active set - bare hands.</summary>
        private static string Unequip()
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            EmptyHands(player.GetEquipmentModule());
            return "hands empty";
        }

        private static void EmptyHands(Actor.EquipmentModule equipment)
        {
            foreach (ActorEquipSlot slot in equipment.AllItemSlotsRef)
            {
                Il2CppDrova.Items.Item? worn = slot?.GetItemOnSlot();
                string? wornId = worn?.ReadableId;
                if (worn == null || wornId == null || !(wornId.StartsWith("weapon_") || wornId.StartsWith("tool_"))) continue;
                equipment.TryUnequipInActiveSet(worn);
            }
        }

        /// <summary><c>itemsearch &lt;fragment&gt;</c>: readable ids containing the fragment, at most 60.</summary>
        private static string ItemSearch(string fragment)
        {
            StringBuilder text = new();
            int found = 0;
            foreach (Il2CppDrova.Items.Item item in Resources.FindObjectsOfTypeAll<Il2CppDrova.Items.Item>())
            {
                string? id = item?.ReadableId;
                if (id == null || id.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0) continue;

                text.Append(id).Append(';');
                if (++found >= 60) break;
            }

            return found == 0 ? "none" : text.ToString();
        }

        /// <summary>
        /// <c>actlog start &lt;me|name fragment&gt;</c> starts recording what that body plays; <c>actlog read</c>
        /// answers it as tokens joined by " > ": one per action - its bind and combo step ("PrimaryAttack.1:prefab"; no "#", which scenarios read as a comment), "!charged" when the
        /// heavy override fired, or the action's prefab name for one no button started - and "mode:X" per stance change. A teammate's body is found by
        /// name, "DrovaCoop.Remote" for the co-op mod's.
        /// </summary>
        private static string ActLog(string verb, string target)
        {
            if (verb == "read") return ActionLog.Count == 0 ? "none" : string.Join(" > ", ActionLog);
            if (verb == "brief") return ActLogBrief();
            if (verb != "start") throw new InvalidOperationException("actlog start <me|name> | actlog read | actlog brief");

            Actor? body = null;
            if (target == "me")
            {
                PlayerAccess.TryGetPlayer(out body);
            }
            else
            {
                foreach (Actor candidate in Resources.FindObjectsOfTypeAll<Actor>())
                {
                    if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.name.Contains(target)) { body = candidate; break; }
                }
            }

            _logged = body ?? throw new InvalidOperationException("no body " + target);
            ActionLog.Clear();
            _loggedInstance = default;
            _loggedToken = null;
            _loggedCharged = false;
            _loggedMode = null;
            return "logging " + body.name;
        }

        /// <summary>
        /// The log without phases and hold times, joined by "&gt;" with no spaces - what is compared across machines:
        /// hold times differ by a frame or two between a body and its copy, the sequence of actions must not.
        /// </summary>
        private static string ActLogBrief()
        {
            if (ActionLog.Count == 0) return "none";

            StringBuilder text = new();
            foreach (string entry in ActionLog)
            {
                int phases = entry.IndexOf('[');
                string brief = phases < 0 ? entry : entry.Substring(0, phases) + entry.Substring(entry.IndexOf(']') + 1);
                if (text.Length > 0) text.Append('>');
                text.Append(brief);
            }

            return text.ToString();
        }

        /// <summary>Where the player aims while set: facing, and the mouse point spells and shots are aimed at.</summary>
        private static Vector2? _aimAt;

        /// <summary>An actor by "me", guid, or the start of an active object's name (a teammate's copy: DrovaCoop.Remote).</summary>
        /// <summary>How far a by-name target is looked for: a few screens, short of a Red Tower floor (about 3000 up).</summary>
        private const float ByNameRange = 2500f;

        private static Actor ResolveActor(string target)
        {
            if (target == "me") return PlayerAccess.TryGetPlayer(out Actor? player) ? player : throw new InvalidOperationException("no player yet");

            Actor? byGuid = Entities().GetActorFromGuid(target);
            if (byGuid != null) return byGuid;

            // By name: the nearest living match - "hit Draugr" means the next one standing, not the corpse beside it
            // or one across the map. The nearest dead one only when none lives. Within ByNameRange: unbounded, a Red Tower
            // floor-2 "hit Draugr" struck one still standing on floor 1, 6000 units off, and the party ran to it.
            Vector2 origin = PlayerAccess.TryGetPlayer(out Actor? self) ? self.GetFeetPosition() : Vector2.zero;
            Actor? alive = null, dead = null;
            float aliveBest = float.MaxValue, deadBest = float.MaxValue;
            foreach (Actor candidate in Resources.FindObjectsOfTypeAll<Actor>())
            {
                // By its own name's start, or by its parent's exact name - dummies share one name ("FakeActor") and are
                // told apart only by the object they sit in (the Red Tower's tesla storages).
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
                if (!candidate.name.StartsWith(target, StringComparison.Ordinal)
                    && candidate.transform.parent?.name != target) continue;

                float distance = (candidate.GetFeetPosition() - origin).sqrMagnitude;
                if (distance > ByNameRange * ByNameRange) continue;
                bool isDead = candidate.GetHealth() != null && candidate.GetHealth().IsDead;
                if (!isDead && distance < aliveBest) { alive = candidate; aliveBest = distance; }
                if (isDead && distance < deadBest) { dead = candidate; deadBest = distance; }
            }

            return alive ?? dead ?? throw new InvalidOperationException($"no actor {target} within {ByNameRange}");
        }

        /// <summary><c>health &lt;me|guid|name&gt; [cur|max]</c>: "current/max", and "dead" when it is; with "cur" or "max" that number alone, for >= checks.</summary>
        private static string ActorHealth(string target, string part)
        {
            Health health = ResolveActor(target).GetHealth() ?? throw new InvalidOperationException("no health: " + target);
            if (part == "cur") return Format(health.CurHealth);
            if (part == "max") return Format(health.MaxHealth);
            return Format(health.CurHealth) + "/" + Format(health.MaxHealth) + (health.IsDead ? " dead" : string.Empty);
        }

        /// <summary><c>effects &lt;me|guid|name&gt;</c>: the status effects on that body, by name, or "none".</summary>
        private static string Effects(string target)
        {
            Il2CppDrova.StatusEffects.StatusEffectHandler? handler = ResolveActor(target).GetStatusEffectHandler();
            if (handler == null || handler.SourceToEffectMap == null) return "none";

            List<string> names = new();
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Il2CppDrova.StatusEffects.IStatusEffectSource, Il2CppSystem.Collections.Generic.List<Il2CppDrova.StatusEffects.StatusEffect>> pair in handler.SourceToEffectMap)
            {
                if (pair.Value == null) continue;
                foreach (Il2CppDrova.StatusEffects.StatusEffect effect in pair.Value)
                {
                    if (effect != null) names.Add(effect.name.Replace("(Clone)", string.Empty).Replace(" ", "_"));
                }
            }

            return names.Count == 0 ? "none" : string.Join(";", names);
        }

        /// <summary><c>actors [range]</c>: the living bodies near the player, nearest first, as "name@distance:health" - to find what a
        /// summon or a spawn is called before asserting on it.</summary>
        private static string Actors(float range)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            List<(float Distance, string Text)> found = new();
            foreach (Actor actor in UnityEngine.Object.FindObjectsOfType<Actor>())
            {
                if (actor == null || actor.Pointer == player.Pointer || !actor.IsInitialized) continue;

                float distance = (actor.GetFeetPosition() - player.GetFeetPosition()).magnitude;
                if (distance > range) continue;

                Health? health = actor.GetHealth();
                found.Add((distance, actor.name.Replace(" ", "_") + "@" + Format(distance) + ":" + (health == null ? "-" : Format(health.CurHealth))));
            }

            found.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            return found.Count == 0 ? "none" : string.Join(";", found.Take(40).Select(entry => entry.Text));
        }

        /// <summary>
        /// <c>effect list &lt;fragment&gt;</c> names the status effects the game knows that contain it; <c>effect add
        /// &lt;name&gt;</c> puts one on this player - an always-crit, a poison - as if something had just applied it.
        /// </summary>
        private static string Effect(string verb, string name)
        {
            if (verb == "list")
            {
                List<string> names = new();
                foreach (Il2CppDrova.StatusEffects.StatusEffect effect in Resources.FindObjectsOfTypeAll<Il2CppDrova.StatusEffects.StatusEffect>())
                {
                    if (effect != null && effect.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 && !names.Contains(effect.name)) names.Add(effect.name);
                    if (names.Count >= 40) break;
                }

                return names.Count == 0 ? "none" : string.Join(";", names);
            }

            if (verb != "add" && verb != "remove") throw new InvalidOperationException("effect list <fragment> | effect add|remove <name>");
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            foreach (Il2CppDrova.StatusEffects.StatusEffect effect in Resources.FindObjectsOfTypeAll<Il2CppDrova.StatusEffects.StatusEffect>())
            {
                if (effect == null || effect.name != name) continue;

                // The way the game's own cures take one off: every stack, through RemoveAllEffects.
                if (verb == "remove")
                {
                    Il2CppSystem.Collections.Generic.List<Il2CppDrova.StatusEffects.StatusEffect> one = new();
                    one.Add(effect);
                    player.GetStatusEffectHandler().RemoveAllEffects(one);
                    return "removed " + name;
                }

                player.GetStatusEffectHandler().AddEffectFromSource(effect);
                return "added " + name;
            }

            throw new InvalidOperationException("no status effect " + name);
        }

        /// <summary>
        /// <c>talents &lt;me|guid|name&gt; [fragment]</c>: the talents that body has learned, by asset name (count first);
        /// <c>talent learn &lt;name&gt;</c> and <c>talent list &lt;fragment&gt;</c> for this player. Skills change combos,
        /// charges and on-hit effects, and a teammate's copy has to carry its owner's.
        /// </summary>
        private static string Talents(string target, string fragment)
        {
            Actor actor = ResolveActor(target);
            if (actor.TalentActorModule is not { } module) return "no talent module";

            Il2CppDrova.Talent.TalentActorModule? talents = module.TryCast<Il2CppDrova.Talent.TalentActorModule>();
            if (talents == null || talents._learnedTalents == null) return "0";

            List<string> names = new();
            foreach (string guid in talents._learnedTalents)
            {
                string name = TalentName(guid) ?? guid;
                if (fragment.Length == 0 || name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) names.Add(name);
            }

            names.Sort(StringComparer.Ordinal);
            return names.Count + (names.Count == 0 ? string.Empty : " " + string.Join(";", names));
        }

        private static string Talent(string verb, string name)
        {
            if (verb == "list")
            {
                List<string> names = new();
                foreach (Il2CppDrova.Talent.TalentContainer container in Resources.FindObjectsOfTypeAll<Il2CppDrova.Talent.TalentContainer>())
                {
                    if (container != null && container.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 && !names.Contains(container.name)) names.Add(container.name);
                    if (names.Count >= 60) break;
                }

                return names.Count == 0 ? "none" : string.Join(";", names);
            }

            if (verb != "learn") throw new InvalidOperationException("talent list <fragment> | talent learn <name>");
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            foreach (Il2CppDrova.Talent.TalentContainer container in Resources.FindObjectsOfTypeAll<Il2CppDrova.Talent.TalentContainer>())
            {
                if (container == null || container.name != name) continue;

                player.TalentActorModule.TryCast<Il2CppDrova.Talent.TalentActorModule>()!.LearnTalentIntern(container.GUID, false);
                return "learned " + name;
            }

            throw new InvalidOperationException("no talent " + name);
        }

        private static string? TalentName(string guid)
        {
            foreach (Il2CppDrova.Talent.TalentContainer container in Resources.FindObjectsOfTypeAll<Il2CppDrova.Talent.TalentContainer>())
            {
                if (container != null && container.GUID == guid) return container.name;
            }

            return null;
        }

        /// <summary>
        /// <c>objects &lt;fragment&gt; [list]</c>: how many active objects have that in their name - a spell's effects,
        /// summons. With <c>list</c>: every loaded match, inactive ones too, as <c>parent/name=1|0</c> (active in the
        /// hierarchy) - scenery and routine containers switched by gvars.
        /// </summary>
        private static string Objects(string fragment, bool list)
        {
            int count = 0;
            List<string> found = new();
            foreach (Transform transform in UnityEngine.Object.FindObjectsOfType<Transform>(list))
            {
                if (transform == null || transform.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0) continue;

                count++;
                if (found.Count < 40) found.Add((transform.parent != null ? transform.parent.name + "/" : "") + transform.name + "=" + (transform.gameObject.activeInHierarchy ? 1 : 0));
            }

            return list ? count + " " + string.Join(";", found) : count.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <c>look &lt;x,y|me|guid|name|off&gt;</c>: faces the player that way and aims there - spells and shots aim at
        /// the mouse, so the mouse point is held there too - until <c>look off</c>.
        /// </summary>
        private static string Look(string target)
        {
            if (target == "off")
            {
                _aimAt = null;
                return "aim released";
            }

            string[] xy = target.Split(',');
            _aimAt = xy.Length == 2 && float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                ? new Vector2(x, y)
                : ResolveActor(target).GetFeetPosition();

            if (PlayerAccess.TryGetPlayer(out Actor? player)) player.GetLookModule().LookAtPosInstant(_aimAt.Value);
            return "aiming at " + Format(_aimAt.Value.x) + "," + Format(_aimAt.Value.y);
        }

        /// <summary>Holds the facing while aimed: the look input would turn the player back to the mouse every frame.</summary>
        private static bool LookInputPrefix()
        {
            if (_aimAt is not { } aim) return true;
            if (PlayerAccess.TryGetPlayer(out Actor? player)) player.GetLookModule().LookAtPosInstant(aim);
            return false;
        }

        /// <summary>Holds the mouse point on the aim after the camera handler read the real mouse.</summary>
        private static void MousePointPostfix()
        {
            if (_aimAt is { } aim) CameraGameHandler._mouseWorldPos2D = aim;
        }

        /// <summary>Set while the agent itself lets go of a button.</summary>
        private static bool _agentReleasing;

        /// <summary>
        /// Keeps a held button held. The combat input lets go of every button that is not physically down, every
        /// frame - and with the desktop's keyboard off, none is - so a `press ... hold` was a tap: a shield went up
        /// and straight back down, and a block never caught a blow. While the agent holds anything, only the
        /// agent's own release reaches the local player's actions.
        /// </summary>
        private static bool HeldReleasePrefix(AA_ABase __instance)
        {
            if (_agentReleasing || _releases.Count == 0) return true;
            if (!PlayerAccess.TryGetPlayer(out Actor? player) || __instance == null) return true;

            Actor? owner = __instance.GetActor();
            return owner == null || owner.Pointer != player.Pointer;
        }

        private static void TickActions()
        {
            float now = Time.unscaledTime;

            if ((_releases.Count > 0 || _presses.Count > 0 || _itemEnds.Count > 0) && PlayerAccess.TryGetPlayer(out Actor? player))
            {
                for (int i = 0; i < _presses.Count; i++)
                {
                    if (_presses[i].At > now) continue;
                    StartPress(player, _presses[i].Bind, _presses[i].Hold);
                    _presses.RemoveAt(i--);
                }

                for (int i = _releases.Count - 1; i >= 0; i--)
                {
                    if (_releases[i].At > now) continue;

                    _agentReleasing = true;
                    try
                    {
                        player.GetActionModule().TryReleaseAction(_releases[i].Bind);
                    }
                    finally
                    {
                        _agentReleasing = false;
                    }

                    _releases.RemoveAt(i);
                }

                for (int i = _itemEnds.Count - 1; i >= 0; i--)
                {
                    if (_itemEnds[i].At > now) continue;
                    _itemEnds[i].Item.EndUseItem(player);
                    _itemEnds.RemoveAt(i);
                }
            }

            RecordAction();
        }

        private static void RecordAction()
        {
            if (_logged == null) return;
            if (_logged.WasCollected || _logged.gameObject == null) { _logged = null; return; }

            string mode = _logged.GetInteractionModule()?.CurrentMode.ToString() ?? "-";
            if (mode != _loggedMode)
            {
                if (_loggedMode != null) ActionLog.Add("mode:" + mode);
                _loggedMode = mode;
            }

            Actor.ActionModule module = _logged.GetActionModule();
            AA_ABase? current = module != null && module.HasCurrentAction ? module.CurrentAction : null;
            // A pooled instance is reused: a second swing can be the same object started again, told apart
            // only by its use counter.
            (IntPtr, int, int) instance = current == null ? default : (current.Pointer, current.UsageCounter, current.ComboCount);

            if (instance != _loggedInstance)
            {
                FlushAction();
                _loggedInstance = instance;
                _loggedToken = current == null ? null : Describe(module!, current);
                _loggedCharged = false;
                _loggedPhases = string.Empty;
                _loggedHold = 0f;
            }

            AA_AttackAction? attack = current?.TryCast<AA_AttackAction>();
            if (attack != null)
            {
                if (attack.IsAttackOverriden) _loggedCharged = true;
                if (attack.CurrentPhase == AA_AttackAction.Phase.Hold) _loggedHold += Time.unscaledDeltaTime;

                // One letter per phase entered: a tap runs A-S-K-L-R, a hold waits in H.
                char phase = attack.CurrentPhase == AA_AttackAction.Phase.None ? '-' : attack.CurrentPhase.ToString()[0];
                if (_loggedPhases.Length == 0 || _loggedPhases[^1] != phase) _loggedPhases += phase;
            }
        }

        private static void FlushAction()
        {
            if (_loggedToken == null) return;
            // How long the swing was held, to a tenth: a tap and a charge are the same action and differ only here.
            string token = _loggedPhases.Length > 0
                ? _loggedToken + "[" + _loggedPhases + " hold=" + _loggedHold.ToString("0.0", CultureInfo.InvariantCulture) + "]"
                : _loggedToken;
            ActionLog.Add(_loggedCharged ? token + "!charged" : token);
            _loggedToken = null;
        }

        /// <summary>The bind an action came from - through its combo root, as the co-op mod matches it - and its combo step; else its item or its name.</summary>
        private static string Describe(Actor.ActionModule module, AA_ABase current)
        {
            AA_ABase root = current.RootActionInstance;
            foreach (Actor.ActionModule.EActionBind bind in Enum.GetValues<Actor.ActionModule.EActionBind>())
            {
                if (bind == Actor.ActionModule.EActionBind.None) continue;
                AA_ABase bound = module.GetBindedPrefabAction(bind);
                if (bound == null) continue;

                if (bound.Pointer == current.Prefab?.Pointer || bound.Pointer == current.Pointer
                    || (root != null && (bound.Pointer == root.Prefab?.Pointer || bound.Pointer == root.Pointer)))
                {
                    // The prefab too: a charged swing is its own action, told apart from a tap only by name.
                    return bind + "." + current.ComboCount.ToString(CultureInfo.InvariantCulture) + ":" + Name(current.Prefab ?? current);
                }
            }

            // By prefab, not by the item it came from: a teammate's copy starts some of these (a sling's load)
            // from the prefab alone, and the two must read the same.
            return Name(current.Prefab ?? current);
        }

        /// <summary>
        /// <c>popup [yes|no]</c>: the open confirm popup's text - or presses its button. Dialogues stop on some (the
        /// Red Tower's "no going back") until the player answers.
        /// </summary>
        /// <summary>
        /// The weather as the game holds it: the five levels (0-3) in the order the game numbers them, whether the
        /// updater is running and lerping, how many things lock it, and the fog effect's live percentage - what the
        /// foglet patches read, and what a fog-clearing death moves without touching the levels.
        /// </summary>
        private static string Weather()
        {
            if (!Il2CppDrova.Weather.WeatherGameHandler.TryGet(out Il2CppDrova.Weather.WeatherGameHandler handler) || handler == null) return "no weather handler";

            Il2CppDrova.Weather.WeatherStateUpdater updater = handler.StateUpdater;
            Il2Cpp.WeatherState? state = updater?.CurState;
            if (updater == null || state == null) return "no weather state";

            string[] names = ["cloud", "wind", "rain", "thunder", "fog"];
            StringBuilder text = new();
            for (int index = 0; index < names.Length && index < state.ParamCount; index++)
            {
                // One decimal: the levels are whole steps, and a transition settles a hair short of one (1.99 for 2).
                text.Append(names[index]).Append('=').Append(state.GetParamValue(index).ToString("0.#", CultureInfo.InvariantCulture)).Append(' ');
            }

            float fogPercent = handler.TryGetWeatherEffect(out Il2CppDrova.Weather.WeatherEffect_Fog fog) && fog != null ? fog.CurPercent : -1f;
            text.Append("enabled=").Append(updater._isEnabled ? 1 : 0)
                .Append(" lerping=").Append(updater.Lerper != null && updater.Lerper.IsLerping ? 1 : 0)
                .Append(" locks=").Append(updater._weatherLocks?.Count ?? 0)
                .Append(" fogpct=").Append(Format(fogPercent));
            return text.ToString();
        }

        /// <summary>
        /// <c>worldevent</c>: the running global and regional events and the registered global ones. <c>worldevent start &lt;name&gt;</c>:
        /// starts the global event named like that, ending a running one. <c>worldevent region &lt;Region,Region,...&gt;</c>: starts the
        /// first of those regions' events that survives its start (a pool's skip roll ends it at once).
        /// </summary>
        private static string WorldEvent(string[] parts)
        {
            WorldEvents.WorldEventSystemManager manager = WorldEvents.WorldEventSystemManager.Instance
                ?? throw new InvalidOperationException("no world event system yet");
            string op = parts.Length > 1 ? parts[1] : string.Empty;

            if (op == "start")
            {
                string name = Arg(parts, 2);
                WorldEvents.IWorldEvent picked = WorldEvents.WorldEventSystemManager.WorldEvents.FirstOrDefault(e => e.ToString()!.Contains(name, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("no global event named like " + name);
                if (manager.CurrentEvent != null) manager.EndEvent();
                manager.StartEvent(picked);

                // StartEvent declines quietly (player dead or teleporting); a scenario must not read that as a start.
                return manager.CurrentEvent == picked ? "started " + picked : throw new InvalidOperationException(picked + " did not start");
            }

            if (op == "region")
            {
                // The first of the named regions whose event survives its own start: a pool's skip roll ends it at once.
                foreach (string name in Arg(parts, 2).Split(','))
                {
                    if (!Enum.TryParse(name, out Region region) || !WorldEvents.RegionalEventRegistry.TryGetEvents(region, out List<WorldEvents.Regional.ARegionalEvent>? events)) continue;
                    foreach (WorldEvents.Regional.ARegionalEvent regional in events)
                    {
                        // Started again it would spawn a second time over a coroutine nothing can stop.
                        if (regional.IsRunning) continue;
                        regional.StartEvent();
                        if (regional.IsRunning) return "started " + region;
                    }
                }

                return "none started";
            }

            List<Region> here = new();
            RegionAccess.GetCurrentRegions(here);
            return "here=" + string.Join(",", here) + " current=" + (manager.CurrentEvent?.ToString() ?? "none") + " global=" + string.Join(",", WorldEvents.WorldEventSystemManager.WorldEvents)
                + " regional=" + string.Join(",", manager.RegionalEvents.Where(e => e.IsRunning).Select(e => e.Region));
        }

        /// <summary>
        /// Foglet fog patches within range of the player: mode, defeated, whether the player is inside, and how many of its
        /// foglets are spawned, active and alive.
        /// </summary>
        private static string Foglets(float range)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) return "no player";

            Vector3 at = player.transform.position;
            StringBuilder text = new();
            int found = 0;

            foreach (Il2CppDrova.Weather.Fog.FogletFogBhvr patch in UnityEngine.Object.FindObjectsOfType<Il2CppDrova.Weather.Fog.FogletFogBhvr>())
            {
                if (patch == null || Vector2.Distance(patch.transform.position, at) > range) continue;

                int spawned = 0, active = 0, alive = 0;
                foreach (Actor foglet in patch._fogletInstances)
                {
                    if (foglet == null) continue;
                    spawned++;
                    if (foglet.IsActiveInGame) active++;
                    if (!foglet.IsDead()) alive++;
                }

                found++;
                text.Append(patch.name).Append(" mode=").Append(patch._mode).Append(" defeated=").Append(patch._defeated ? 1 : 0)
                    .Append(" inside=").Append(patch._isPlayerInside ? 1 : 0).Append(" spawned=").Append(spawned)
                    .Append(" active=").Append(active).Append(" alive=").Append(alive)
                    .Append(" dist=").Append(Format(Vector2.Distance(patch.transform.position, at))).Append("; ");
            }

            return found + " patch(es) " + text;
        }

        /// <summary>
        /// The merchant window, driven as a player would: <c>merchant</c> describes it, <c>merchant take|give &lt;item&gt; [n]</c>
        /// puts n of an item from the trader's side (take) or the player's side (give) on the table, <c>merchant accept</c>
        /// presses Trade, <c>merchant close</c> closes the window.
        /// </summary>
        private static string Merchant(string verb, string readableId, int amount)
        {
            // Found, not asked for: the handler's getter builds the window when there is none, which is not a question.
            Il2CppDrova.GUI.GUI_Window_Merchant? window = UnityEngine.Object.FindObjectOfType<Il2CppDrova.GUI.GUI_Window_Merchant>();
            if (window == null || !window.IsWindowOpen || window._marketPlace == null) return "open=0";

            Il2CppTradingSystem.MarketPlace market = window._marketPlace;
            switch (verb)
            {
                case "":
                    return "open=1 trader=" + DescribeTrader(market.GetTraderLeft()) + " player=" + DescribeTrader(market.GetTraderRight())
                        + " table=" + Format(market.GetGrabTableBoardValueLeft()) + "/" + Format(market.GetGrabTableBoardValueRight());

                case "take":
                case "give":
                    Il2CppTradingSystem.Services.ITrader side = verb == "take" ? market.GetTraderLeft() : market.GetTraderRight();
                    string wanted = ResolveItem(readableId).ReadableId;
                    foreach (Il2CppTradingSystem.ItemContainers.IItemContainer container in side.GetAllItems())
                    {
                        if (container?.GetItem()?.ReadableId != wanted) continue;

                        Il2CppTradingSystem.ItemContainers.ItemTraderStack stack = new(container, amount);
                        Il2CppTradingSystem.ResultState result = verb == "take" ? market.AddItemToGrabTableFromLeft(stack) : market.AddItemToGrabTableFromRight(stack);
                        window.RefreshGUI();
                        return verb + " " + readableId + ": " + result.ResultType;
                    }

                    return "no " + readableId + " on that side";

                case "accept":
                    window.Accept();
                    return "accepted";

                case "close":
                    window.CloseWindow();
                    return "closed";
            }

            return "unknown merchant verb " + verb;

            static string DescribeTrader(Il2CppTradingSystem.Services.ITrader trader)
            {
                StringBuilder text = new();
                foreach (Il2CppTradingSystem.ItemContainers.IItemContainer container in trader.GetAllItems())
                {
                    string? id = container?.GetItem()?.ReadableId;
                    if (id != null) text.Append(id).Append(':').Append(container!.GetAmount()).Append(';');
                }

                return text.Length == 0 ? "none" : text.ToString();
            }
        }

        /// <summary>Opens the pause menu, as the pause key does.</summary>
        private static string Opened(GUIGameHandler gui)
        {
            gui.ShowPauseMenu();
            return "pause menu open";
        }

        /// <summary>Presses an active UI button by its GameObject name - a menu button a mod added, say.</summary>
        private static string Click(string name)
        {
            GameObject found = GameObject.Find(name);
            UnityEngine.UI.Button? button = found != null ? found.GetComponent<UnityEngine.UI.Button>() : null;
            if (button == null) return "no active button " + name;
            if (!button.interactable) return "not interactable " + name;

            button.onClick.Invoke();
            return "clicked " + name;
        }

        private static string Popup(string answer)
        {
            foreach (Il2CppDrova.GUI.GUI_Window_ConfirmPopUp popup in UnityEngine.Object.FindObjectsOfType<Il2CppDrova.GUI.GUI_Window_ConfirmPopUp>())
            {
                if (popup == null || !popup.gameObject.activeInHierarchy) continue;

                string text = popup._descriptionText != null ? popup._descriptionText.text : "-";
                if (answer.Length == 0) return text.Replace(' ', '_');

                UnityEngine.UI.Button? button = answer == "no" ? popup._noButton : popup._yesButton;
                if (button == null) throw new InvalidOperationException("this popup has no " + answer + " button");

                button.onClick.Invoke();
                return "pressed " + answer;
            }

            if (answer.Length == 0) return "none";
            throw new InvalidOperationException("no popup open");
        }

        private static string Name(AA_ABase action) => action.name.Replace(" ", "_").Replace("(Clone)", string.Empty);
    }
}
