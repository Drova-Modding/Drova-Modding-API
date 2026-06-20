using Drova_Modding_API.Systems.Spawning.Modules;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    internal sealed class ExternalHealthModule : IExternalNpcModule
    {
        public const string ModuleKey = "health";

        private HealthModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private string _maxHealthInput = "100";

        public string Key => ModuleKey;
        public string DisplayName => "Health";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new HealthModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            GUILayout.Label("Max Health");
            int updatedMaxHealth = Mathf.Max(1, (int)GUILayout.HorizontalSlider(_cachedState.MaxHealth, 1f, 5000f));
            string previousMaxHealthInput = _maxHealthInput;
            string updatedMaxHealthInput = GUILayout.TextField(_maxHealthInput);
            bool textChanged = !string.Equals(updatedMaxHealthInput, previousMaxHealthInput, StringComparison.Ordinal);
            if (textChanged)
            {
                if (int.TryParse(updatedMaxHealthInput, out int parsedMaxHealth))
                    updatedMaxHealth = Mathf.Max(1, parsedMaxHealth);
                else
                    updatedMaxHealthInput = previousMaxHealthInput;
                _maxHealthInput = updatedMaxHealthInput;
            }
            else
            {
                _maxHealthInput = updatedMaxHealth.ToString();
            }

            if (updatedMaxHealth == _cachedState.MaxHealth)
                return _serializedPayload;

            _cachedState.MaxHealth = updatedMaxHealth;
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            HealthModuleState state = Parse(payload);
            creator.WithModule(new HealthPresetModule().With(Math.Max(1, state.MaxHealth)));
        }

        private static HealthModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new HealthModuleState();

            try
            {
                return JsonSerializer.Deserialize<HealthModuleState>(payload) ?? new HealthModuleState();
            }
            catch
            {
                return new HealthModuleState();
            }
        }

        private void EnsureUiState(string? payload)
        {
            string normalizedPayload = payload ?? string.Empty;
            if (string.Equals(_lastPayload, normalizedPayload, StringComparison.Ordinal))
                return;

            _cachedState = Parse(payload);
            _cachedState.MaxHealth = Math.Max(1, _cachedState.MaxHealth);
            _maxHealthInput = _cachedState.MaxHealth.ToString();
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = normalizedPayload;
        }

        private sealed class HealthModuleState
        {
            public int MaxHealth { get; set; } = 100;
        }
    }
}



