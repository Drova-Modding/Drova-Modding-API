using Drova_Modding_API.Systems.Spawning.Modules;
using Drova_Modding_API.Systems.Talents;
using System.Text.Json;

namespace Drova_Modding_API.Systems.Spawning
{
    internal sealed class ExternalTalentModule : IExternalNpcModule
    {
        public const string ModuleKey = "talents";

        private readonly ReadableIdModuleSupport.EditorUiState _editorUiState = new();
        private TalentModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private string _rawInput = string.Empty;
        private ReadableIdModuleSupport.EditorDataSource? _editorDataSource;

        public string Key => ModuleKey;
        public string DisplayName => "Talents";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new TalentModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            EnsureEditorDataSource();
            string? rawInput = _rawInput;

            ReadableIdModuleSupport.DrawEditor(
                "Talent readable ids",
                ref rawInput,
                _editorUiState,
                out List<string> selectedIds,
                ModuleKey,
                _editorDataSource!);

            if (string.Equals(rawInput, _rawInput, StringComparison.Ordinal))
                return _serializedPayload;

            _rawInput = rawInput ?? string.Empty;
            _cachedState.TalentReadableIds = selectedIds;
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            TalentModuleState state = Parse(payload);
            if (state.TalentReadableIds.Count == 0)
                return;

            TalentPresetModule preset = new();
            for (int i = 0; i < state.TalentReadableIds.Count; i++)
                preset.With(state.TalentReadableIds[i]);

            creator.WithModule(preset);
        }

        private static TalentModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new TalentModuleState();

            try
            {
                TalentModuleState state = JsonSerializer.Deserialize<TalentModuleState>(payload) ?? new TalentModuleState();
                state.TalentReadableIds = SplitAndNormalize(string.Join(Environment.NewLine, state.TalentReadableIds));
                return state;
            }
            catch
            {
                return new TalentModuleState();
            }
        }

        private void EnsureUiState(string? payload)
        {
            string normalizedPayload = payload ?? string.Empty;
            if (string.Equals(_lastPayload, normalizedPayload, StringComparison.Ordinal))
                return;

            _cachedState = Parse(payload);
            _rawInput = string.Join(Environment.NewLine, _cachedState.TalentReadableIds);
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = normalizedPayload;
        }

        private void EnsureEditorDataSource()
        {
            if (_editorDataSource != null)
                return;

            TalentContainerDatabase.InitializeDatabase();
            Dictionary<string, List<Il2CppDrova.Talent.TalentContainer>> groupedTalents = TalentContainerDatabase.GetGroupedTalents();

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

        private static List<string> SplitAndNormalize(string? rawInput)
            => ReadableIdModuleSupport.SplitCsv(rawInput);

        private sealed class TalentModuleState
        {
            public List<string> TalentReadableIds { get; set; } = [];
        }
    }
}


