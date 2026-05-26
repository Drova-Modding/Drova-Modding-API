using Drova_Modding_API.Systems.Spawning.Modules;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    internal sealed class ExternalDialogueModule : IExternalNpcModule
    {
        public const string ModuleKey = "dialogue";

        private DialogueModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;

        public string Key => ModuleKey;
        public string DisplayName => "Dialogue";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new DialogueModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            GUILayout.Label("Dialogue Id (stored in separate dialogue files)");
            string updatedDialogueId = GUILayout.TextField(_cachedState.DialogueId).Trim();
            if (string.Equals(updatedDialogueId, _cachedState.DialogueId, StringComparison.Ordinal))
                return _serializedPayload;

            _cachedState.DialogueId = updatedDialogueId;
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            DialogueModuleState state = Parse(payload);
            if (string.IsNullOrWhiteSpace(state.DialogueId))
                return;

            creator.WithModule(new DialoguePresetModule(state.DialogueId));
        }

        internal static string GetDialogueId(string? payload)
            => Parse(payload).DialogueId.Trim();

        private static DialogueModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new DialogueModuleState();

            try
            {
                return JsonSerializer.Deserialize<DialogueModuleState>(payload) ?? new DialogueModuleState();
            }
            catch
            {
                return new DialogueModuleState();
            }
        }

        private void EnsureUiState(string? payload)
        {
            string normalizedPayload = payload ?? string.Empty;
            if (string.Equals(_lastPayload, normalizedPayload, StringComparison.Ordinal))
                return;

            _cachedState = Parse(payload);
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = normalizedPayload;
        }

        private sealed class DialogueModuleState
        {
            public string DialogueId { get; set; } = string.Empty;
        }
    }
}


