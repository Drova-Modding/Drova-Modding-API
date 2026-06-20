using System.Text.Json;

namespace Drova_Modding_API.Systems.Spawning
{
    internal sealed class ExternalCosmeticsModule : IExternalNpcModule
    {
        public const string ModuleKey = "cosmetics";
        private readonly ReadableIdModuleSupport.EditorUiState _editorUiState = new();
        private CosmeticsModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private string _rawInput = string.Empty;

        public string Key => ModuleKey;
        public string DisplayName => "Cosmetics";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new CosmeticsModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            string? rawInput = _rawInput;

            ReadableIdModuleSupport.DrawEditor(
                "Cosmetic readable ids",
                ref rawInput,
                _editorUiState,
                out List<string> selectedIds,
                ModuleKey);

            if (string.Equals(rawInput, _rawInput, StringComparison.Ordinal))
                return _serializedPayload;

            _rawInput = rawInput ?? string.Empty;
            _cachedState.CosmeticReadableIds = selectedIds;
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            CosmeticsModuleState state = Parse(payload);
            for (int i = 0; i < state.CosmeticReadableIds.Count; i++)
                creator.WithCosmetic(state.CosmeticReadableIds[i]);
        }

        private static CosmeticsModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new CosmeticsModuleState();

            try
            {
                CosmeticsModuleState parsed = JsonSerializer.Deserialize<CosmeticsModuleState>(payload) ?? new CosmeticsModuleState();
                return parsed;
            }
            catch
            {
                return new CosmeticsModuleState();
            }
        }

        private void EnsureUiState(string? payload)
        {
            string normalizedPayload = payload ?? string.Empty;
            if (string.Equals(_lastPayload, normalizedPayload, StringComparison.Ordinal))
                return;

            _cachedState = Parse(payload);
            _rawInput = string.Join(Environment.NewLine, _cachedState.CosmeticReadableIds);
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = normalizedPayload;
        }

        private sealed class CosmeticsModuleState
        {
            public List<string> CosmeticReadableIds { get; set; } = [];
        }
    }
}