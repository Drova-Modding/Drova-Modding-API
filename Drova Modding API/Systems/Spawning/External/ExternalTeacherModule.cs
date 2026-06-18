using Drova_Modding_API.Systems.Spawning.Modules;
using Drova_Modding_API.Systems.Talents;
using Il2CppDrova.GUI.LearnGUI;
using Il2CppDrova.Items;
using Il2CppDrova.Talent;
using MelonLoader;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// External NPC module that lets a spawned NPC teach attributes and talents,
    /// backed by <see cref="TeacherModule"/>.
    /// </summary>
    internal sealed class ExternalTeacherModule : IExternalNpcModule
    {
        public const string ModuleKey = "teacher";

        private readonly ReadableIdModuleSupport.EditorUiState _editorUiState = new();
        private TeacherModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private string _rawInput = string.Empty;
        private string _maxAttributeLevelInput = "130";
        private ReadableIdModuleSupport.EditorDataSource? _editorDataSource;

        public string Key => ModuleKey;
        public string DisplayName => "Teacher";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new TeacherModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            bool changed = false;

            // ── Max attribute level ──────────────────────────────────────────
            GUILayout.Label("Max teachable attribute level");
            int updatedMaxLevel = Mathf.Max(1, (int)GUILayout.HorizontalSlider(_cachedState.MaxAttributeLevel, 1f, 200f));
            string previousMaxLevelInput = _maxAttributeLevelInput;
            string updatedMaxLevelInput = GUILayout.TextField(_maxAttributeLevelInput);
            if (!string.Equals(updatedMaxLevelInput, previousMaxLevelInput, StringComparison.Ordinal))
            {
                if (int.TryParse(updatedMaxLevelInput, out int parsedMaxLevel))
                    updatedMaxLevel = Mathf.Max(1, parsedMaxLevel);
                else
                    updatedMaxLevelInput = previousMaxLevelInput;
                _maxAttributeLevelInput = updatedMaxLevelInput;
            }
            else
            {
                _maxAttributeLevelInput = updatedMaxLevel.ToString();
            }

            if (updatedMaxLevel != _cachedState.MaxAttributeLevel)
            {
                _cachedState.MaxAttributeLevel = updatedMaxLevel;
                changed = true;
            }

            // ── Teachable attributes ─────────────────────────────────────────
            GUILayout.Space(6f);
            GUILayout.Label("Teachable attributes");

            bool updatedStrength = GUILayout.Toggle(_cachedState.TeachStrength, "Strength");
            if (updatedStrength != _cachedState.TeachStrength)
            {
                _cachedState.TeachStrength = updatedStrength;
                changed = true;
            }

            bool updatedDex = GUILayout.Toggle(_cachedState.TeachDex, "Dexterity");
            if (updatedDex != _cachedState.TeachDex)
            {
                _cachedState.TeachDex = updatedDex;
                changed = true;
            }

            bool updatedMind = GUILayout.Toggle(_cachedState.TeachMind, "Mind");
            if (updatedMind != _cachedState.TeachMind)
            {
                _cachedState.TeachMind = updatedMind;
                changed = true;
            }

            // ── Teachable talents ────────────────────────────────────────────
            GUILayout.Space(6f);
            bool updatedTeachExplicit = GUILayout.Toggle(_cachedState.TeachTalentsExplicitly, "Teach talents explicitly");
            if (updatedTeachExplicit != _cachedState.TeachTalentsExplicitly)
            {
                _cachedState.TeachTalentsExplicitly = updatedTeachExplicit;
                changed = true;
            }

            EnsureEditorDataSource();
            string? rawInput = _rawInput;
            ReadableIdModuleSupport.DrawEditor(
                "Teachable talent readable ids",
                ref rawInput,
                _editorUiState,
                out List<string> selectedIds,
                ModuleKey,
                _editorDataSource!);

            if (!string.Equals(rawInput, _rawInput, StringComparison.Ordinal))
            {
                _rawInput = rawInput ?? string.Empty;
                _cachedState.TalentReadableIds = selectedIds;
                changed = true;
            }

            return changed ? SerializeCachedState() : _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            TeacherModuleState state = Parse(payload);

            List<TeacherConfig.Stat> stats = [];
            if (state.TeachStrength) stats.Add(TeacherConfig.Stat.Strength);
            if (state.TeachDex) stats.Add(TeacherConfig.Stat.Dex);
            if (state.TeachMind) stats.Add(TeacherConfig.Stat.Mind);

            List<TeachableTalent> talents = [];
            if (state.TalentReadableIds.Count > 0)
            {
                TalentContainerDatabase.InitializeDatabase();
                for (int i = 0; i < state.TalentReadableIds.Count; i++)
                {
                    string readableId = state.TalentReadableIds[i];
                    if (!TalentContainerDatabase.TryGetTalent(readableId, out TalentContainer? talent) || talent == null)
                    {
                        MelonLogger.Warning($"{nameof(ExternalTeacherModule)}: could not resolve talent readable id '{readableId}'.");
                        continue;
                    }

                    talents.Add(new TeachableTalent
                    {
                        _talent = talent,
                        _teachExplicit = state.TeachTalentsExplicitly
                    });
                }
            }

            if (stats.Count == 0 && talents.Count == 0)
                return;

            TeacherModule module = new();
            module.With(Math.Max(1, state.MaxAttributeLevel));
            if (stats.Count > 0)
                module.With([.. stats]);
            if (talents.Count > 0)
                module.With([.. talents]);

            creator.WithModule(module);
        }

        private static TeacherModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new TeacherModuleState();

            try
            {
                TeacherModuleState state = JsonSerializer.Deserialize<TeacherModuleState>(payload) ?? new TeacherModuleState();
                state.TalentReadableIds = ReadableIdModuleSupport.SplitCsv(string.Join(Environment.NewLine, state.TalentReadableIds));
                return state;
            }
            catch
            {
                return new TeacherModuleState();
            }
        }

        private void EnsureUiState(string? payload)
        {
            string normalizedPayload = payload ?? string.Empty;
            if (string.Equals(_lastPayload, normalizedPayload, StringComparison.Ordinal))
                return;

            _cachedState = Parse(payload);
            _cachedState.MaxAttributeLevel = Math.Max(1, _cachedState.MaxAttributeLevel);
            _maxAttributeLevelInput = _cachedState.MaxAttributeLevel.ToString();
            _rawInput = string.Join(Environment.NewLine, _cachedState.TalentReadableIds);
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = normalizedPayload;
        }

        private string SerializeCachedState()
        {
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        private void EnsureEditorDataSource()
        {
            if (_editorDataSource != null)
                return;

            TalentContainerDatabase.InitializeDatabase();
            Dictionary<string, List<TalentContainer>> groupedTalents = TalentContainerDatabase.GetGroupedTalents();

            List<string> talentIds = groupedTalents
                .SelectMany(entry => entry.Value)
                .Select(talent => talent.name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _editorDataSource = new ReadableIdModuleSupport.EditorDataSource(
                talentIds,
                categorySelector: GetTalentCategory);
        }

        private static string GetTalentCategory(string talentId)
        {
            if (talentId.StartsWith("TalentBhvr_", StringComparison.OrdinalIgnoreCase))
                return "TalentBhvr";
            if (talentId.StartsWith("Talent_", StringComparison.OrdinalIgnoreCase))
                return "Talent";
            if (talentId.StartsWith("Test_", StringComparison.OrdinalIgnoreCase))
                return "Test";
            return "Other";
        }

        private sealed class TeacherModuleState
        {
            public int MaxAttributeLevel { get; set; } = 130;
            public bool TeachStrength { get; set; }
            public bool TeachDex { get; set; }
            public bool TeachMind { get; set; }
            public bool TeachTalentsExplicitly { get; set; } = true;
            public List<string> TalentReadableIds { get; set; } = [];
        }
    }
}