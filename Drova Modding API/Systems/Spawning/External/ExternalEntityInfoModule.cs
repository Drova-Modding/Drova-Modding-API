using Il2Cpp;
using Il2CppDrova;
using System.Text.Json;
using UnityEngine;
using Drova_Modding_API.Systems.Editor;

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

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new EntityInfoModuleState(Guid.NewGuid().ToString()));

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            GUILayout.Label("EntityInfo GUID (read-only)");
            GUILayout.Label(_cachedState.Guid);

            bool updatedUseLocalizedName = GUILayout.Toggle(_cachedState.UseLocalizedName, "Use localized display name");
            if (updatedUseLocalizedName != _cachedState.UseLocalizedName)
            {
                _cachedState.UseLocalizedName = updatedUseLocalizedName;
                return SerializeCachedState();
            }

            if (_cachedState.UseLocalizedName)
            {
                if (LocalizationEntriesEditorSystem.TryConsumeSelection(ModuleKey, out string selectedLocaPath, out string selectedLocaKey))
                {
                    _cachedState.LocaPath = selectedLocaPath;
                    _cachedState.LocaKey = selectedLocaKey;
                    return SerializeCachedState();
                }

                GUILayout.Label("Localized name path");
                string updatedLocaPath = GUILayout.TextField(_cachedState.LocaPath);

                GUILayout.Label("Localized name key");
                string updatedLocaKey = GUILayout.TextField(_cachedState.LocaKey);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Localization Editor", GUILayout.Width(200f)))
                    LocalizationEntriesEditorSystem.Show();

                if (GUILayout.Button("Pick Entry", GUILayout.Width(120f)))
                    LocalizationEntriesEditorSystem.RequestSelection(ModuleKey);
                GUILayout.EndHorizontal();

                if (string.Equals(updatedLocaPath, _cachedState.LocaPath, StringComparison.Ordinal)
                    && string.Equals(updatedLocaKey, _cachedState.LocaKey, StringComparison.Ordinal))
                {
                    return _serializedPayload;
                }

                _cachedState.LocaPath = updatedLocaPath;
                _cachedState.LocaKey = updatedLocaKey;
                return SerializeCachedState();
            }

            GUILayout.Label("Unity property name");
            string updatedUnityPropertyName = GUILayout.TextField(_cachedState.UnityPropertyName);
            if (string.Equals(updatedUnityPropertyName, _cachedState.UnityPropertyName, StringComparison.Ordinal))
                return _serializedPayload;

            _cachedState.UnityPropertyName = updatedUnityPropertyName;
            return SerializeCachedState();
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            EntityInfoModuleState state = Parse(payload);
            EntityInfo entityInfo = EntityInfo.CreateUndefined();

            if (!string.IsNullOrWhiteSpace(state.Guid))
                entityInfo._guid = state.Guid;

            if (state.UseLocalizedName)
            {
                if (!string.IsNullOrWhiteSpace(state.LocaPath) && !string.IsNullOrWhiteSpace(state.LocaKey)){
                    entityInfo._locaName = new LocalizedString(state.LocaPath, state.LocaKey).Cast<IOptionalLoca>();
                }
                entityInfo._aliasNameModule = new AliasNameModule();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(state.UnityPropertyName))
                    entityInfo.name = state.UnityPropertyName;
            }

            creator.WithLazyEntityInfo(entityInfo);
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

        private string SerializeCachedState()
        {
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        private sealed class EntityInfoModuleState
        {
            /// <summary>
            /// Stable identity GUID for the custom EntityInfo. Generated once on creation and never changed.
            /// </summary>
            public string Guid { get; init; } = string.Empty;
            public bool UseLocalizedName { get; set; } = true;
            public string LocaPath { get; set; } = string.Empty;
            public string LocaKey { get; set; } = string.Empty;
            public string UnityPropertyName { get; set; } = string.Empty;

            public EntityInfoModuleState() { }

            public EntityInfoModuleState(string guid)
            {
                Guid = guid;
            }
        }
    }
}

