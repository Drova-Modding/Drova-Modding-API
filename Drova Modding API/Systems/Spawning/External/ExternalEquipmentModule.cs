using System.Text.Json;

namespace Drova_Modding_API.Systems.Spawning
{
    internal sealed class ExternalEquipmentModule : IExternalNpcModule
    {
        public const string ModuleKey = "equipment";
        private readonly ReadableIdModuleSupport.EditorUiState _editorUiState = new();
        private EquipmentModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private string _rawInput = string.Empty;

        public string Key => ModuleKey;
        public string DisplayName => "Equipment";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new EquipmentModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            string? rawInput = _rawInput;

            ReadableIdModuleSupport.DrawEditor(
                "Equipment readable ids",
                ref rawInput,
                _editorUiState,
                out List<string> selectedIds,
                ModuleKey);

            if (string.Equals(rawInput, _rawInput, StringComparison.Ordinal))
                return _serializedPayload;

            _rawInput = rawInput ?? string.Empty;
            _cachedState.ItemReadableIds = selectedIds;
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            EquipmentModuleState state = Parse(payload);
            for (int i = 0; i < state.ItemReadableIds.Count; i++)
                creator.WithItem(state.ItemReadableIds[i]);
        }

        private static EquipmentModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new EquipmentModuleState();

            try
            {
                EquipmentModuleState parsed = JsonSerializer.Deserialize<EquipmentModuleState>(payload) ?? new EquipmentModuleState();
                return parsed;
            }
            catch
            {
                return new EquipmentModuleState();
            }
        }

        private void EnsureUiState(string? payload)
        {
            string normalizedPayload = payload ?? string.Empty;
            if (string.Equals(_lastPayload, normalizedPayload, StringComparison.Ordinal))
                return;

            _cachedState = Parse(payload);
            _rawInput = string.Join(Environment.NewLine, _cachedState.ItemReadableIds);
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = normalizedPayload;
        }

        private sealed class EquipmentModuleState
        {
            public List<string> ItemReadableIds { get; set; } = [];
        }
    }
}