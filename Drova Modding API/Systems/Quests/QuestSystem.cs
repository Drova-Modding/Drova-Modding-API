using System.Text.Json;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.GlobalVars;
using Drova_Modding_API.Systems.Items;
using Drova_Modding_API.Systems.Spawning;
using Il2Cpp;
using Il2CppDrova;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.JournalSystem;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.Saveables;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// Custom quests from <c>Mods/Modding_API/Quests/*.json</c>, built from the game's own quest pieces:
    /// a quest GVarList holds the state, <see cref="JournalTopic"/> and <see cref="JournalEntry"/> objects hold the texts.
    /// Dialogues drive a quest with the vanilla quest state, gvar and <c>TriggerJournalTask</c> nodes.
    /// </summary>
    public static class QuestSystem
    {
        private const string FolderName = "Quests";
        private const string ResourcePathPrefix = "Modding_API/Quests/";
        private const string RunningCategoryPath = "categories/RunningQuests";
        private const string FinishedCategoryPath = "categories/FinishedQuests";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly Dictionary<string, QuestDefinition> QuestsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, AJournalTopic> TopicsByQuestId = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, JournalTopic> OwnTopicsByQuestId = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, JournalEntry> EntriesByPath = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<(QuestDefinition Quest, QuestEntryDefinition Entry)>>? _onGBoolByName;
        private static readonly Dictionary<string, JournalTopic> CreatedTopics = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, JournalEntry> CreatedEntries = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<(QuestDefinition Quest, QuestKillDefinition Kill)>> KillsByActorGuid = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> DiedThisSession = new(StringComparer.OrdinalIgnoreCase);
        private static bool _groupsResolved;

        /// <summary>
        /// Absolute path of the folder quest files are read from.
        /// </summary>
        public static string GetQuestFolderPath() => Path.Combine(Utils.SavePath, FolderName);

        /// <summary>
        /// All loaded quest definitions by id.
        /// </summary>
        public static IReadOnlyDictionary<string, QuestDefinition> Quests => QuestsById;

        /// <summary>
        /// Name of the <see cref="JournalEntry"/> of a quest entry. A dialogue's <c>TriggerJournalTask</c> finds the entry by it.
        /// </summary>
        public static string GetEntryName(string questId, string entryId) => ResourcePathPrefix + questId + "/" + entryId;

        /// <summary>
        /// The <see cref="JournalEntry"/> of a quest entry, or null when unknown.
        /// </summary>
        public static JournalEntry? GetJournalEntry(string questId, string entryId)
            => EntriesByPath.TryGetValue(GetEntryName(questId, entryId), out JournalEntry? entry) ? entry : null;

        /// <summary>
        /// Adds a journal entry of a quest now, for mods that drive a quest from code.
        /// Returns false when the entry or the journal is missing, or the entry is already in the journal.
        /// </summary>
        public static bool TriggerEntry(string questId, string entryId) => Trigger(GetJournalEntry(questId, entryId));

        /// <summary>
        /// A custom quest's journal entry by its resource path (<see cref="JournalEntry.GetResourcesPath"/>).
        /// These entries are not in <c>Resources</c>, so code that resolves journal paths, like a co-op journal sync, asks here.
        /// </summary>
        public static JournalEntry? FindJournalEntry(string resourcePath)
            => EntriesByPath.TryGetValue(resourcePath, out JournalEntry? entry) ? entry : null;

        /// <summary>
        /// A custom quest's own journal topic by its resource path. Null for vanilla topics, which <c>Resources</c> holds.
        /// </summary>
        public static AJournalTopic? FindJournalTopic(string resourcePath)
            => CreatedTopics.TryGetValue(resourcePath, out JournalTopic? topic) ? topic : CustomItemSystem.FindLetterTopic(resourcePath);

        internal static void Initialize(HarmonyLib.Harmony harmony)
        {
            Hooking.TryPostfix(harmony, typeof(SaveData_JournalTopic), nameof(SaveData_JournalTopic.GetEntriesNonAlloc),
                typeof(QuestSystem), nameof(GetEntriesNonAllocPostfix));
            Hooking.TryPostfix(harmony, typeof(SaveData_JournalTopic), nameof(SaveData_JournalTopic.GetTopic),
                typeof(QuestSystem), nameof(GetTopicPostfix));
            GameEvents.OnQuestStateChanged += OnQuestStateChanged;
            GameEvents.OnEntityDied += OnEntityDied;
            GvarBusSystem.OnGBoolValueChanged += OnGBoolChanged;
            PlayerAccess.OnPlayerFound += _ => CheckKillsOnLoad();
            GVarConditions.Initialize();
            QuestAreas.Initialize();
            ExternalCreaturePlacementSystem.Initialize();
            ExternalItemPlacementSystem.Initialize();
            ExternalNpcPlacementSystem.Initialize();
        }

        /// <summary>
        /// Reads every quest file and creates its journal objects.
        /// Runs at the main menu before saved dialogues are patched, so their journal references resolve.
        /// </summary>
        internal static void LoadDefinitions()
        {
            QuestsById.Clear();
            TopicsByQuestId.Clear();
            OwnTopicsByQuestId.Clear();
            EntriesByPath.Clear();
            KillsByActorGuid.Clear();
            _groupsResolved = false;
            _onGBoolByName = null;

            string folder = GetQuestFolderPath();
            Directory.CreateDirectory(folder);
            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    QuestConfigFile? config = JsonSerializer.Deserialize<QuestConfigFile>(File.ReadAllText(file), JsonOptions);
                    foreach (QuestDefinition quest in config?.Quests ?? [])
                    {
                        if (string.IsNullOrWhiteSpace(quest.Id) || string.IsNullOrWhiteSpace(quest.GVarList))
                        {
                            MelonLogger.Warning("[QuestSystem] Skipping a quest without Id or GVarList in " + Path.GetFileName(file));
                            continue;
                        }

                        QuestsById[quest.Id] = quest;
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[QuestSystem] Could not read " + file + ": " + e.Message);
                }
            }

            JournalCategory? running = LoadResource<JournalCategory>(RunningCategoryPath);
            foreach (QuestDefinition quest in QuestsById.Values)
            {
                BuildJournal(quest, running);
                foreach (QuestKillDefinition kill in quest.Kills)
                {
                    foreach (string actor in kill.Actors)
                    {
                        string guid = ResolveActorGuid(actor);
                        if (!KillsByActorGuid.TryGetValue(guid, out List<(QuestDefinition, QuestKillDefinition)>? kills))
                        {
                            kills = [];
                            KillsByActorGuid[guid] = kills;
                        }

                        kills.Add((quest, kill));
                    }
                }
            }

            MelonLogger.Msg($"[QuestSystem] Loaded {QuestsById.Count} quest(s) from '{folder}'.");
        }

        /// <summary>
        /// A placed creature id becomes its actor guid, anything shaped like a guid is taken as a vanilla actor guid.
        /// </summary>
        private static string ResolveActorGuid(string actor)
            => Guid.TryParse(actor, out Guid guid) ? guid.ToString() : ExternalCreaturePlacementSystem.GetActorGuid(actor);

        private static void BuildJournal(QuestDefinition quest, JournalCategory? running)
        {
            AJournalTopic? topic;
            if (quest.Topic.Vanilla != null)
            {
                topic = LoadResource<AJournalTopic>(quest.Topic.Vanilla);
            }
            else
            {
                // Created once per path and updated in place: dialogues patched at an earlier main menu still point at them.
                string path = ResourcePathPrefix + quest.Id;
                if (!CreatedTopics.TryGetValue(path, out JournalTopic? ownTopic) || ownTopic == null)
                {
                    ownTopic = JournalTopic.CreateTopicInstance(path, running, null, quest.Topic.LocaPath, quest.Topic.LocaKey);
                    ownTopic._resourcePathReference._resourcePath = path;
                    ownTopic.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    CreatedTopics[path] = ownTopic;
                }

                ownTopic._relatedDefaultCategory = running;
                ownTopic._locaTopicName = new LocalizedString(quest.Topic.LocaPath, quest.Topic.LocaKey);
                OwnTopicsByQuestId[quest.Id] = ownTopic;
                topic = ownTopic;
            }

            if (topic == null)
            {
                MelonLogger.Warning($"[QuestSystem] Quest '{quest.Id}': no journal topic '{quest.Topic.Vanilla}'.");
                return;
            }

            TopicsByQuestId[quest.Id] = topic;
            foreach (QuestEntryDefinition entry in quest.Entries)
            {
                string path = GetEntryName(quest.Id, entry.Id);
                if (!CreatedEntries.TryGetValue(path, out JournalEntry? journalEntry) || journalEntry == null)
                {
                    journalEntry = JournalEntry.CreateEntryInstance(path, null, quest.Topic.LocaPath, entry.LocaKey);
                    journalEntry._resourcePathReference._resourcePath = path;
                    journalEntry.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    CreatedEntries[path] = journalEntry;
                }

                journalEntry._relatedTopic = topic;
                journalEntry._locaEntryText = new LocalizedString(quest.Topic.LocaPath, entry.LocaKey);
                QuestExp? exp = null;
                if (entry.Exp > 0)
                {
                    exp = journalEntry._exp != null ? journalEntry._exp : ScriptableObject.CreateInstance<QuestExp>();
                    exp._exp = entry.Exp;
                    exp.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }

                JournalEntry.SetQuestExp(journalEntry, exp);

                EntriesByPath[path] = journalEntry;
            }
        }

        /// <summary>
        /// Journal groups are not resources and only exist once a game runs, so own topics get theirs on first use.
        /// </summary>
        private static void ResolveGroups()
        {
            if (_groupsResolved || OwnTopicsByQuestId.Count == 0)
            {
                return;
            }

            Dictionary<string, JournalTopicGroup> groups = new(StringComparer.OrdinalIgnoreCase);
            foreach (JournalTopicGroup group in Resources.FindObjectsOfTypeAll<JournalTopicGroup>())
            {
                groups.TryAdd(group.name, group);
            }

            if (groups.Count == 0)
            {
                return;
            }

            _groupsResolved = true;
            foreach (KeyValuePair<string, JournalTopic> pair in OwnTopicsByQuestId)
            {
                string groupName = QuestsById[pair.Key].Topic.Group;
                if (groups.TryGetValue(groupName, out JournalTopicGroup? group) || groups.TryGetValue("TopicGroup_Unsorted", out group))
                {
                    pair.Value._topicGroup = group;
                }
                else
                {
                    MelonLogger.Warning($"[QuestSystem] Quest '{pair.Key}': unknown journal group '{groupName}'.");
                }
            }
        }

        private static bool Trigger(JournalEntry? journalEntry)
        {
            if (journalEntry == null || !PlayerMetaDataGameHandler.TryGet(out PlayerMetaDataGameHandler handler) || handler == null)
            {
                return false;
            }

            ResolveGroups();
            Journal_RuntimeTopic? topic = handler.GetJournal().GetRuntimeTopic(journalEntry.GetRelatedTopic());
            if (topic != null && topic.ContainsEntry(journalEntry))
            {
                return false;
            }

            journalEntry.Trigger(null);
            return true;
        }

        private static void OnQuestStateChanged(string listName, QuestState state)
        {
            if (AGVarBase.IsGVarLoading)
            {
                return;
            }

            string stateName = state.ToString();
            foreach (QuestDefinition quest in QuestsById.Values)
            {
                if (!string.Equals(quest.GVarList, listName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (QuestEntryDefinition entry in quest.Entries)
                {
                    if (string.Equals(entry.OnState, stateName, StringComparison.OrdinalIgnoreCase))
                    {
                        TriggerEntry(quest.Id, entry.Id);
                    }
                }

                if (quest.Topic.Vanilla == null && (state == QuestState.IsCompleted || state == QuestState.HasFailed))
                {
                    MoveToCategory(quest, FinishedCategoryPath);
                }
            }
        }

        private static void OnGBoolChanged(GvarBusSystem.GvarChangeEvent e)
        {
            if (!e.Value || AGVarBase.IsGVarLoading)
            {
                return;
            }

            _onGBoolByName ??= IndexOnGBoolEntries();
            if (!_onGBoolByName.TryGetValue(e.Name, out List<(QuestDefinition Quest, QuestEntryDefinition Entry)>? entries))
            {
                return;
            }

            foreach ((QuestDefinition quest, QuestEntryDefinition entry) in entries)
            {
                if (IsKnown(quest))
                {
                    TriggerEntry(quest.Id, entry.Id);
                }
            }
        }

        /// <summary>
        /// The entries written on a bool, by the bool's name, so a write does not search every quest.
        /// </summary>
        private static Dictionary<string, List<(QuestDefinition Quest, QuestEntryDefinition Entry)>> IndexOnGBoolEntries()
        {
            Dictionary<string, List<(QuestDefinition Quest, QuestEntryDefinition Entry)>> index = new(StringComparer.Ordinal);
            foreach (QuestDefinition quest in QuestsById.Values)
            {
                foreach (QuestEntryDefinition entry in quest.Entries)
                {
                    AGVarBase? gvar = entry.OnGBool == null ? null : GVarConditions.Find(quest.GVarList, entry.OnGBool);
                    if (gvar == null)
                    {
                        continue;
                    }

                    if (!index.TryGetValue(gvar.name, out List<(QuestDefinition Quest, QuestEntryDefinition Entry)>? entries))
                    {
                        entries = [];
                        index[gvar.name] = entries;
                    }

                    entries.Add((quest, entry));
                }
            }

            return index;
        }

        private static void MoveToCategory(QuestDefinition quest, string categoryPath)
        {
            JournalCategory? category = LoadResource<JournalCategory>(categoryPath);
            if (category == null || !TopicsByQuestId.TryGetValue(quest.Id, out AJournalTopic? topic)
                || !PlayerMetaDataGameHandler.TryGet(out PlayerMetaDataGameHandler handler) || handler == null)
            {
                return;
            }

            Journal journal = handler.GetJournal();
            if (journal.GetRuntimeTopic(topic) != null)
            {
                journal.ChangeTopicCategory(topic, category);
            }
        }

        private static void OnEntityDied(Entity entity)
        {
            try
            {
                Actor? actor = entity.TryCast<Actor>();
                GuidComponent? guid = actor != null ? actor.GetGuidComponent() : null;
                if (guid == null || !KillsByActorGuid.TryGetValue(guid.GuidString, out List<(QuestDefinition Quest, QuestKillDefinition Kill)>? kills))
                {
                    return;
                }

                DiedThisSession.Add(guid.GuidString);
                foreach ((QuestDefinition quest, QuestKillDefinition kill) in kills)
                {
                    CheckKill(quest, kill);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[QuestSystem] Death handling failed: " + e);
            }
        }

        /// <summary>
        /// Sets the bool of every kill whose actors already died in this save, so a target killed before the quest still counts.
        /// </summary>
        private static void CheckKillsOnLoad()
        {
            DiedThisSession.Clear();
            foreach (QuestDefinition quest in QuestsById.Values)
            {
                foreach (QuestKillDefinition kill in quest.Kills)
                {
                    CheckKill(quest, kill);
                }
            }
        }

        private static void CheckKill(QuestDefinition quest, QuestKillDefinition kill)
        {
            if (kill.Actors.Count == 0)
            {
                return;
            }

            foreach (string actor in kill.Actors)
            {
                string guid = ResolveActorGuid(actor);
                if (!DiedThisSession.Contains(guid) && SavegameDataExtensions.IsActorAlive(guid))
                {
                    return;
                }
            }

            GVarConditions.TrySetBool(quest.GVarList, kill.SetGBool, true);
        }

        /// <summary>
        /// The journal reloads entries with <c>Resources.Load</c> and drops what it cannot find.
        /// Quest entries are not resources, so they are resolved here by the path the save stored.
        /// </summary>
        private static void GetEntriesNonAllocPostfix(SaveData_JournalTopic __instance, ref Il2CppCollections.List<JournalEntry> entries)
        {
            try
            {
                Il2CppCollections.List<string>? paths = __instance._entryResourcesPath;
                if (paths == null)
                {
                    return;
                }

                ResolveGroups();
                for (int i = 0; i < paths.Count; i++)
                {
                    if (paths[i] != null && EntriesByPath.TryGetValue(paths[i], out JournalEntry? entry))
                    {
                        entries.Add(entry);
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[QuestSystem] Journal restore failed: " + e);
            }
        }

        /// <summary>
        /// Bool entries wait until the quest is known, so a target killed before the quest adds no journal line.
        /// </summary>
        private static bool IsKnown(QuestDefinition quest)
        {
            GVarList? list = ProviderAccess.GVarDatabase?.GetGVarListByName(quest.GVarList);
            return list != null && QuestStateReader.TryRead(list.GetQuestState(), out QuestState state) && state != QuestState.None;
        }

        /// <summary>
        /// The journal reloads topics with <c>Resources.Load</c>, which knows neither custom quest topics nor custom letters.
        /// </summary>
        private static void GetTopicPostfix(SaveData_JournalTopic __instance, ref AJournalTopic? __result)
        {
            if (__result == null && !string.IsNullOrEmpty(__instance._topicResourcePath))
            {
                __result = FindJournalTopic(__instance._topicResourcePath);
            }
        }

        private static T? LoadResource<T>(string path) where T : Object
        {
            Object? asset = Resources.Load(path, Il2CppType.Of<T>());
            return asset != null ? asset.TryCast<T>() : null;
        }
    }
}
