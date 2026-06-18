using Il2Cpp;
using Il2CppDrova;
using Il2CppDrova.GlobalVarSystem;
using MelonLoader;
using System.Text.Json;
using UnityEngine;
using Drova_Modding_API.Systems.Editor;
using Drova_Modding_API.Systems.GlobalVars;
using Drova_Modding_API.Systems.Spawning.Modules;

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

            bool changed = false;
            GUILayout.Label("EntityInfo GUID (read-only)");
            GUILayout.Label(_cachedState.Guid);

            GUILayout.Label("EntityInfo name (Unity ScriptableObject name)");
            string updatedName = GUILayout.TextField(_cachedState.UnityPropertyName);
            if (!string.Equals(updatedName, _cachedState.UnityPropertyName, StringComparison.Ordinal))
            {
                _cachedState.UnityPropertyName = updatedName;
                changed = true;
            }

            bool updatedAlwaysKnown = GUILayout.Toggle(_cachedState.AlwaysKnown, "Always known");
            if (updatedAlwaysKnown != _cachedState.AlwaysKnown)
            {
                _cachedState.AlwaysKnown = updatedAlwaysKnown;
                changed = true;
            }

            if (!_cachedState.AlwaysKnown)
            {
                #if DEBUG
                if (GlobalVarInspectorSystem.TryConsumeSelection(ModuleKey, _cachedState.KnownTypeName, out string selectedGvarListGuid, out string selectedGvarGuid))
                {
                    _cachedState.KnownByGBool = new GBoolReference
                    {
                        GVarListGuid = selectedGvarListGuid,
                        GBoolGuid = selectedGvarGuid
                    };
                    changed = true;
                }
#endif

                GUILayout.Label("Known when this GBool is true");
                GUILayout.Label(GetKnownByGvarLabel());

                GUILayout.BeginHorizontal();
                try
                {
#if DEBUG
                    if (GUILayout.Button("Pick from GVar Inspector", GUILayout.Width(220f)))
                        GlobalVarInspectorSystem.RequestSelection(ModuleKey, _cachedState.KnownTypeName);
#else
                    GUI.enabled = false;
                    GUILayout.Button("Pick from GVar Inspector", GUILayout.Width(220f));
                    GUI.enabled = true;
#endif
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

                if (_cachedState.KnownByGBool == null)
                    GUILayout.Label("Select a GBool in the global var inspector, then click the selection button there.");
            }

            bool updatedUseLocalizedName = GUILayout.Toggle(_cachedState.UseLocalizedName, "Use localized display name");
            if (updatedUseLocalizedName != _cachedState.UseLocalizedName)
            {
                _cachedState.UseLocalizedName = updatedUseLocalizedName;
                changed = true;
            }

            if (_cachedState.UseLocalizedName)
            {
                if (LocalizationEntriesEditorSystem.TryConsumeSelection(ModuleKey, out string selectedLocaPath, out string selectedLocaKey))
                {
                    _cachedState.LocaPath = selectedLocaPath;
                    _cachedState.LocaKey = selectedLocaKey;
                    changed = true;
                }

                GUILayout.Label("Localized name path");
                string updatedLocaPath = GUILayout.TextField(_cachedState.LocaPath);

                GUILayout.Label("Localized name key");
                string updatedLocaKey = GUILayout.TextField(_cachedState.LocaKey);

                GUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button("Open Localization Editor", GUILayout.Width(200f)))
                        LocalizationEntriesEditorSystem.Show();

                    if (GUILayout.Button("Pick Entry", GUILayout.Width(120f)))
                        LocalizationEntriesEditorSystem.RequestSelection(ModuleKey);
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

                if (!string.Equals(updatedLocaPath, _cachedState.LocaPath, StringComparison.Ordinal)
                    || !string.Equals(updatedLocaKey, _cachedState.LocaKey, StringComparison.Ordinal))
                {
                    _cachedState.LocaPath = updatedLocaPath;
                    _cachedState.LocaKey = updatedLocaKey;
                    changed = true;
                }

                return changed ? SerializeCachedState() : _serializedPayload;
            }

            return changed ? SerializeCachedState() : _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            EntityInfoModuleState state = Parse(payload);

            // Reuse the pre-created instance from the registry when available so that
            // any cross-references captured before this NPC was spawned remain valid.
            EntityInfo entityInfo = !string.IsNullOrWhiteSpace(state.Guid)
                ? ExternalEntityInfoRegistry.GetOrCreate(state.Guid)
                : EntityInfo.CreateUndefined();

            if (state.AlwaysKnown)
            {
                AlwaysAcquainted acquaintance = new();
                entityInfo._acquaintance = acquaintance.Cast<IAcquaintance>();
            }
            else
            {
                GBool? knownByBool = state.KnownByGBool?.Resolve();
                if (knownByBool != null)
                {
                    Acquaintance acquaintance = new(knownByBool);
                    entityInfo._acquaintance = acquaintance.Cast<IAcquaintance>();
                }
                else
                {
                    MelonLogger.Warning($"{nameof(ExternalEntityInfoModule)} for entity '{state.Guid}' is configured to be GBool-gated, but the selected GBool could not be resolved. Falling back to always known.");
                    AlwaysAcquainted acquaintance = new();
                    entityInfo._acquaintance = acquaintance.Cast<IAcquaintance>();
                }
            }

            if (state.UseLocalizedName)
            {
                if (!string.IsNullOrWhiteSpace(state.LocaPath) && !string.IsNullOrWhiteSpace(state.LocaKey)){
                    entityInfo._locaName = new LocalizedString(state.LocaPath, state.LocaKey).Cast<IOptionalLoca>();
                }
                entityInfo._aliasNameModule = new AliasNameModule();
            }

            // The Unity object name is independent of the localized display name; apply it
            // whenever one was configured so the ScriptableObject is identifiable in-game.
            if (!string.IsNullOrWhiteSpace(state.UnityPropertyName))
                entityInfo.name = state.UnityPropertyName;

            creator.WithLazyEntityInfo(entityInfo);
            creator.WithModule(new EntityInfoModule(entityInfo));
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

        private string GetKnownByGvarLabel()
        {
            GBool? selectedBool = _cachedState.KnownByGBool?.Resolve();
            if (selectedBool != null)
            {
                GVarList? parent = selectedBool.GetParent();
                if (parent != null)
                    return $"Selected GBool: {parent.name}/{selectedBool.name}";

                return $"Selected GBool: {selectedBool.name}";
            }

            if (_cachedState.KnownByGBool != null)
            {
                return $"Selected GBool: {_cachedState.KnownByGBool.GVarListGuid}/{_cachedState.KnownByGBool.GBoolGuid}";
            }

            return "Selected GBool: <none>";
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
            public bool AlwaysKnown { get; set; } = true;
            public string KnownTypeName { get; set; } = nameof(GBool);
            public GBoolReference? KnownByGBool { get; set; }

            public EntityInfoModuleState() { }

            public EntityInfoModuleState(string guid)
            {
                Guid = guid;
            }
        }

    }
}

