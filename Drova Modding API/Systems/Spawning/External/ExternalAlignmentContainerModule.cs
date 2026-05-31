using Drova_Modding_API.Systems.Spawning.Modules;
using Il2CppDrova.Alignment;
using MelonLoader;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// External module for NPC alignment handling.
    /// Supports selecting an explicit <see cref="AlignmentContainer"/> override
    /// resolved at spawn time via
    /// <see cref="Resources.FindObjectsOfTypeAll{T}"/>.
    /// </summary>
    internal sealed class ExternalAlignmentContainerModule : IExternalNpcModule
    {
        public const string ModuleKey = "alignment_container";
        private AlignmentContainerModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private List<AlignmentContainer>? _cachedContainers;
        private AlignmentMatrixContainer? _cachedMatrix;
        private bool _showContainerList;
        private bool _showMatrix;
        private Vector2 _containerListScroll;
        private Vector2 _matrixScroll;
        private static GUIStyle? _infoStyle;
        private static GUIStyle? _cellFriendlyStyle;
        private static GUIStyle? _cellNeutralStyle;
        private static GUIStyle? _cellEnemyStyle;
        private static GUIStyle? _cellHeaderStyle;
        private const float HeaderCellWidth = 140f;
        private const float DataCellWidth = 70f;
        private const float MatrixRowHeight = 22f;
        public string Key => ModuleKey;
        public string DisplayName => "Alignment";
        public string CreateDefaultPayload() => JsonSerializer.Serialize(new AlignmentContainerModuleState());
        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);
            EnsureContainerCache();
            bool changed = false;
            GUILayout.Label("Alignment");

            GUILayout.BeginHorizontal();
            if (string.IsNullOrWhiteSpace(_cachedState.ContainerName))
                GUILayout.Label("No override (template alignment)", GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(_cachedState.ContainerName, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Clear", GUILayout.Width(60f)))
            {
                _cachedState.ContainerName = string.Empty;
                changed = true;
            }
            if (GUILayout.Button(_showContainerList ? "Hide" : "Pick", GUILayout.Width(60f)))
                _showContainerList = !_showContainerList;
            GUILayout.EndHorizontal();

            // Show live info for the currently selected container.
            if (!string.IsNullOrWhiteSpace(_cachedState.ContainerName) && _cachedContainers != null)
            {
                AlignmentContainer? selected = FindByName(_cachedContainers, _cachedState.ContainerName);
                GUILayout.Label(selected != null
                        ? $"  ID: {selected.GetID()}  |  Enemy with player: {selected._isEnemyWithPlayer}"
                        : $"  (''{_cachedState.ContainerName}'' not found in scene - will be resolved at spawn)",
                    GetInfoStyle());
            }

            // Container picker list
            if (_showContainerList && _cachedContainers != null)
            {
                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Available ({_cachedContainers.Count})", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
                {
                    RefreshContainerCache();
                    _cachedMatrix = null;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginVertical("box");
                _containerListScroll = GUILayout.BeginScrollView(_containerListScroll, GUILayout.Height(180f));
                for (int i = 0; i < _cachedContainers.Count; i++)
                {
                    AlignmentContainer container = _cachedContainers[i];
                    bool isSelected = string.Equals(container.name, _cachedState.ContainerName, StringComparison.Ordinal);
                    string label = $"{(isSelected ? "> " : string.Empty)}{container.name}  [enemy:{container._isEnemyWithPlayer}  id:{container.GetID()}]";
                    if (GUILayout.Button(label, GUILayout.ExpandWidth(true)))
                    {
                        _cachedState.ContainerName = container.name;
                        _showContainerList = false;
                        changed = true;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }

            // Alignment matrix viewer
            GUILayout.Space(6f);
            if (GUILayout.Button(_showMatrix ? "Hide Alignment Matrix" : "Show Alignment Matrix", GUILayout.Width(200f)))
                _showMatrix = !_showMatrix;
            if (_showMatrix)
                DrawAlignmentMatrix();
            if (changed)
            {
                _serializedPayload = JsonSerializer.Serialize(_cachedState);
                _lastPayload = _serializedPayload;
            }
            return _serializedPayload;
        }
        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            AlignmentContainerModuleState state = Parse(payload);
            if (string.IsNullOrWhiteSpace(state.ContainerName))
                return;

            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<AlignmentContainer> all =
                Resources.FindObjectsOfTypeAll<AlignmentContainer>();
            for (int i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i].name, state.ContainerName, StringComparison.Ordinal))
                {
                    creator.WithModule(new AlignmentModule(all[i]));
                    return;
                }
            }
            MelonLogger.Warning($"[ExternalAlignmentContainerModule] AlignmentContainer ''{state.ContainerName}'' was not found at spawn time.");
        }

        // -- Matrix drawing ----------------------------------------------------
        private void DrawAlignmentMatrix()
        {
            EnsureMatrixCache();
            if (_cachedMatrix == null)
            {
                GUILayout.Label("No AlignmentMatrixContainer found in scene.", GetInfoStyle());
                return;
            }
            Il2CppSystem.Collections.Generic.List<AlignmentContainer>? il2CppList = _cachedMatrix._alignmentContainers;
            if (il2CppList == null || il2CppList.Count == 0)
            {
                GUILayout.Label("Matrix has no containers.", GetInfoStyle());
                return;
            }
            List<AlignmentContainer> containers = new(il2CppList.Count);
            for (int i = 0; i < il2CppList.Count; i++)
            {
                if (il2CppList[i] != null)
                    containers.Add(il2CppList[i]);
            }
            EnsureCellStyles();
            GUILayout.Space(4f);
            GUILayout.Label($"Alignment Matrix ({containers.Count}x{containers.Count})  -- row checks col", GetInfoStyle());
            GUILayout.Label("Green=Friendly  Yellow=Neutral  Red=Enemy  >=selected", GetInfoStyle());
            GUILayout.Space(2f);
            _matrixScroll = GUILayout.BeginScrollView(_matrixScroll, GUILayout.Height(MatrixRowHeight * (containers.Count + 2) + 24f));
            // Header row
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Empty, GUILayout.Width(HeaderCellWidth), GUILayout.Height(MatrixRowHeight));
            for (int col = 0; col < containers.Count; col++)
            {
                bool isSelectedCol = string.Equals(containers[col].name, _cachedState.ContainerName, StringComparison.Ordinal);
                string shortName = GetShortName(containers[col].name);
                GUILayout.Label(isSelectedCol ? $">{shortName}" : shortName, _cellHeaderStyle!, GUILayout.Width(DataCellWidth), GUILayout.Height(MatrixRowHeight));
            }
            GUILayout.EndHorizontal();
            // Data rows
            for (int row = 0; row < containers.Count; row++)
            {
                AlignmentContainer rowContainer = containers[row];
                bool isSelectedRow = string.Equals(rowContainer.name, _cachedState.ContainerName, StringComparison.Ordinal);
                GUILayout.BeginHorizontal();
                string rowLabel = isSelectedRow ? $"> {GetShortName(rowContainer.name)}" : GetShortName(rowContainer.name);
                GUILayout.Label(rowLabel, isSelectedRow ? _cellHeaderStyle! : GetInfoStyle(), GUILayout.Width(HeaderCellWidth), GUILayout.Height(MatrixRowHeight));
                for (int col = 0; col < containers.Count; col++)
                {
                    AlignmentState state = GetStateBetween(_cachedMatrix, rowContainer, containers[col]);
                    GUILayout.Label(StateToShortString(state), GetCellStyle(state), GUILayout.Width(DataCellWidth), GUILayout.Height(MatrixRowHeight));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
        private static AlignmentState GetStateBetween(AlignmentMatrixContainer matrix, AlignmentContainer a, AlignmentContainer b)
        {
            if (matrix.CheckState(a, b, AlignmentState.Friendly))
                return AlignmentState.Friendly;
            if (matrix.CheckState(a, b, AlignmentState.Enemy))
                return AlignmentState.Enemy;
            return AlignmentState.Neutral;
        }
        private static string StateToShortString(AlignmentState state) => state switch
        {
            AlignmentState.Friendly => "Friend",
            AlignmentState.Enemy => "Enemy",
            _ => "Neutral"
        };
        private static string GetShortName(string name)
        {
            // "Alignment_Wild_Human" -> "Wild_Human"
            int idx = name.IndexOf('_');
            return idx > 0 && idx < name.Length - 1 ? name[(idx + 1)..] : name;
        }
        // -- Caching -----------------------------------------------------------
        private void EnsureContainerCache()
        {
            if (_cachedContainers != null)
                return;
            RefreshContainerCache();
        }
        private void RefreshContainerCache()
        {
            EnsureMatrixCache();

            // Merge matrix containers and all discovered containers.
            // Matrix entries are added first to prefer canonical runtime references.
            Dictionary<string, AlignmentContainer> uniqueByName = new(StringComparer.OrdinalIgnoreCase);

            if (_cachedMatrix != null)
            {
                Il2CppSystem.Collections.Generic.List<AlignmentContainer>? il2CppList = _cachedMatrix._alignmentContainers;
                if (il2CppList != null)
                {
                    for (int i = 0; i < il2CppList.Count; i++)
                    {
                        AlignmentContainer? container = il2CppList[i];
                        if (container == null || string.IsNullOrWhiteSpace(container.name))
                            continue;

                        uniqueByName[container.name] = container;
                    }
                }
            }

            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<AlignmentContainer> all =
                Resources.FindObjectsOfTypeAll<AlignmentContainer>();

            for (int i = 0; i < all.Count; i++)
            {
                AlignmentContainer? container = all[i];
                if (container == null || string.IsNullOrWhiteSpace(container.name))
                    continue;

                uniqueByName.TryAdd(container.name, container);
            }

            _cachedContainers = [.. uniqueByName.Values];
            _cachedContainers.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        }
        private void EnsureMatrixCache()
        {
            if (_cachedMatrix != null)
                return;
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<AlignmentMatrixContainer> all =
                Resources.FindObjectsOfTypeAll<AlignmentMatrixContainer>();
            if (all.Count > 0)
                _cachedMatrix = all[0];
        }
        private static AlignmentContainer? FindByName(List<AlignmentContainer> containers, string name)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                if (string.Equals(containers[i].name, name, StringComparison.Ordinal))
                    return containers[i];
            }
            return null;
        }
        // -- Parsing -----------------------------------------------------------
        private static AlignmentContainerModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new AlignmentContainerModuleState();
            try
            {
                return JsonSerializer.Deserialize<AlignmentContainerModuleState>(payload) ?? new AlignmentContainerModuleState();
            }
            catch
            {
                return new AlignmentContainerModuleState();
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
        // -- Styles ------------------------------------------------------------
        private static void EnsureCellStyles()
        {
            _cellFriendlyStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _cellFriendlyStyle.normal.textColor = new Color(0.2f, 0.9f, 0.2f);
            _cellNeutralStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };
            _cellNeutralStyle.normal.textColor = new Color(0.8f, 0.8f, 0.4f);
            _cellEnemyStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _cellEnemyStyle.normal.textColor = new Color(0.95f, 0.25f, 0.25f);
            _cellHeaderStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _cellHeaderStyle.normal.textColor = new Color(0f, 0.85f, 1f);
        }
        private static GUIStyle GetCellStyle(AlignmentState state) => state switch
        {
            AlignmentState.Friendly => _cellFriendlyStyle!,
            AlignmentState.Enemy => _cellEnemyStyle!,
            _ => _cellNeutralStyle!
        };
        private static GUIStyle GetInfoStyle()
        {
            _infoStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 10 };
            _infoStyle.normal.textColor = Color.gray;
            return _infoStyle;
        }
        // -- State -------------------------------------------------------------
        private sealed class AlignmentContainerModuleState
        {
            /// <summary>
            /// Unity object name of the <see cref="AlignmentContainer"/> to apply.
            /// Empty means no explicit override and template alignment is kept.
            /// </summary>
            public string ContainerName { get; set; } = string.Empty;
        }

    }
}