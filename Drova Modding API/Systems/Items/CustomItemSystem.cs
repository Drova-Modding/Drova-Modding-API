using System.Text.Json;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Morphing;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;
using Il2CppDrova.ABI;
using Il2CppDrova.ActorActions;
using Il2CppCustomFramework.Localization;
using Il2CppDrova.Items;
using Il2CppDrova.JournalSystem;
using Il2CppDrova.StatusEffects;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using BindingFlags = Il2CppSystem.Reflection.BindingFlags;
using FieldInfo = Il2CppSystem.Reflection.FieldInfo;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Items
{
    /// <summary>
    /// Adds custom items to the game's item database. Each is a copy of a vanilla item, so it brings that item's look,
    /// use and effects, and the game saves it by its guid like any vanilla item. Reloading updates the items in place,
    /// so it also works while a world is loaded and inventories hold the items.
    /// </summary>
    public static class CustomItemSystem
    {
        private const string FolderName = "Items";
        private const string KnownItemsFile = "known_items.txt";

        private const string LetterPathPrefix = "Modding_API/Letters/";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly Dictionary<string, Item> ItemsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, (string Guid, string Base)> KnownItems = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<Object>> OwnedById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Object> Retired = [];
        private static readonly Dictionary<string, JournalLetterTopic> LetterTopicsById = new(StringComparer.OrdinalIgnoreCase);
        private static GameObject? _prefabHolder;

        /// <summary>
        /// Absolute path of the folder item files are read from.
        /// </summary>
        public static string GetFolderPath() => Path.Combine(Utils.SavePath, FolderName);

        /// <summary>
        /// The custom items registered now, by readable id.
        /// </summary>
        public static IReadOnlyDictionary<string, Item> Items => ItemsById;

        /// <summary>
        /// The save guid of a custom item: the definition's own, or one derived from its readable id.
        /// </summary>
        public static string GetGuid(CustomItemDefinition definition)
            => System.Guid.TryParse(definition.Guid, out Guid fixedGuid)
                ? fixedGuid.ToString()
                : LazyActorCreator.StableGuid("Modding_API.Item", definition.ReadableId);

        /// <summary>
        /// Registers a custom item, or updates it in place when it is registered already.
        /// Must run before a save loads, or the save's stacks of a new item come back empty.
        /// </summary>
        /// <returns>The item, or null when the definition is invalid.</returns>
        public static Item? Register(CustomItemDefinition definition)
        {
            SubDatabase_Item? database = ProviderAccess.ItemDatabase;
            if (database == null)
            {
                MelonLogger.Warning("[Items] The item database is not loaded yet.");
                return null;
            }

            Item? baseItem = string.IsNullOrWhiteSpace(definition.Base) ? null : database.GetItemByReadableId(definition.Base);
            if (string.IsNullOrWhiteSpace(definition.ReadableId) || baseItem == null || ItemsById.ContainsKey(definition.Base))
            {
                MelonLogger.Warning($"[Items] '{definition.ReadableId}': a readable id and a vanilla base item are required (base '{definition.Base}').");
                return null;
            }

            string guid = GetGuid(definition);
            if (ItemsById.TryGetValue(definition.ReadableId, out Item? existing))
            {
                if (existing._copyLocaItem != baseItem)
                {
                    MelonLogger.Warning($"[Items] '{definition.ReadableId}': a changed base item takes a restart.");
                    return existing;
                }

                Apply(existing, baseItem, definition);
                return existing;
            }

            Item? taken = database.GetItemByReadableId(definition.ReadableId) ?? database.GetItemByGuid(guid);
            if (taken != null && !KnownItems.ContainsKey(definition.ReadableId))
            {
                MelonLogger.Warning($"[Items] '{definition.ReadableId}': the readable id or guid is already taken by '{taken.ReadableId}'.");
                return null;
            }

            // The game's own copy: the item and its behavior assets, re-pointed at the copy.
            Item item = ItemExtensions.InstantiateItem(baseItem);
            item.name = definition.ReadableId;
            item._readableId = definition.ReadableId;
            item._guid = guid;
            item.hideFlags = HideFlags.DontUnloadUnusedAsset;
            for (int i = 0; i < item._itemBehaviours.Count; i++)
            {
                item._itemBehaviours[i].hideFlags = HideFlags.DontUnloadUnusedAsset;
            }

            Apply(item, baseItem, definition);

            // The database's add method is an empty stub, so the entry goes into its list and both lookups directly.
            SubDatabase_Item.Entry entry = new() { ReadableId = definition.ReadableId, Guid = guid, Item = item };
            database._entries.Add(entry);
            database._readableIdToData[definition.ReadableId] = entry;
            database._guidToData[guid] = entry;

            ItemsById[definition.ReadableId] = item;
            KnownItems[definition.ReadableId] = (guid, definition.Base);
            return item;
        }

        /// <summary>
        /// Reads every item file: new items are registered, known ones updated in place, and removed ones point at their
        /// base item, so saves holding them keep a vanilla item instead of an empty stack. Runs at every main menu, before
        /// saved dialogues resolve their item references, and from the Modding Tools window.
        /// </summary>
        public static void Reload()
        {
            // At the main menu nothing uses a replaced copy any more, while in a world a running effect may still hold one.
            if (!PlayerAccess.TryGetPlayer(out _))
            {
                foreach (Object retired in Retired)
                {
                    if (retired != null)
                    {
                        Object.Destroy(retired);
                    }
                }

                Retired.Clear();
            }

            string folder = GetFolderPath();
            Directory.CreateDirectory(folder);
            ReadKnownItems(folder);

            HashSet<string> defined = new(StringComparer.OrdinalIgnoreCase);
            List<(Item Item, CustomItemDefinition Definition)> teaching = [];
            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    CustomItemConfigFile? config = JsonSerializer.Deserialize<CustomItemConfigFile>(File.ReadAllText(file), JsonOptions);
                    foreach (CustomItemDefinition definition in config?.Items ?? [])
                    {
                        Item? item = Register(definition);
                        if (item == null)
                        {
                            continue;
                        }

                        defined.Add(definition.ReadableId);
                        if (definition.LearnSpell != null)
                        {
                            teaching.Add((item, definition));
                        }
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[Items] Could not read " + file + ": " + e.Message);
                }
            }

            // After all files, since a recipe may name a spell defined further down.
            foreach ((Item item, CustomItemDefinition definition) in teaching)
            {
                SetLearnedSpell(item, definition.LearnSpell!);
            }

            foreach (KeyValuePair<string, (string Guid, string Base)> known in KnownItems.Where(known => !defined.Contains(known.Key)).ToList())
            {
                PointAtBase(known.Key, known.Value.Guid, known.Value.Base);
            }

            WriteKnownItems(folder);
            MelonLogger.Msg($"[Items] {ItemsById.Count} custom item(s) from '{folder}'.");
        }

        private static void Apply(Item item, Item baseItem, CustomItemDefinition definition)
        {
            Retire(item.ReadableId);
            // Name and description come from the mod's file. Rarity and type labels keep reading the base item's file.
            item._locaPath = definition.LocaPath;
            item._copyLocaFromItem = true;
            item._copyLocaItem = baseItem;
            item._copyName = false;
            item._copyDescription = false;
            item._buyValue = definition.BuyValue ?? baseItem.BuyValue;
            item._sellValue = definition.SellValue ?? baseItem.SellValue;
            item._maxStackSize = definition.MaxStackSize ?? baseItem._maxStackSize;
            item.SetIconSprite(baseItem.Icon);
            if (!string.IsNullOrWhiteSpace(definition.Icon))
            {
                SetIcon(item, baseItem, definition.Icon);
            }

            RestoreBaseActions(item, baseItem);
            if (definition.Letter != null)
            {
                SetLetter(item, baseItem, definition, definition.Letter);
            }

            bool changesAction = definition.EffectStacks != null || definition.Ability is { Count: > 0 } || definition.Morph != null;
            AA_ABase? action = changesAction ? OwnActionPrefab(item) : null;
            if (definition.EffectStacks is int stacks)
            {
                SetEffectStacks(item, action, stacks);
            }

            if (definition.Ability is { Count: > 0 })
            {
                SetAbilityValues(item, action, definition.Ability);
            }

            if (definition.Morph != null)
            {
                SetMorph(item, action, definition.Morph);
            }
        }

        /// <summary>
        /// Points the item's action and instant ability back at the base item's, so a reload starts from the base again.
        /// </summary>
        private static void RestoreBaseActions(Item item, Item baseItem)
        {
            for (int i = 0; i < item._itemBehaviours.Count && i < baseItem._itemBehaviours.Count; i++)
            {
                ItemBhvr_ActorAction_FromPrefab? action = item._itemBehaviours[i].TryCast<ItemBhvr_ActorAction_FromPrefab>();
                ItemBhvr_ActorAction_FromPrefab? baseAction = baseItem._itemBehaviours[i].TryCast<ItemBhvr_ActorAction_FromPrefab>();
                if (action != null && baseAction != null)
                {
                    action._actionPrefab = baseAction._actionPrefab;
                }

                ItemBhvr_Instant_Ability? instant = item._itemBehaviours[i].TryCast<ItemBhvr_Instant_Ability>();
                ItemBhvr_Instant_Ability? baseInstant = baseItem._itemBehaviours[i].TryCast<ItemBhvr_Instant_Ability>();
                if (instant != null && baseInstant != null)
                {
                    instant._effect = baseInstant._effect;
                }
            }
        }

        private static void SetLearnedSpell(Item item, string spellId)
        {
            Item? spell = ProviderAccess.ItemDatabase?.GetItemByReadableId(spellId);
            ItemBhvr_Instant_LearnSpell? learn = null;
            for (int i = 0; i < item._itemBehaviours.Count && learn == null; i++)
            {
                learn = item._itemBehaviours[i].TryCast<ItemBhvr_Instant_LearnSpell>();
            }

            if (spell == null || learn == null)
            {
                MelonLogger.Warning($"[Items] '{item.ReadableId}': LearnSpell needs a spell recipe as Base and a known spell, not '{spellId}'.");
                return;
            }

            learn._spellToLearn = spell;
        }

        /// <summary>
        /// The spell's first ability becomes the morph. Its other abilities are dropped, so one cast spawns one creature.
        /// </summary>
        private static void SetMorph(Item item, AA_ABase? action, CustomMorphDefinition morph)
        {
            AA_Trigger_Ability[] triggers = action != null ? action.GetComponentsInChildren<AA_Trigger_Ability>(true) : [];
            AA_Trigger_Ability? first = triggers.FirstOrDefault(trigger => trigger._chargeLevels is { Count: > 0 } && trigger._chargeLevels[0] != null);
            ABI_Ability? ability = first != null ? MorphManager.CreateAbility(first._chargeLevels[0], morph.Creature, morph.Duration, PrefabHolder) : null;
            if (first == null || ability == null)
            {
                MelonLogger.Warning($"[Items] '{item.ReadableId}': Morph needs a spell that spawns an ability as Base and a creature, not '{morph.Creature}'.");
                return;
            }

            Own(item, ability);
            Own(item, ability.AbilityPrefab.gameObject);
            foreach (AA_Trigger_Ability trigger in triggers)
            {
                trigger._chargeLevels = new Il2CppCollections.List<ABI_Ability>();
            }

            first.SetAbility(ability);
            first._placedOnPreDefinedPositions = false;
        }

        /// <summary>
        /// A custom letter's journal topic by its resource path, for the journal reloading a save. Null for anything else.
        /// </summary>
        public static AJournalTopic? FindLetterTopic(string resourcePath)
        {
            return resourcePath.StartsWith(LetterPathPrefix, StringComparison.Ordinal)
                && LetterTopicsById.TryGetValue(resourcePath[LetterPathPrefix.Length..], out JournalLetterTopic? topic) ? topic : null;
        }

        /// <summary>
        /// The base letter's paper with the mod's text, under its own journal topic. The topic is made once per item and
        /// updated in place, since journals and dialogues of a running game hold it.
        /// </summary>
        private static void SetLetter(Item item, Item baseItem, CustomItemDefinition definition, CustomLetterDefinition letter)
        {
            ItemBhvr_Letter? own = null;
            ItemBhvr_Letter? vanilla = null;
            for (int i = 0; i < item._itemBehaviours.Count && i < baseItem._itemBehaviours.Count; i++)
            {
                own ??= item._itemBehaviours[i].TryCast<ItemBhvr_Letter>();
                vanilla ??= baseItem._itemBehaviours[i].TryCast<ItemBhvr_Letter>();
            }

            if (own == null || vanilla == null || vanilla._topic == null || vanilla._topic._letterContentPrefab == null)
            {
                MelonLogger.Warning($"[Items] '{item.ReadableId}': Letter needs a vanilla letter as Base.");
                return;
            }

            GameObject content = Object.Instantiate(vanilla._topic._letterContentPrefab, PrefabHolder);
            content.name = vanilla._topic._letterContentPrefab.name + "_" + item.ReadableId;
            Own(item, content);
            LocalizedTextMeshPro? text = content.GetComponentInChildren<LocalizedTextMeshPro>(true);
            if (text != null)
            {
                text._localizedString = new LocalizedString(definition.LocaPath, letter.TextKey);
            }

            LocalizedString title = new(definition.LocaPath, letter.TitleKey ?? item.ReadableId + "_name");
            if (!LetterTopicsById.TryGetValue(item.ReadableId, out JournalLetterTopic? topic) || topic == null)
            {
                topic = JournalLetterTopic.CreateLetterTopicInstance(new JournalLetterTopic.CreateArgs
                {
                    ScriptableName = "LetterTopic_" + item.ReadableId,
                    LocaPath = definition.LocaPath,
                    LocaKey = title.Key,
                    ContentPrefab = content,
                    Group = vanilla._topic._topicGroup,
                    Category = vanilla._topic._relatedDefaultCategory
                });
                topic._resourcePathReference._resourcePath = LetterPathPrefix + item.ReadableId;
                topic.hideFlags = HideFlags.DontUnloadUnusedAsset;
                LetterTopicsById[item.ReadableId] = topic;
            }

            topic._letterContentPrefab = content;
            topic._locaTopicName = title;
            own._topic = topic;
        }

        /// <summary>
        /// An inactive parent that outlives worlds, for prefab copies the game instantiates later.
        /// </summary>
        private static Transform PrefabHolder
        {
            get
            {
                if (_prefabHolder == null)
                {
                    _prefabHolder = new GameObject("Modding_API_ItemActionPrefabs");
                    _prefabHolder.SetActive(false);
                    Object.DontDestroyOnLoad(_prefabHolder);
                }

                return _prefabHolder.transform;
            }
        }

        /// <summary>
        /// The action prefab is shared with the base item, so the item gets its own copy before anything on it changes.
        /// The copy waits under an inactive holder until the game instantiates it on use.
        /// </summary>
        private static AA_ABase? OwnActionPrefab(Item item)
        {
            for (int i = 0; i < item._itemBehaviours.Count; i++)
            {
                ItemBhvr_ActorAction_FromPrefab? action = item._itemBehaviours[i].TryCast<ItemBhvr_ActorAction_FromPrefab>();
                if (action == null || action._actionPrefab == null)
                {
                    continue;
                }

                AA_ABase copy = Object.Instantiate(action._actionPrefab, PrefabHolder);
                copy.name = action._actionPrefab.name + "_" + item.ReadableId;
                action._actionPrefab = copy;
                Own(item, copy.gameObject);
                return copy;
            }

            return null;
        }

        /// <summary>
        /// The game's own stack override on the status effect trigger.
        /// </summary>
        private static void SetEffectStacks(Item item, AA_ABase? action, int stacks)
        {
            AA_Trigger_StatusEffect[] triggers = action != null ? action.GetComponentsInChildren<AA_Trigger_StatusEffect>(true) : [];
            if (triggers.Length == 0)
            {
                MelonLogger.Warning($"[Items] '{item.ReadableId}': the base item applies no status effect, EffectStacks is ignored.");
                return;
            }

            foreach (AA_Trigger_StatusEffect trigger in triggers)
            {
                trigger._hasCustomStacks = true;
                trigger._stacksToAdd = stacks;
            }
        }

        /// <summary>
        /// Gives the item its own copies of every ability it uses and writes the values into them. A status effect copy gets
        /// its own guid in the effect database, so an effect still running when the game saves comes back as the item's.
        /// </summary>
        private static void SetAbilityValues(Item item, AA_ABase? action, Dictionary<string, double> values)
        {
            HashSet<string> applied = new(StringComparer.Ordinal);

            ABI_Ability CopyAbility(ABI_Ability ability) => CopyWith(ability, values, string.Empty);

            // A key "Nested/Value" belongs to an ability the copy's controller spawns, such as a projectile's hit. The copy
            // gets its own controller prefab, whose reference then points at a copy of that ability with the value.
            ABI_Ability CopyWith(ABI_Ability ability, Dictionary<string, double> own, string prefix)
            {
                ABI_Ability copy = Object.Instantiate(ability);
                copy.name = ability.name + "_" + item.ReadableId;
                copy.hideFlags = HideFlags.DontUnloadUnusedAsset;
                Own(item, copy);
                Dictionary<string, Dictionary<string, double>> nested = new(StringComparer.Ordinal);
                foreach (KeyValuePair<string, double> value in own)
                {
                    int slash = value.Key.IndexOf('/');
                    if (slash > 0)
                    {
                        string name = value.Key[..slash];
                        if (!nested.TryGetValue(name, out Dictionary<string, double>? values))
                        {
                            values = new Dictionary<string, double>(StringComparer.Ordinal);
                            nested[name] = values;
                        }

                        values[value.Key[(slash + 1)..]] = value.Value;
                    }
                    else if (TrySetAbilityValue(copy, value.Key, value.Value))
                    {
                        applied.Add(prefix + value.Key);
                    }
                }

                if (nested.Count > 0 && copy.AbilityPrefab != null)
                {
                    ABI_Controller controller = Object.Instantiate(copy.AbilityPrefab, PrefabHolder);
                    controller.name = copy.AbilityPrefab.name + "_" + item.ReadableId;
                    Own(item, controller.gameObject);
                    copy.SetAbilityPrefab(controller);
                    ReplaceAbilities(controller.gameObject, spawned => nested.TryGetValue(spawned.name, out Dictionary<string, double>? values)
                        ? CopyWith(spawned, values, prefix + spawned.name + "/")
                        : null);
                }

                return copy;
            }

            AA_Trigger_StatusEffect[] effectTriggers = action != null ? action.GetComponentsInChildren<AA_Trigger_StatusEffect>(true) : [];
            for (int t = 0; t < effectTriggers.Length; t++)
            {
                AA_Trigger_StatusEffect trigger = effectTriggers[t];
                if (trigger._statusEffect == null)
                {
                    continue;
                }

                StatusEffect effect = Object.Instantiate(trigger._statusEffect);
                effect.name = trigger._statusEffect.name + "_" + item.ReadableId;
                effect.hideFlags = HideFlags.DontUnloadUnusedAsset;
                Own(item, effect);
                for (int i = 0; i < effect._effectsBhvrs.Count; i++)
                {
                    StatusEffectBhvr_SpawnAbility? spawn = effect._effectsBhvrs[i].TryCast<StatusEffectBhvr_SpawnAbility>();
                    for (int a = 0; spawn != null && spawn._abilities != null && a < spawn._abilities.Count; a++)
                    {
                        spawn._abilities[a] = CopyAbility(spawn._abilities[a]);
                    }
                }

                RegisterEffect(effect, LazyActorCreator.StableGuid("Modding_API.ItemEffect", item.ReadableId + "/" + t));
                trigger._statusEffect = effect;
            }

            AA_Trigger_Ability[] abilityTriggers = action != null ? action.GetComponentsInChildren<AA_Trigger_Ability>(true) : [];
            foreach (AA_Trigger_Ability trigger in abilityTriggers)
            {
                for (int i = 0; trigger._chargeLevels != null && i < trigger._chargeLevels.Count; i++)
                {
                    trigger._chargeLevels[i] = CopyAbility(trigger._chargeLevels[i]);
                }
            }

            for (int i = 0; i < item._itemBehaviours.Count; i++)
            {
                ItemBhvr_Instant_Ability? instant = item._itemBehaviours[i].TryCast<ItemBhvr_Instant_Ability>();
                if (instant != null && instant._effect != null)
                {
                    instant._effect = CopyAbility(instant._effect);
                }
            }

            foreach (string key in values.Keys.Where(key => !applied.Contains(key)))
            {
                MelonLogger.Warning($"[Items] '{item.ReadableId}': no number or bool ability value '{key}' on the base item.");
            }
        }

        /// <summary>
        /// A number, int or bool value by its key, or one damage entry as <c>Dmg_Properties[0]</c>.
        /// </summary>
        private static bool TrySetAbilityValue(ABI_Ability ability, string key, double value)
        {
            int open = key.IndexOf('[');
            if (open > 0 && key.EndsWith(']') && int.TryParse(key[(open + 1)..^1], out int index))
            {
                return AbilityDataAccess.TryGet(ability, key[..open], out ABIBaseData? list) && AbilityDataAccess.TrySetDamage(list, index, value);
            }

            return AbilityDataAccess.TryGet(ability, key, out ABIBaseData? data) && AbilityDataAccess.TrySet(data, value);
        }

        /// <summary>
        /// Offers every ability a component under <paramref name="root"/> references, single or in a list, to
        /// <paramref name="replace"/>, and stores what it returns in its place.
        /// </summary>
        private static void ReplaceAbilities(GameObject root, Func<ABI_Ability, ABI_Ability?> replace)
        {
            const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                for (Il2CppSystem.Type? type = component.GetIl2CppType(); type != null && type.Name != nameof(MonoBehaviour); type = type.BaseType)
                {
                    foreach (FieldInfo field in type.GetFields(Fields))
                    {
                        Il2CppSystem.Object? value = field.GetValue(component);
                        ABI_Ability? single = value?.TryCast<ABI_Ability>();
                        if (single != null && replace(single) is { } replacement)
                        {
                            field.SetValue(component, replacement);
                        }

                        Il2CppCollections.List<ABI_Ability>? list = value?.TryCast<Il2CppCollections.List<ABI_Ability>>();
                        for (int i = 0; list != null && i < list.Count; i++)
                        {
                            if (list[i] != null && replace(list[i]) is { } replaced)
                            {
                                list[i] = replaced;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves store running status effects by guid, so the item's copy is entered in the effect database under its own.
        /// Like the item database, it has no add method, so the entry goes into its list and lookup directly.
        /// </summary>
        private static void RegisterEffect(StatusEffect effect, string guid)
        {
            SubDatabase_StatusEffect database = GameDatabase.Instance.StatusEffectDatabase;
            effect._guid = guid;
            if (database._guidToData.TryGetValue(guid, out SubDatabase_StatusEffect.Entry existing))
            {
                existing.RelatedStatusEffect = effect;
                return;
            }

            SubDatabase_StatusEffect.Entry entry = new() { Guid = guid, RelatedStatusEffect = effect };
            database._entries.Add(entry);
            database._guidToData[guid] = entry;
        }

        /// <summary>
        /// A removed item keeps resolving, to its base item, for saves that still hold it.
        /// </summary>
        private static void PointAtBase(string readableId, string guid, string baseId)
        {
            SubDatabase_Item? database = ProviderAccess.ItemDatabase;
            Item? baseItem = database?.GetItemByReadableId(baseId);
            if (database == null || baseItem == null || !database._readableIdToData.TryGetValue(baseId, out SubDatabase_Item.Entry baseEntry))
            {
                return;
            }

            database._readableIdToData[readableId] = baseEntry;
            database._guidToData[guid] = baseEntry;
            ItemsById.Remove(readableId);
        }

        /// <summary>
        /// Loads a PNG as the item's icon, at the base icon's scale so it sits in slots and on pickups like the vanilla ones.
        /// </summary>
        private static void SetIcon(Item item, Item baseItem, string fileName)
        {
            string path = Path.Combine(GetFolderPath(), fileName);
            if (!File.Exists(path))
            {
                MelonLogger.Warning($"[Items] '{item.ReadableId}': no icon file '{path}'.");
                return;
            }

            Texture2D texture = new(2, 2) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
            {
                MelonLogger.Warning($"[Items] '{item.ReadableId}': '{path}' is not a readable PNG.");
                return;
            }

            float pixelsPerUnit = baseItem.Icon != null ? baseItem.Icon.pixelsPerUnit : 100f;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = item.ReadableId;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            item.SetIconSprite(sprite);

            // Spells show their icon on the flow bar from this behavior, not from the item.
            for (int i = 0; i < item._itemBehaviours.Count; i++)
            {
                ItemBhvr_FlowAbilityData? flow = item._itemBehaviours[i].TryCast<ItemBhvr_FlowAbilityData>();
                if (flow != null)
                {
                    flow._icon = sprite;
                }
            }

            Own(item, texture);
            Own(item, sprite);
        }

        /// <summary>
        /// Copies made for an item, retired when it is applied again or removed.
        /// </summary>
        private static void Own(Item item, Object copy)
        {
            if (!OwnedById.TryGetValue(item.ReadableId, out List<Object>? owned))
            {
                owned = [];
                OwnedById[item.ReadableId] = owned;
            }

            owned.Add(copy);
        }

        private static void Retire(string readableId)
        {
            if (OwnedById.Remove(readableId, out List<Object>? owned))
            {
                Retired.AddRange(owned);
            }
        }

        /// <summary>
        /// Every custom item ever registered here, one <c>readableId;guid;base</c> per line, so a removed one still resolves.
        /// </summary>
        private static void ReadKnownItems(string folder)
        {
            string path = Path.Combine(folder, KnownItemsFile);
            if (!File.Exists(path))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                string[] parts = line.Split(';');
                if (parts.Length == 3 && !KnownItems.ContainsKey(parts[0]))
                {
                    KnownItems[parts[0]] = (parts[1], parts[2]);
                }
            }
        }

        private static void WriteKnownItems(string folder)
        {
            try
            {
                File.WriteAllLines(Path.Combine(folder, KnownItemsFile),
                    KnownItems.Select(known => known.Key + ";" + known.Value.Guid + ";" + known.Value.Base));
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Items] Could not write the known item list: " + e.Message);
            }
        }
    }
}
