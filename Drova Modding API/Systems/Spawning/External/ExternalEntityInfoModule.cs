using System.Text.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Drova_Modding_API.Systems.Spawning
{
    internal sealed class ExternalEntityInfoModule : IExternalNpcModule
    {
        public const string ModuleKey = "entityInfo";
        private EntityInfoModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;

        public string Key => ModuleKey;
        public string DisplayName => "Entity Info";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new EntityInfoModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            GUILayout.Label("EntityInfo Asset GUID (optional)");
            string updatedAssetGuid = GUILayout.TextField(_cachedState.AssetGuid);
            if (string.Equals(updatedAssetGuid, _cachedState.AssetGuid, StringComparison.Ordinal))
                return _serializedPayload;

            _cachedState.AssetGuid = updatedAssetGuid;
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            EntityInfoModuleState state = Parse(payload);
            if (!string.IsNullOrWhiteSpace(state.AssetGuid))
                creator.WithLazyEntityInfo(new AssetReference(state.AssetGuid));
        }

        private static EntityInfoModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new EntityInfoModuleState();

            try
            {
                return JsonSerializer.Deserialize<EntityInfoModuleState>(payload) ?? new EntityInfoModuleState();
            }
            catch
            {
                return new EntityInfoModuleState();
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

        private sealed class EntityInfoModuleState
        {
            public string AssetGuid { get; set; } = string.Empty;
        }
    }
}

