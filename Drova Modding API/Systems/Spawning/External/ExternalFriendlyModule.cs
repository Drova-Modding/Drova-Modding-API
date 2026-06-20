using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    internal sealed class ExternalFriendlyModule : IExternalNpcModule
    {
        public const string ModuleKey = "friendly";
        private FriendlyModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;

        public string Key => ModuleKey;
        public string DisplayName => "Alignment";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new FriendlyModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            bool updatedIsPlayerFriendly = GUILayout.Toggle(_cachedState.IsPlayerFriendly, "Friendly to player");
            if (updatedIsPlayerFriendly == _cachedState.IsPlayerFriendly)
                return _serializedPayload;

            _cachedState.IsPlayerFriendly = updatedIsPlayerFriendly;
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            FriendlyModuleState state = Parse(payload);
            creator.IsPlayerFriendly(state.IsPlayerFriendly);
        }

        private static FriendlyModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new FriendlyModuleState();

            try
            {
                return JsonSerializer.Deserialize<FriendlyModuleState>(payload) ?? new FriendlyModuleState();
            }
            catch
            {
                return new FriendlyModuleState();
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

        private sealed class FriendlyModuleState
        {
            public bool IsPlayerFriendly { get; set; } = true;
        }
    }
}

