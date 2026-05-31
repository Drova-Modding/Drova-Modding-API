using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Spawning.Modules;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// External module for configuring routine points in the NPC wizard.
    /// Supports both manual coordinate entry and freecam-based point picking.
    /// </summary>
    internal sealed class ExternalRoutineModule : IExternalNpcModule
    {
        public const string ModuleKey = "routine";
        private static GUIStyle? _hintLabelStyle;
        private static GUIStyle? _warningLabelStyle;
        private static GUIStyle? _errorLabelStyle;
        private RoutineEditorMode _editorMode = RoutineEditorMode.Manual;
        private RoutineEditorState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private string _manualPointXInput = "0";
        private string _manualPointYInput = "0";
        private bool _freecamActive;
        private static RoutinePointVisualizer? _visualizer;

        public string Key => ModuleKey;
        public string DisplayName => "Routine";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new RoutineEditorState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);
            EnsureVisualizerExists();
            _visualizer?.SetPoints(_cachedState.Points, true);
            
            bool modeChanged = false;

            // Mode selection
            GUILayout.Label("Routine Configuration Mode");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Manual", GUILayout.Width(120f)))
            {
                if (_editorMode != RoutineEditorMode.Manual)
                {
                    _editorMode = RoutineEditorMode.Manual;
                    CleanupFreecam();
                    modeChanged = true;
                }
            }

            if (GUILayout.Button("Freecam Click", GUILayout.Width(120f)))
            {
                if (_editorMode != RoutineEditorMode.FreecamClick)
                {
                    _editorMode = RoutineEditorMode.FreecamClick;
                    modeChanged = true;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // Mode-specific UI
            if (_editorMode == RoutineEditorMode.Manual)
            {
                DrawManualMode();
            }
            else
            {
                DrawFreecamClickMode();
            }

            // Points list
            GUILayout.Space(8f);
            GUILayout.Label($"Routine Points ({_cachedState.Points.Count})");
            GUILayout.Label("(Cyan circles show points on screen)", GetHintLabelStyle());

            if (_cachedState.Points.Count > 0)
            {
                GUILayout.BeginVertical("box");
                for (int i = 0; i < _cachedState.Points.Count; i++)
                {
                    Vector2 point = _cachedState.Points[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Point {i}: ({point.x:F2}, {point.y:F2})", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", GUILayout.Width(30f)))
                    {
                        _cachedState.Points.RemoveAt(i);
                        modeChanged = true;
                        break; // Avoid index out of range
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                if (GUILayout.Button("Clear All Points", GUILayout.Width(120f)))
                {
                    _cachedState.Points.Clear();
                    modeChanged = true;
                }
            }
            else
            {
                GUILayout.Label("No routine points added yet.");
            }

            if (modeChanged)
            {
                _serializedPayload = JsonSerializer.Serialize(_cachedState);
                _lastPayload = _serializedPayload;
            }

            return _serializedPayload;
        }

        private void DrawManualMode()
        {
            GUILayout.Label("Enter coordinates and click 'Add Point'");
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(30f));
            _manualPointXInput = GUILayout.TextField(_manualPointXInput, GUILayout.Width(100f));
            GUILayout.Label("Y:", GUILayout.Width(30f));
            _manualPointYInput = GUILayout.TextField(_manualPointYInput, GUILayout.Width(100f));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Add Point", GUILayout.Width(100f)))
            {
                if (float.TryParse(_manualPointXInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(_manualPointYInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                {
                    _cachedState.Points.Add(new Vector2(x, y));
                    _manualPointXInput = "0";
                    _manualPointYInput = "0";
                    _serializedPayload = JsonSerializer.Serialize(_cachedState);
                    _lastPayload = _serializedPayload;
                }
                else
                {
                    GUILayout.Label("Invalid coordinates. Use numbers only.", GetErrorLabelStyle());
                }
            }
        }

        private void DrawFreecamClickMode()
        {
            GUILayout.Label("Use freecam to position the camera, then left-click in the world to add points.");
            GUILayout.Space(4f);

            if (_freecamActive)
            {
                if (GUILayout.Button("Stop Freecam (camstop)", GUILayout.Width(150f)))
                {
                    CheatMenuAccess.FireCommand("camstop");
                    _freecamActive = false;
                }
                GUILayout.Label("Freecam is ACTIVE. Use your input to move the camera, then click 'Stop Freecam'.", GetWarningLabelStyle());
            }
            else
            {
                if (GUILayout.Button("Start Freecam (camstop)", GUILayout.Width(150f)))
                {
                    CheatMenuAccess.FireCommand("camstop");
                    _freecamActive = true;
                }
                GUILayout.Label("Click to activate freecam mode.");
            }

            GUILayout.Space(4f);
            GUILayout.Label("Left-click in the world while freecam is active to add a routine point.");
        }

        public void OnWizardUpdate()
        {
            if (!_freecamActive || _editorMode != RoutineEditorMode.FreecamClick)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            if (!TryGetMouseWorldPosition(out Vector2 worldPosition))
                return;

            _cachedState.Points.Add(worldPosition);
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
        }

        private static bool TryGetMouseWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = default;

            Camera? camera = Camera.main;
            if (camera == null)
                return false;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Plane worldPlane = new(Vector3.forward, Vector3.zero);
            if (!worldPlane.Raycast(ray, out float enter))
                return false;

            Vector3 hitPoint = ray.GetPoint(enter);
            worldPosition = new Vector2(hitPoint.x, hitPoint.y);
            return true;
        }

        private void CleanupFreecam()
        {
            if (_freecamActive)
            {
                CheatMenuAccess.FireCommand("camstop");
                _freecamActive = false;
            }
        }

        private static void EnsureVisualizerExists()
        {
            if (_visualizer != null)
                return;

            GameObject visualizerObject = GameObject.Find("RoutinePointVisualizer");
            if (visualizerObject == null)
            {
                visualizerObject = new GameObject("RoutinePointVisualizer");
                UnityEngine.Object.DontDestroyOnLoad(visualizerObject);
            }

            _visualizer = visualizerObject.GetComponent<RoutinePointVisualizer>();
            if (_visualizer == null)
            {
                _visualizer = visualizerObject.AddComponent<RoutinePointVisualizer>();
            }
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            RoutineEditorState state = Parse(payload);
            if (state.Points.Count == 0)
                return;

            creator.WithModule(new RoutineModule().With([.. state.Points]));
        }

        private static RoutineEditorState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new RoutineEditorState();

            try
            {
                return JsonSerializer.Deserialize<RoutineEditorState>(payload) ?? new RoutineEditorState();
            }
            catch
            {
                return new RoutineEditorState();
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

        private enum RoutineEditorMode
        {
            Manual,
            FreecamClick
        }

        private static GUIStyle GetHintLabelStyle()
        {
            _hintLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 10
            };
            _hintLabelStyle.normal.textColor = Color.gray;
            return _hintLabelStyle;
        }

        private static GUIStyle GetWarningLabelStyle()
        {
            _warningLabelStyle ??= new GUIStyle(GUI.skin.label);
            _warningLabelStyle.normal.textColor = Color.yellow;
            return _warningLabelStyle;
        }

        private static GUIStyle GetErrorLabelStyle()
        {
            _errorLabelStyle ??= new GUIStyle(GUI.skin.label);
            _errorLabelStyle.normal.textColor = Color.red;
            return _errorLabelStyle;
        }

        private sealed class RoutineEditorState
        {
            public List<Vector2> Points { get; set; } = [];
        }
    }
}






