using System.Globalization;
using System.Text.Json;
using Drova_Modding_API.Converters;
using Drova_Modding_API.Systems.Dialogues.Editor;
using Drova_Modding_API.Systems.Editor;
using Drova_Modding_API.Systems.Routines.Store;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;
using Il2CppDrova.Routine;
using Il2CppInterop.Runtime.Attributes;
using Il2CppNodeCanvas.BehaviourTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Routines.Editor
{
    /// <summary>
    /// Debug window for the routine of the selected NPC: opens the shared routine graphs or the NPC's own graph in the
    /// <see cref="GraphEditorManager"/>, and edits the NPC's <see cref="RoutineOverride"/> (own places, replacing the vanilla ones).
    /// </summary>
    [RegisterTypeInIl2Cpp]
    internal class RoutineEditorWindow(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private const int WindowId = 925601;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new Vector2JsonConverter() }
        };

        private readonly ExternalRoutineModule _pointEditor = new();
        private Rect _windowRect = new(20, 220, 460, 620);
        private Vector2 _scroll;
        private GraphEditorManager? _graphEditor;
        private RoutineOverride? _override;
        private string? _actorGuid;
        private string? _storedKey;
        private string _pointsPayload = string.Empty;
        private string _status = string.Empty;
        private bool _isVisible;
        private readonly List<PlaceView> _existingPlaces = [];
        private readonly Dictionary<string, string> _positionTexts = new(StringComparer.Ordinal);
        private RoutinePlaceEdit? _pickEdit;
        private int _pickIndex;
        private string _title = string.Empty;
        private GUI.WindowFunction? _windowFunction;

        [HideFromIl2Cpp]
        internal void Open(Actor actor, GraphEditorManager graphEditor)
        {
            EntityInfo? entityInfo = actor.GetEntityInfo();
            if (entityInfo == null || string.IsNullOrEmpty(entityInfo.GUID))
            {
                MelonLogger.Warning($"RoutineEditorWindow: '{actor.name}' has no EntityInfo.");
                return;
            }

            _graphEditor = graphEditor;
            string? actorGuid = actor.GetGuidComponent()?.GuidString;
            _actorGuid = string.IsNullOrEmpty(actorGuid) ? null : actorGuid;
            if ((_actorGuid != null && RoutineOverrideStore.TryGet(_actorGuid, out RoutineOverride? stored))
                || RoutineOverrideStore.TryGet(entityInfo.GUID, out stored))
            {
                _override = stored;
                _storedKey = stored.Key;
            }
            else
            {
                _override = new RoutineOverride { EntityGuid = entityInfo.GUID, ActorGuid = _actorGuid, Name = actor.name };
                _storedKey = null;
            }
            List<Vector2> points = _override.Places.Count > 0 ? _override.Places[0].Points : [];
            _pointsPayload = JsonSerializer.Serialize(new { Points = points }, JsonOptions);
            _existingPlaces.Clear();
            foreach (BaseRoutinePlace place in RoutineOverrideStore.FindExistingPlaces(_actorGuid, entityInfo.GUID))
            {
                _existingPlaces.Add(new PlaceView(place));
            }
            _title = "Routine: " + _override.Name;
            _positionTexts.Clear();
            _pickEdit = null;
            _status = string.Empty;
            _isVisible = true;
            EditorManager.InEditor = true;
        }

        internal void Update()
        {
            if (!_isVisible || IsGraphOpen) return;

            if (_pickEdit == null)
            {
                _pointEditor.OnWizardUpdate();
                return;
            }

            Vector2 guiMouse = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (!Input.GetMouseButtonDown(0) || _windowRect.Contains(guiMouse)) return;
            if (!ExternalRoutineModule.TryGetMouseWorldPosition(out Vector2 world)) return;

            SetPointPosition(_pickEdit, _pickIndex, world);
            _pickEdit = null;
        }

        internal void OnGUI()
        {
            if (!_isVisible || IsGraphOpen) return;

            _windowFunction ??= new Action<int>(DrawWindow);
            _windowRect = GUI.Window(WindowId, _windowRect, _windowFunction, _title);
        }

        private bool IsGraphOpen => _graphEditor != null && _graphEditor.Graph != null;

        [HideFromIl2Cpp]
        private void DrawWindow(int id)
        {
            RoutineOverride routineOverride = _override!;
            GUILayout.BeginArea(new Rect(10, 25, _windowRect.width - 20, _windowRect.height - 35));
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Select a GUID or PlaceKey and press Ctrl+C to copy it into an override file.");
            DrawCopyable("Entity GUID", routineOverride.EntityGuid);
            if (_actorGuid != null) DrawCopyable("Actor GUID", _actorGuid);
            if (_actorGuid != null)
            {
                bool onlyThisNpc = GUILayout.Toggle(routineOverride.ActorGuid != null, "Only this NPC (otherwise every NPC with this EntityInfo)");
                routineOverride.ActorGuid = onlyThisNpc ? _actorGuid : null;
            }
            GUILayout.Space(6);
            GUILayout.Label("Routine graph: " + (routineOverride.GraphId ?? "shared"));
            if (GUILayout.Button("Edit shared routine graph (affects every NPC)")) OpenGraph(RoutineGraphAccess.DefaultRoutineGraph);
            if (GUILayout.Button("Edit shared group graph (affects every NPC)")) OpenGraph(RoutineGraphAccess.DefaultGroupGraph);
            if (routineOverride.GraphId == null)
            {
                if (GUILayout.Button("Create own graph from the shared one")) CreateOwnGraph(routineOverride);
            }
            else
            {
                if (GUILayout.Button("Edit own graph")) OpenOwnGraph(routineOverride.GraphId);
                if (GUILayout.Button("Use shared graph again")) routineOverride.GraphId = null;
            }

            GUILayout.Space(10);
            routineOverride.ReplaceExisting = GUILayout.Toggle(routineOverride.ReplaceExisting, "Replace the NPC's existing routine places");
            if (!routineOverride.ReplaceExisting) DrawExistingPlaces(routineOverride);

            GUILayout.Space(10);
            GUILayout.Label("New routine place");
            _pointsPayload = _pointEditor.DrawWizardUiAndSerialize(_pointsPayload);

            GUILayout.Space(10);
            GUILayout.Label("Graph and existing place changes apply on save; new places and replacing on the next load of the world.");
            if (GUILayout.Button("Save")) Save(routineOverride);
            if (GUILayout.Button("Delete override") && _storedKey != null)
            {
                RoutineOverrideStore.Delete(_storedKey);
                _storedKey = null;
                _status = "Deleted.";
            }
            if (GUILayout.Button("Close")) Close();
            if (_status.Length > 0) GUILayout.Label(_status);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        /// <summary>
        /// The NPC's routine places as the game ships them, each point with its authored index, editable position
        /// (typed, or picked with a click in the world) and a remove toggle.
        /// </summary>
        [HideFromIl2Cpp]
        private void DrawExistingPlaces(RoutineOverride routineOverride)
        {
            GUILayout.Label($"Existing routine places ({_existingPlaces.Count})");
            if (_pickEdit != null) GUILayout.Label("Click in the world to place the point (outside this window).");

            foreach (PlaceView view in _existingPlaces)
            {
                if (view.Place == null) continue;

                RoutinePlaceEdit edit = GetOrCreateEdit(routineOverride, view);
                GUILayout.BeginVertical("box");
                GUILayout.Label(view.GetHeader());
                DrawCopyable("PlaceKey", view.Key);
                edit.Disabled = GUILayout.Toggle(edit.Disabled, "Switch this place off");

                for (int i = 0; i < view.Points.Count; i++)
                {
                    RoutinePointEdit? pointEdit = FindPointEdit(edit, i);
                    bool exists = view.Points[i] != null;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(exists ? view.Labels[i] : view.RemovedLabels[i], GUILayout.Width(150));
                    if (exists && view.Movable[i])
                    {
                        if (!_positionTexts.TryGetValue(view.TextKeys[i], out string text)) text = FormatPosition(pointEdit?.Position ?? view.Authored[i]);
                        string typed = GUILayout.TextField(text, GUILayout.Width(130));
                        _positionTexts[view.TextKeys[i]] = typed;
                        if (typed != text && TryParsePosition(typed, out Vector2 parsed)) SetPointPosition(edit, i, parsed, keepText: true);
                        if (GUILayout.Button("Pick", GUILayout.Width(45))) (_pickEdit, _pickIndex) = (edit, i);
                    }
                    else
                    {
                        GUILayout.Label(view.FixedTexts[i], GUILayout.Width(179));
                    }
                    bool removed = GUILayout.Toggle(pointEdit?.Removed ?? false, "X", GUILayout.Width(30));
                    if (removed != (pointEdit?.Removed ?? false)) GetOrCreatePointEdit(edit, i).Removed = removed;
                    GUILayout.EndHorizontal();
                }

                for (int i = 0; i < edit.AddedPoints.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"+{i}: {FormatPosition(edit.AddedPoints[i])}", GUILayout.Width(285));
                    if (GUILayout.Button("X", GUILayout.Width(30)))
                    {
                        edit.AddedPoints.RemoveAt(i);
                        GUILayout.EndHorizontal();
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Add point (click in the world)")) (_pickEdit, _pickIndex) = (edit, -1);
                GUILayout.EndVertical();
            }
        }

        /// <summary>
        /// A label and a text field holding <paramref name="value"/>, so it can be selected and copied.
        /// </summary>
        private static void DrawCopyable(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            GUILayout.TextField(value);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Sets an authored point's position, or appends a new point when <paramref name="index"/> is -1.
        /// </summary>
        [HideFromIl2Cpp]
        private void SetPointPosition(RoutinePlaceEdit edit, int index, Vector2 position, bool keepText = false)
        {
            if (index < 0)
            {
                edit.AddedPoints.Add(position);
                return;
            }

            GetOrCreatePointEdit(edit, index).Position = position;
            if (!keepText) _positionTexts.Remove(edit.PlaceKey + "#" + index);
        }

        [HideFromIl2Cpp]
        private static RoutinePlaceEdit GetOrCreateEdit(RoutineOverride routineOverride, PlaceView view)
        {
            foreach (RoutinePlaceEdit candidate in routineOverride.PlaceEdits)
            {
                if (string.Equals(candidate.PlaceKey, view.Key, StringComparison.OrdinalIgnoreCase)) return candidate;
            }

            RoutinePlaceEdit edit = new() { PlaceKey = view.Key, Name = view.Name };
            routineOverride.PlaceEdits.Add(edit);
            return edit;
        }

        [HideFromIl2Cpp]
        private static RoutinePointEdit? FindPointEdit(RoutinePlaceEdit edit, int index)
        {
            foreach (RoutinePointEdit candidate in edit.Points)
            {
                if (candidate.Index == index) return candidate;
            }
            return null;
        }

        [HideFromIl2Cpp]
        private static RoutinePointEdit GetOrCreatePointEdit(RoutinePlaceEdit edit, int index)
        {
            RoutinePointEdit? pointEdit = FindPointEdit(edit, index);
            if (pointEdit != null) return pointEdit;

            pointEdit = new RoutinePointEdit { Index = index };
            edit.Points.Add(pointEdit);
            return pointEdit;
        }

        private static string FormatPosition(Vector2 position)
            => position.x.ToString("0.##", CultureInfo.InvariantCulture) + "; " + position.y.ToString("0.##", CultureInfo.InvariantCulture);

        private static bool TryParsePosition(string text, out Vector2 position)
        {
            position = default;
            string[] parts = text.Split(';');
            if (parts.Length != 2) return false;

            bool parsedX = float.TryParse(parts[0].Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
            bool parsedY = float.TryParse(parts[1].Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
            position = new Vector2(x, y);
            return parsedX && parsedY;
        }

        [HideFromIl2Cpp]
        private void Save(RoutineOverride routineOverride)
        {
            List<Vector2> points = JsonSerializer.Deserialize<PointsPayload>(_pointsPayload, JsonOptions)?.Points ?? [];
            routineOverride.Places = points.Count > 0 ? [new RoutinePlaceOverride { Points = points }] : [];
            routineOverride.PlaceEdits.RemoveAll(edit => !edit.Disabled && edit.Points.Count == 0 && edit.AddedPoints.Count == 0);
            if (_storedKey != null && _storedKey != routineOverride.Key) RoutineOverrideStore.Delete(_storedKey);
            bool saved = RoutineOverrideStore.Save(routineOverride);
            if (saved) _storedKey = routineOverride.Key;
            _status = saved ? "Saved." : "Saving failed, see the log.";
        }

        /// <summary>
        /// Copies the shared routine graph into the store under a name of its own and assigns it to the NPC.
        /// </summary>
        [HideFromIl2Cpp]
        private void CreateOwnGraph(RoutineOverride routineOverride)
        {
            BehaviourTree? shared = RoutineGraphAccess.DefaultRoutineGraph;
            if (shared == null)
            {
                _status = "The shared routine graph is not loaded.";
                return;
            }

            string treeId = "Routine_" + routineOverride.Key;
            BehaviourTree copy = Instantiate(shared);
            copy.name = treeId;
            if (!BehaviourTreeStore.SaveBehaviourTree(treeId, copy))
            {
                _status = "Saving the copy failed, see the log.";
                return;
            }

            routineOverride.GraphId = treeId;
            Save(routineOverride);
            OpenGraph(copy);
        }

        [HideFromIl2Cpp]
        private void OpenOwnGraph(string treeId)
        {
            RoutineGraphAccess.Invalidate(treeId);
            if (!BehaviourTreeStore.TryLoadBehaviourTree(treeId, out BehaviourTree? tree))
            {
                _status = $"Graph '{treeId}' could not be loaded.";
                return;
            }
            OpenGraph(tree);
        }

        [HideFromIl2Cpp]
        private void OpenGraph(BehaviourTree? tree)
        {
            if (tree == null || _graphEditor == null) return;
            _graphEditor.gameObject.SetActive(true);
            _graphEditor.Graph = tree;
        }

        [HideFromIl2Cpp]
        private void Close()
        {
            _isVisible = false;
            EditorManager.InEditor = false;
        }

        private sealed class PointsPayload
        {
            public List<Vector2> Points { get; set; } = [];
        }

        /// <summary>
        /// What the window shows for one existing place, built once when it opens.
        /// </summary>
        private sealed class PlaceView
        {
            private RoutineState _headerState;
            private string _header = string.Empty;

            public PlaceView(BaseRoutinePlace place)
            {
                Place = place;
                Key = place.Key();
                Name = place.Name;
                Points = RoutineOverrideStore.GetAuthoredPoints(place, out IReadOnlyList<Vector2> authored);
                Authored = authored;
                Labels = new string[Points.Count];
                RemovedLabels = new string[Points.Count];
                TextKeys = new string[Points.Count];
                FixedTexts = new string[Points.Count];
                Movable = new bool[Points.Count];
                for (int i = 0; i < Points.Count; i++)
                {
                    RoutinePoint? point = Points[i];
                    Labels[i] = i + ": " + (point != null ? point.GetIl2CppType().Name : "removed");
                    RemovedLabels[i] = i + ": removed";
                    TextKeys[i] = Key + "#" + i;
                    FixedTexts[i] = FormatPosition(authored[i]) + " (at its object)";
                    Movable[i] = point != null && RoutineOverrideStore.IsMovable(point);
                }
            }

            public BaseRoutinePlace Place { get; }
            public string Key { get; }
            public string Name { get; }
            public IReadOnlyList<RoutinePoint?> Points { get; }
            public IReadOnlyList<Vector2> Authored { get; }
            public string[] Labels { get; }
            public string[] RemovedLabels { get; }
            public string[] TextKeys { get; }
            public string[] FixedTexts { get; }
            public bool[] Movable { get; }

            /// <summary>
            /// The place's name and current state, rebuilt only when the state changes.
            /// </summary>
            public string GetHeader()
            {
                RoutineState state = Place.State;
                if (_header.Length == 0 || state != _headerState)
                {
                    _headerState = state;
                    _header = Name + "  [" + state + "]";
                }
                return _header;
            }
        }
    }
}
