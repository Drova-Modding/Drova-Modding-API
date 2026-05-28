using Drova_Modding_API.Access;
using Drova_Modding_API.Extensions;
using Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies;
using Drova_Modding_API.Systems.Dialogues.Editor.Factories;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Drova_Modding_API.Systems.Dialogues.Store;
using Drova_Modding_API.Systems.Editor;
using Il2Cpp;
using Il2CppDrova;
using Il2CppInterop.Runtime.Attributes;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// Class that manages the context menu for the graph editor window
    /// </summary>
    /// <param name="ptr">Do not try to create this object with new()!</param>
    [RegisterTypeInIl2Cpp]
    public class GraphEditorManager(IntPtr ptr) : MonoBehaviour(ptr)
    {
        /// <summary>
        /// List of snapshots of the graph editor state
        /// </summary>
        protected readonly List<GraphEditorSnapshot> GraphEditorSnapshots = [];

        private DialogueTree? _dialogueTree;
        private bool _isActive;
        private IActionStrategy? _activeAction;
        private string? _activeActionHint;

        /// <summary>
        /// The dialogue tree that is being edited
        /// </summary>
        public DialogueTree? DialogueTree
        {
            get
            {
                return _dialogueTree;
            }
            set
            {
                if (value == null)
                {
                    _dialogueTree = value;
                    _nodeActorDropdowns.Clear();
                    InvalidateActorLayoutCache();
                    return;
                }

                _nodeActorDropdowns.Clear();
                InvalidateActorLayoutCache();

                if (value.IsLazyLoading || value.allNodes.Count == 0)
                {
                    value.SelfDeserialize();
                }
                value.DeserializeIfNotDoneYet(true);
                BuildNodeEditorsFromFirstInit(value);
                _dialogueTree = value;
                if (_isActive) return;
                EditorManager.AllowNpcSelection = false;
                InputAccess.ToggleGameplayActionMaps(false);
                Time.timeScale = 0;
                _isActive = true;
            }
        }

        // Currently selected node
        private DrawNodeEditor? _selectedNode;

        // Offset between the mouse position and node position
        private Vector2 _dragOffset;

        /// <summary>
        /// Factory for creating node editors
        /// </summary>
        [HideFromIl2Cpp]
        public DrawNodeEditorFactory DrawNodeEditorFactory { get; set; }

        /// <summary>
        /// Factory for creating task editors
        /// </summary>
        [HideFromIl2Cpp]
        public DrawTaskEditorFactory DrawTaskEditorFactory { get; set; }

        // Track whether the context menu is open
        private bool _showContextMenu;

        // Track whether the sub-context menu is open
        private bool _showSubContextMenu;

        // Position where the right-click occurred
        private Vector2 _contextMenuPosition;

        // Current rect of the context menu
        private Rect _rect;
        // Zoom level
        private float _scaleFactor = 1f;
        // Offset for panning
        private Vector2 _panOffset = new(200, -200);
        // Start point for panning drag
        private Vector2 _dragStart;
        // Is panning in progress?
        private bool _isDragging;
        // Is a node being dragged?
        private bool _isDraggingNode;

        private bool _isFirstDraw = true;

        // List of context menu options
        private readonly LocalizedString[] _contextMenuOptions = [new("Modding_API/GraphEditor", "Create"), new("Modding_API/GraphEditor", "Delete"), new("Modding_API/GraphEditor", "Duplicate"), new("Modding_API/GraphEditor", "Connect")];

        private readonly Dictionary<string, DrawNodeEditor> _drawNodeEditors = [];

        // Editors whose AABB overlaps another editor this frame
        private readonly HashSet<DrawNodeEditor> _overlappingEditors = [];

        // Keep one dropdown per node UID for actor parameter selection.
        private readonly Dictionary<string, GUIDropdown> _nodeActorDropdowns = [];

        private string _newActorParameterName = string.Empty;
        private int _actorNamesLayoutCacheKey;
        private float _cachedActorDropdownWidth = -1f;

        /// <summary>
        /// List of node editors
        /// </summary>
        [HideFromIl2Cpp]
        public Dictionary<string, DrawNodeEditor> DrawNodeEditors => _drawNodeEditors;

        [HideFromIl2Cpp]
        private GraphEditorActionContext CreateActionContext(Vector2 pointerPosition)
        {
            return new GraphEditorActionContext(this, _selectedNode, pointerPosition, _activeActionHint);
        }

        [HideFromIl2Cpp]
        private void ApplyActionContext(GraphEditorActionContext context)
        {
            _activeActionHint = context.HintText;
        }

        /// <summary>
        /// Sets the selected node editor and updates selection visuals.
        /// </summary>
        /// <param name="editor">Editor to select, or null to clear selection.</param>
        [HideFromIl2Cpp]
        public void SetSelectedNode(DrawNodeEditor? editor)
        {
            if (_selectedNode != null)
            {
                _selectedNode.IsSelected = false;
            }

            _selectedNode = editor;

            if (_selectedNode != null)
            {
                _selectedNode.IsSelected = true;
            }
        }

        internal void Awake()
        {
            DrawNodeEditorFactory = new DrawNodeEditorFactory();
            DrawTaskEditorFactory = new DrawTaskEditorFactory();
        }

        internal void Start()
        {
            useGUILayout = false;
        }

        internal void Update()
        {
            // Detect right-click (mouse button 1)
            if (Input.GetMouseButtonDown(1))
            {
                Vector2 guiMousePosition = Input.mousePosition;
                guiMousePosition.y = Screen.height - guiMousePosition.y;

                DrawNodeEditor? nodeUnderCursor = FindNodeAt((guiMousePosition - _panOffset) / _scaleFactor);
                SetSelectedNode(nodeUnderCursor);

                _showContextMenu = true;
                _contextMenuPosition = guiMousePosition;
                // Ensure the menu doesn't go off the screen
                _contextMenuPosition.x = Mathf.Clamp(_contextMenuPosition.x, 0, Screen.width - 10) + 10;
                EndActiveAction();
            }
            if (Input.GetKey(KeyCode.Escape))
            {
                EndActiveAction();
            }
            // Close the context menu when clicking elsewhere
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePosition = Input.mousePosition;
                mousePosition.y = Screen.height - mousePosition.y;
                if (!IsPointInRect(mousePosition, _rect))
                {
                    _showContextMenu = false;
                    _showSubContextMenu = false;
                }
            }
        }

        internal void OnGUI()
        {
            if (DialogueTree == null) return;

            Color prevoiousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.backgroundColor = prevoiousBackgroundColor;

            if (_isFirstDraw)
            {
                for (int i = 0; i < _drawNodeEditors.Count; i++)
                {
                    _drawNodeEditors.Values.ElementAt(i).Init();
                }
                _isFirstDraw = false;
            }

            // Handle user input for zoom and pan
            HandleZoomAndPan();

            // Handle node dragging
            HandleMouseClick();

            // Define the visible screen rect for dynamic rendering
            Rect visibleArea = new(
                -_panOffset.x / _scaleFactor,
                -_panOffset.y / _scaleFactor,
                Screen.width / _scaleFactor,
                Screen.height / _scaleFactor
            );

            // Apply transformations for zoom and pan
            GUI.matrix = Matrix4x4.TRS(_panOffset, Quaternion.identity, new Vector3(1 * _scaleFactor, 1 * _scaleFactor, 1));

            // Draw the background
            if (Event.current.type == EventType.Repaint)
                DrawInfiniteBackground();

            // Detect overlaps using last-frame sizes; highlight pass runs after node draw
            DetectOverlaps();

            // Draw only nodes and connections in the visible area
            DrawConnectionsInArea(visibleArea);
            DrawNodesInArea(visibleArea);
            DrawOverlapHighlights(visibleArea);
            HandleActiveActionStrategy();

            // Flush all deferred dropdown overlays so they paint on top of every node
            GUIDropdown.FlushOverlays();

            // Draw the tooltip
            if (GUI.tooltip != "")
            {
                GUIStyle style = new(GUI.skin.label)
                {
                    normal =
                    {
                        textColor = Color.white
                    },
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };

                float width = style.CalcSize(new GUIContent(GUI.tooltip)).x;
                float height = style.CalcHeight(new GUIContent(GUI.tooltip), width);
                GUI.Label(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - 50, width, height), GUI.tooltip, style);
            }

            // Reset transformations
            GUI.matrix = Matrix4x4.identity;

            // Draw context menus if needed
            if (_showContextMenu) DrawContextMenu();
            if (_showSubContextMenu) DrawSubContextMenu();

            DrawActiveActionHint();
            DrawControls();
        }

        private void EndActiveAction()
        {
            if (_activeAction != null)
            {
                GraphEditorActionContext context = CreateActionContext(Vector2.zero);
                _activeAction.OnCancel(context);
                ApplyActionContext(context);
                _activeAction = null;
            }
        }

        /// <summary>
        /// Initialize the graph editor with the actor
        /// </summary>
        /// <param name="actor">Actor to initalize</param>
        public void Init(Actor actor)
        {
            if (actor == null) return;
            DS_DialogueTreeController dialogueTreeController = actor.GetComponentInChildren<DS_DialogueTreeController>();
            if (dialogueTreeController == null) return;
            if (dialogueTreeController.graph == null) return;
            DialogueTree = dialogueTreeController.graph.Cast<DialogueTree>();
        }

        [HideFromIl2Cpp]
        static bool IsPointInRect(Vector2 point, Rect rect)
        {
            if (rect == default) return false;
            return rect.Contains(point);
        }

        [HideFromIl2Cpp]
        private DrawNodeEditor? FindNodeAt(Vector2 graphMousePosition)
        {
            foreach (DrawNodeEditor editor in _drawNodeEditors.Values)
            {
                if (editor == null) continue;
                Rect nodeRect = new(editor.Position, editor.NodeSize);
                if (nodeRect.Contains(graphMousePosition))
                {
                    return editor;
                }
            }

            return null;
        }

        [HideFromIl2Cpp]
        private void BuildNodeEditorsFromFirstInit(DialogueTree value)
        {
            Vector2 lastPosition = new(100, 0);
            for (int i = 0; i < value.allNodes.Count; i++)
            {
                Il2CppNodeCanvas.Framework.Node node = value.allNodes[i];
                if (node == null) continue;
                if (!_drawNodeEditors.ContainsKey(node.UID))
                {
                    DrawNodeEditor editor = DrawNodeEditorFactory.GetDrawNodeEditorFromType(node.GetIl2CppType());
                    if (editor == null)
                    {
                        MelonLogger.Warning("Editor is null for {0}", node.GetIl2CppType().Name);
                        continue;
                    }
                    editor.Node = node.Cast<DTNode>();
                    _drawNodeEditors.TryAdd(node.UID, editor);
                    Vector2 newPosition = new(lastPosition.x, lastPosition.y + editor.NodeSize.y);
                    editor.Position = newPosition;
                    editor.GraphEditorManager = this;

                    Il2CppSystem.Collections.Generic.List<Il2CppNodeCanvas.Framework.Connection> connections = node.outConnections;
                    int highestHeight = CreateOutConnections(editor, newPosition, connections);
                    lastPosition = new Vector2(newPosition.x, newPosition.y + highestHeight + 100);
                }
                else if (_drawNodeEditors.TryGetValue(node.UID, out DrawNodeEditor editor))
                {
                    Vector2 newPosition = new(lastPosition.x, lastPosition.y + editor.NodeSize.y);
                    int highestHeight = CreateOutConnections(editor, newPosition, node.outConnections);
                    lastPosition = new Vector2(newPosition.x, newPosition.y + highestHeight + 100);
                }
            }
        }

        [HideFromIl2Cpp]
        private int CreateOutConnections(DrawNodeEditor editor, Vector2 newPosition, Il2CppSystem.Collections.Generic.List<Il2CppNodeCanvas.Framework.Connection> connections)
        {
            Vector2 connectionPosition = new(newPosition.x, newPosition.y + 100);
            int highestHeight = 0;
            for (int j = 0; j < connections.Count; j++)
            {
                Il2CppNodeCanvas.Framework.Connection connection = connections[j];
                if (connection == null) continue;
                if (connection.targetNode == null) continue;
                Il2CppNodeCanvas.Framework.Node targetNode = connection.targetNode;
                if (_drawNodeEditors.TryGetValue(targetNode.UID, out DrawNodeEditor targetConnection))
                {
                    editor.ConnectedWith.Add(targetConnection);
                }
                else
                {
                    DrawNodeEditor connectedEditor = DrawNodeEditorFactory.GetDrawNodeEditorFromType(targetNode.GetIl2CppType());
                    if (connectedEditor == null)
                    {
                        MelonLogger.Warning("Editor is null for {0}", targetNode.GetIl2CppType().Name);
                        continue;
                    }
                    connectedEditor.Node = targetNode.Cast<DTNode>();
                    connectedEditor.GraphEditorManager = this;
                    _drawNodeEditors.TryAdd(targetNode.UID, connectedEditor);
                    connectedEditor.Position = connectionPosition;
                    connectionPosition = new Vector2(connectionPosition.x + connectedEditor.NodeSize.x + 100, connectionPosition.y);
                    editor.ConnectedWith.Add(connectedEditor);
                    if (connectedEditor.NodeSize.y > highestHeight)
                    {
                        highestHeight = (int)connectedEditor.NodeSize.y;
                    }
                }
            }
            return highestHeight;
        }

        /// <summary>
        /// Go into a subgraph of a node
        /// </summary>
        /// <param name="node">The node to go into</param>
        public void GoIntoSubGraph(SubDialogueTree node)
        {
            GraphEditorSnapshots.Add(new GraphEditorSnapshot(DialogueTree!, _scaleFactor, _panOffset));
            _drawNodeEditors.Clear();
            _selectedNode = null;
            _isDraggingNode = false;
            _scaleFactor = 1;
            _panOffset = new Vector2(200, -200);
            _isFirstDraw = true;
            DialogueTree = node.subGraph;
        }

        #region Drawing

        [HideFromIl2Cpp]
        private void DrawControls()
        {
            if (GUI.Button(new(Screen.width - 110, 10, 100, 60), "Close Graph"))
            {
                CloseGraph();
                return;
            }
            if (GUI.Button(new(Screen.width - 110, 70, 100, 60), "Save Graph"))
            {
                DialogueTree!.Serialize(null);
                string dialogueId = string.IsNullOrWhiteSpace(DialogueTree.Key) ? DialogueTree.name : DialogueTree.Key;
                DialogueStore.SaveDialogue(dialogueId, DialogueTree);
            }
            if (GUI.Button(new(Screen.width - 160, 130, 150, 60), "Resolve Overlaps"))
            {
                ResolveOverlaps();
            }
            DrawActorParameterControls();
            if (GraphEditorSnapshots.Count > 0)
            {
                GraphEditorSnapshot snapshot = GraphEditorSnapshots[^1];
                if (GUI.Button(new(10, 10, 550, 60), $"Go back to {snapshot.DialogueTree.name}"))
                {
                    _drawNodeEditors.Clear();
                    _selectedNode = null;
                    _isDraggingNode = false;
                    _isFirstDraw = true;
                    _scaleFactor = snapshot.ScaleFactor;
                    _panOffset = snapshot.PanOffset;
                    DialogueTree = snapshot.DialogueTree;
                    GraphEditorSnapshots.RemoveAt(GraphEditorSnapshots.Count - 1);
                }
            }
            GUI.Label(new(10, 80, 550, 25), $"Current Editor: {DialogueTree!.name}");
            string selectedNodeLabel = _selectedNode == null
                ? "Selected Node: none"
                : $"Selected Node: {_selectedNode.Node.GetIl2CppType().Name}";
            GUI.Label(new(10, 105, 550, 25), selectedNodeLabel);
        }

        [HideFromIl2Cpp]
        private void DrawActorParameterControls()
        {
            if (DialogueTree == null) return;

            GUI.Box(new Rect(10, 130, 360, 68), "Actor Parameters");
            GUI.Label(new Rect(20, 152, 90, 20), "Name");
            _newActorParameterName = GUI.TextField(new Rect(70, 152, 200, 20), _newActorParameterName);
            if (GUI.Button(new Rect(280, 152, 80, 20), "Add"))
            {
                AddActorParameter(_newActorParameterName);
            }

            if (DialogueTree.actorParameters != null && DialogueTree.actorParameters.Count > 0)
            {
                GUI.Label(new Rect(20, 174, 330, 20), $"Default actor: {GetActorParameterName(DialogueTree.actorParameters[0])}");
            }
            else
            {
                GUI.Label(new Rect(20, 174, 330, 20), "Default actor: none");
            }
        }

        [HideFromIl2Cpp]
        private void AddActorParameter(string actorName)
        {
            if (DialogueTree == null) return;

            string trimmedName = actorName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedName)) return;

            DialogueTree.actorParameters ??= new Il2CppSystem.Collections.Generic.List<DialogueTree.ActorParameter>();
            DialogueTree.actorParameters.Add(new DialogueTree.ActorParameter
            {
                _keyName = trimmedName,
                Actor = null,
                ActorGuid = string.Empty,
                _id = Il2CppSystem.Guid.NewGuid().ToString()
            });

            _newActorParameterName = string.Empty;
            _nodeActorDropdowns.Clear();
            InvalidateActorLayoutCache();
        }

        [HideFromIl2Cpp]
        private void CloseGraph()
        {
            _isActive = false;
            DialogueTree = null;
            _selectedNode = null;
            _isDraggingNode = false;
            _showContextMenu = false;
            _showSubContextMenu = false;
            _drawNodeEditors.Clear();
            EditorManager.AllowNpcSelection = true;
            EditorManager.InEditor = false;
            _scaleFactor = 1;
            _panOffset = new(200, -200);
            _isFirstDraw = true;
            _nodeActorDropdowns.Clear();
            InvalidateActorLayoutCache();
            EditorManager.ResetLastInvoked();
            _activeActionHint = null;
            // Restore the previous timescale if the game is paused which is the case when the graph is opened
            if (Time.timeScale == 0)
                Time.timeScale = 1;
            InputAccess.ToggleGameplayActionMaps(true);
            gameObject.SetActive(false);
        }

        [HideFromIl2Cpp]
        private void HandleZoomAndPan()
        {
            Event e = Event.current;

            // Zoom with mouse scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = e.delta.y * -0.01f;
                //float oldScaleFactor = _scaleFactor;
                _scaleFactor = Mathf.Clamp(_scaleFactor + zoomDelta, 0.1f, 5.0f); // Allow more extreme zoom levels

                e.Use();
            }

            // Pan with middle mouse button
            if (e.type == EventType.MouseDown && e.button == 2)
            {
                _isDragging = true;
                _dragStart = e.mousePosition - _panOffset;
                e.Use();
            }
            if (e.type == EventType.MouseDrag && _isDragging)
            {
                _panOffset = e.mousePosition - _dragStart;
                e.Use();
            }
            if (e.type == EventType.MouseUp && e.button == 2)
            {
                _isDragging = false;
            }
        }

        /// <summary>
        /// Handles mouse clicks for selecting and dragging nodes
        /// </summary>
        [HideFromIl2Cpp]
        void HandleMouseClick()
        {
            Event e = Event.current;

            // Ignore graph interactions while clicking inside an open context menu.
            // Without this guard, the menu click can deselect the node before the action executes.
            if ((_showContextMenu || _showSubContextMenu) && IsPointInRect(e.mousePosition, _rect))
            {
                return;
            }

            Vector2 adjustedMousePosition = (e.mousePosition - _panOffset) / _scaleFactor;

            if (_activeAction != null && e.type == EventType.MouseDown && e.button == 0)
            {
                GraphEditorActionContext context = CreateActionContext(adjustedMousePosition);
                _activeAction.OnEnd(context);
                ApplyActionContext(context);
                _activeAction = null;
                e.Use();
                return;
            }

            // While an active action is running, block all normal node interaction
            // so the source node is not accidentally dragged or deselected.
            if (_activeAction != null) return;

            // On mouse down, check if we clicked on a node
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                bool isDoubleClick = e.clickCount == 2;
                bool nodeHit = false;

                foreach (KeyValuePair<string, DrawNodeEditor> element in _drawNodeEditors)
                {
                    DrawNodeEditor editor = element.Value;
                    Rect nodeRect = new(editor.Position, editor.NodeSize);

                    if (nodeRect.Contains(adjustedMousePosition))
                    {
                        nodeHit = true;
                        if (isDoubleClick)
                        {
                            editor.OnDoubleClick(adjustedMousePosition);
                            e.Use();
                        }
                        else
                        {
                            if (_selectedNode != null && _selectedNode != editor)
                            {
                                _selectedNode.IsSelected = false;
                            }
                            _selectedNode = editor;
                            editor.IsSelected = true;
                            _dragOffset = adjustedMousePosition - editor.Position;
                            _isDraggingNode = true;
                        }
                        break;
                    }
                }

                if (!nodeHit && !isDoubleClick)
                {
                    if (_selectedNode != null)
                    {
                        _selectedNode.IsSelected = false;
                        _selectedNode = null;
                    }
                }
            }

            // On mouse drag, update the node's position
            if (e.type == EventType.MouseDrag && _isDraggingNode && _selectedNode != null)
            {
                _selectedNode.Position = adjustedMousePosition - _dragOffset;
                e.Use();
            }

            // On mouse up, stop dragging
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isDraggingNode = false;
            }
        }

        [HideFromIl2Cpp]
        private void DrawInfiniteBackground()
        {
            float gridSize = 50;
            Color gridColor = new(0.7f, 0.7f, 0.7f, 0.5f); // semi-transparent gray

            // GUIExtensions.DrawLine uses RotateAroundPivot, which expects the pivot
            // in screen space. The caller's pan/scale matrix would corrupt vertical
            // lines, so draw the grid in screen space with an identity matrix.
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;

            // Visible world rect derived from current pan and zoom
            float worldLeft = -_panOffset.x / _scaleFactor;
            float worldTop = -_panOffset.y / _scaleFactor;
            float worldRight = worldLeft + (Screen.width / _scaleFactor);
            float worldBottom = worldTop + (Screen.height / _scaleFactor);

            float startWorldX = Mathf.Floor(worldLeft / gridSize) * gridSize;
            float startWorldY = Mathf.Floor(worldTop / gridSize) * gridSize;

            int verticalLinesCount = Mathf.CeilToInt((worldRight - startWorldX) / gridSize) + 1;
            int horizontalLinesCount = Mathf.CeilToInt((worldBottom - startWorldY) / gridSize) + 1;

            // Vertical grid lines, drawn fully across the screen height
            for (int i = 0; i < verticalLinesCount; i++)
            {
                float worldX = startWorldX + (i * gridSize);
                float screenX = (worldX * _scaleFactor) + _panOffset.x;
                GUIExtensions.DrawLine(new Vector2(screenX, 0), new Vector2(screenX, Screen.height), gridColor);
            }

            // Horizontal grid lines, drawn fully across the screen width
            for (int i = 0; i < horizontalLinesCount; i++)
            {
                float worldY = startWorldY + (i * gridSize);
                float screenY = (worldY * _scaleFactor) + _panOffset.y;
                GUIExtensions.DrawLine(new Vector2(0, screenY), new Vector2(Screen.width, screenY), gridColor);
            }

            GUI.matrix = previousMatrix;
        }

        [HideFromIl2Cpp]
        private void DetectOverlaps()
        {
            _overlappingEditors.Clear();
            List<DrawNodeEditor> values = [.. _drawNodeEditors.Values];
            for (int i = 0; i < values.Count; i++)
            {
                DrawNodeEditor a = values[i];
                if (a == null || a.NodeSize.x <= 0 || a.NodeSize.y <= 0) continue;
                Rect rectA = new(a.Position, a.NodeSize);
                for (int j = i + 1; j < values.Count; j++)
                {
                    DrawNodeEditor b = values[j];
                    if (b == null || b.NodeSize.x <= 0 || b.NodeSize.y <= 0) continue;
                    Rect rectB = new(b.Position, b.NodeSize);
                    if (rectA.Overlaps(rectB))
                    {
                        _overlappingEditors.Add(a);
                        _overlappingEditors.Add(b);
                    }
                }
            }
        }

        [HideFromIl2Cpp]
        private void DrawOverlapHighlights(Rect visibleArea)
        {
            if (_overlappingEditors.Count == 0) return;

            Color previousColor = GUI.color;
            Color previousBg = GUI.backgroundColor;
            GUI.color = new Color(1f, 0.25f, 0.25f, 1f);

            const float thickness = 2f;
            foreach (DrawNodeEditor editor in _overlappingEditors)
            {
                if (editor == null) continue;
                Rect nodeRect = new(editor.Position, editor.NodeSize);
                if (!visibleArea.Overlaps(nodeRect)) continue;

                float x = editor.Position.x;
                float y = editor.Position.y;
                float w = editor.NodeSize.x;
                float h = editor.NodeSize.y;
                GUI.Box(new Rect(x, y, w, thickness), GUIContent.none);
                GUI.Box(new Rect(x, y + h - thickness, w, thickness), GUIContent.none);
                GUI.Box(new Rect(x, y, thickness, h), GUIContent.none);
                GUI.Box(new Rect(x + w - thickness, y, thickness, h), GUIContent.none);
            }

            GUI.color = previousColor;
            GUI.backgroundColor = previousBg;
        }

        [HideFromIl2Cpp]
        private void ResolveOverlaps()
        {
            const int maxIterations = 50;
            const float padding = 20f;
            List<DrawNodeEditor> values = [.. _drawNodeEditors.Values];

            for (int iter = 0; iter < maxIterations; iter++)
            {
                bool anyOverlap = false;
                for (int i = 0; i < values.Count; i++)
                {
                    DrawNodeEditor a = values[i];
                    if (a == null || a.NodeSize.x <= 0 || a.NodeSize.y <= 0) continue;
                    for (int j = i + 1; j < values.Count; j++)
                    {
                        DrawNodeEditor b = values[j];
                        if (b == null || b.NodeSize.x <= 0 || b.NodeSize.y <= 0) continue;
                        Rect rectA = new(a.Position, a.NodeSize);
                        Rect rectB = new(b.Position, b.NodeSize);
                        if (!rectA.Overlaps(rectB)) continue;

                        anyOverlap = true;

                        float overlapX = Mathf.Min(rectA.xMax, rectB.xMax) - Mathf.Max(rectA.xMin, rectB.xMin);
                        float overlapY = Mathf.Min(rectA.yMax, rectB.yMax) - Mathf.Max(rectA.yMin, rectB.yMin);

                        // Push along the smaller-overlap axis; the lower/right node moves
                        if (overlapX < overlapY)
                        {
                            float push = overlapX + padding;
                            if (a.Position.x <= b.Position.x)
                                b.Position = new Vector2(b.Position.x + push, b.Position.y);
                            else
                                a.Position = new Vector2(a.Position.x + push, a.Position.y);
                        }
                        else
                        {
                            float push = overlapY + padding;
                            if (a.Position.y <= b.Position.y)
                                b.Position = new Vector2(b.Position.x, b.Position.y + push);
                            else
                                a.Position = new Vector2(a.Position.x, a.Position.y + push);
                        }
                    }
                }
                if (!anyOverlap) break;
            }
        }

        [HideFromIl2Cpp]
        private void DrawNodesInArea(Rect visibleArea)
        {
            HashSet<string> activeUids = [];
            for (int i = 0; i < _drawNodeEditors.Values.Count; i++)
            {
                DrawNodeEditor element = _drawNodeEditors.Values.ElementAt(i);
                if (element == null) continue;
                activeUids.Add(element.Node.UID);

                Rect nodeRect = new(
                    element.Position.x,
                    element.Position.y,
                    element.NodeSize.x,
                    element.NodeSize.y
                );

                if (visibleArea.Overlaps(nodeRect))
                {
                    element.DrawNode(element.Position);
                    DrawNodeActorSelector(element);
                }
            }

            // Remove stale dropdowns when nodes were deleted.
            List<string> staleUids = [];
            foreach (string uid in _nodeActorDropdowns.Keys)
            {
                if (!activeUids.Contains(uid))
                    staleUids.Add(uid);
            }
            for (int i = 0; i < staleUids.Count; i++)
            {
                _nodeActorDropdowns.Remove(staleUids[i]);
            }
        }

        [HideFromIl2Cpp]
        private void DrawNodeActorSelector(DrawNodeEditor editor)
        {
            if (DialogueTree == null || editor.Node == null) return;

            DTNode node = editor.Node;
            // Multiple choice is always player-driven and should not expose actor selection.
            if (node is DS_MultipleChoiceNode || !node.requireActorSelection) return;
            if (!TryGetActorOptions(out string[] actorNames, out string[] actorIds)) return;

            EnsureDefaultActorBinding(node, actorNames, actorIds);

            string currentActorId = node._actorParameterID ?? string.Empty;
            int selectedIndex = Array.IndexOf(actorIds, currentActorId);
            if (selectedIndex < 0) selectedIndex = 0;

            if (!_nodeActorDropdowns.TryGetValue(editor.Node.UID, out GUIDropdown actorDropdown)
                || actorDropdown.OptionsCount != actorNames.Length)
            {
                actorDropdown = new GUIDropdown(actorNames, selectedIndex);
                _nodeActorDropdowns[editor.Node.UID] = actorDropdown;
            }
            else
            {
                actorDropdown.SetSelectedIndex(selectedIndex);
            }

            // Render actor selector outside of the node header and size it to long actor names.
            float dropdownWidth = GetCachedActorDropdownWidth(actorNames);
            float panelWidth = dropdownWidth + 50f;
            float panelX = editor.Position.x + editor.NodeSize.x + 8f;
            float panelY = editor.Position.y;
            Rect panelRect = new(panelX, panelY, panelWidth, 22f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.Label(new Rect(panelX + 4f, panelY + 2f, 40f, 18f), "Actor");
            Rect dropdownRect = new(panelX + 46f, panelY + 2f, dropdownWidth, 18f);

            if (actorDropdown.Draw(dropdownRect))
            {
                int newIndex = actorDropdown.SelectedIndex;
                node._actorParameterID = actorIds[newIndex];
                node.actorName = actorNames[newIndex];
            }
        }

        [HideFromIl2Cpp]
        private float GetCachedActorDropdownWidth(string[] actorNames)
        {
            int layoutKey = ComputeActorNamesLayoutKey(actorNames);
            if (layoutKey == _actorNamesLayoutCacheKey && _cachedActorDropdownWidth > 0f)
            {
                return _cachedActorDropdownWidth;
            }

            float widestActorText = 0f;
            GUIStyle buttonStyle = GUI.skin.button;
            for (int i = 0; i < actorNames.Length; i++)
            {
                float candidateWidth = buttonStyle.CalcSize(new GUIContent(actorNames[i])).x;
                if (candidateWidth > widestActorText)
                {
                    widestActorText = candidateWidth;
                }
            }

            _cachedActorDropdownWidth = Mathf.Clamp(widestActorText + 28f, 170f, 420f);
            _actorNamesLayoutCacheKey = layoutKey;
            return _cachedActorDropdownWidth;
        }

        [HideFromIl2Cpp]
        private static int ComputeActorNamesLayoutKey(string[] actorNames)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + actorNames.Length;
                for (int i = 0; i < actorNames.Length; i++)
                {
                    hash = (hash * 31) + actorNames[i].GetHashCode();
                }

                return hash;
            }
        }

        [HideFromIl2Cpp]
        private void InvalidateActorLayoutCache()
        {
            _actorNamesLayoutCacheKey = 0;
            _cachedActorDropdownWidth = -1f;
        }

        [HideFromIl2Cpp]
        internal void ApplyDefaultActorParameter(DTNode node)
        {
            if (DialogueTree == null || node == null) return;
            if (node is DS_MultipleChoiceNode || !node.requireActorSelection) return;
            if (!TryGetActorOptions(out string[] actorNames, out string[] actorIds)) return;

            EnsureDefaultActorBinding(node, actorNames, actorIds);
        }

        [HideFromIl2Cpp]
        private static void EnsureDefaultActorBinding(DTNode node, string[] actorNames, string[] actorIds)
        {
            string currentId = node._actorParameterID ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(currentId) && Array.IndexOf(actorIds, currentId) >= 0)
            {
                return;
            }

            node.actorName = actorNames[0];
            node._actorParameterID = actorIds[0];
        }

        [HideFromIl2Cpp]
        private bool TryGetActorOptions(out string[] actorNames, out string[] actorIds)
        {
            actorNames = [];
            actorIds = [];
            if (DialogueTree?.actorParameters == null || DialogueTree.actorParameters.Count == 0)
            {
                return false;
            }

            actorNames = new string[DialogueTree.actorParameters.Count];
            actorIds = new string[DialogueTree.actorParameters.Count];
            for (int i = 0; i < DialogueTree.actorParameters.Count; i++)
            {
                DialogueTree.ActorParameter actorParameter = DialogueTree.actorParameters[i];
                actorNames[i] = GetActorParameterName(actorParameter);
                actorIds[i] = GetActorParameterId(actorParameter);
            }

            return actorNames.Length > 0;
        }

        [HideFromIl2Cpp]
        private static string GetActorParameterName(DialogueTree.ActorParameter actorParameter)
        {
            if (!string.IsNullOrWhiteSpace(actorParameter.name))
            {
                return actorParameter.name;
            }

            return actorParameter._keyName ?? string.Empty;
        }

        [HideFromIl2Cpp]
        private static string GetActorParameterId(DialogueTree.ActorParameter actorParameter)
        {
            if (!string.IsNullOrWhiteSpace(actorParameter.ID))
            {
                return actorParameter.ID;
            }

            return actorParameter._id ?? string.Empty;
        }

        private void HandleActiveActionStrategy()
        {
            if (_activeAction != null)
            {
                Event e = Event.current;
                Vector2 adjustedMousePosition = (e.mousePosition - _panOffset) / _scaleFactor;
                GraphEditorActionContext context = CreateActionContext(adjustedMousePosition);
                _activeAction.OnGui(context);
                ApplyActionContext(context);
            }
        }

        [HideFromIl2Cpp]
        private void DrawActiveActionHint()
        {
            if (string.IsNullOrWhiteSpace(_activeActionHint)) return;

            Rect hintRect = new(10, Screen.height - 40, Screen.width - 20, 30);
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Box(hintRect, GUIContent.none);

            GUI.color = Color.white;
            GUI.Label(new Rect(hintRect.x + 8, hintRect.y + 6, hintRect.width - 16, 20), _activeActionHint);
            GUI.color = previousColor;
        }

        #endregion Drawing

        #region ContextMenu

        [HideFromIl2Cpp]
        void DrawContextMenu()
        {
            float menuWidth = 120;

            DrawContextMenu(_contextMenuOptions.Length, menuWidth);
            bool hasSelection = _selectedNode != null;

            for (int i = 0; i < _contextMenuOptions.Length; i++)
            {
                bool needsSelection = ContextNeedsSelection(i);
                bool isEnabled = !needsSelection || hasSelection;
                bool previousEnabled = GUI.enabled;
                GUI.enabled = isEnabled;

                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + (i * 20), menuWidth, 20), _contextMenuOptions[i].GetLocalizedString(null)))
                {
                    EndActiveAction();
                    HandleContextMenuSelection(i);
                    _showContextMenu = false;
                }

                GUI.enabled = previousEnabled;
            }
        }

        private static bool ContextNeedsSelection(int index)
        {
            return index == (int)GraphEditorAction.Delete || index == (int)GraphEditorAction.Duplicate || index == (int)GraphEditorAction.Connect;
        }

        [HideFromIl2Cpp]
        void DrawContextMenu(int length, float menuWidth)
        {
            float menuHeight = 20 * length;
            _rect = new Rect(_contextMenuPosition.x, _contextMenuPosition.y, menuWidth, menuHeight);

            // Draw the background box
            GUI.Box(_rect, "");
        }

        [HideFromIl2Cpp]
        void DrawSubContextMenu()
        {
            float menuWidth = 200;
            string[] subContextMenuOptions = DrawNodeEditorFactory.GetNodeNames();

            DrawContextMenu(subContextMenuOptions.Length, menuWidth);

            // Display each option as a button
            for (int i = 0; i < subContextMenuOptions.Length; i++)
            {
                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + (i * 20), menuWidth, 20), subContextMenuOptions[i]))
                {
                    HandleSubContextMenuSelection(subContextMenuOptions[i]);
                    _showSubContextMenu = false; // Close the menu after selection
                }
            }
        }

        [HideFromIl2Cpp]
        void HandleSubContextMenuSelection(string option)
        {
            IActionStrategy action = new CreateNodeActionStrategy(option);
            GraphEditorActionContext context = CreateActionContext((_contextMenuPosition - _panOffset) / _scaleFactor);
            action.OnStart(context);
            ApplyActionContext(context);
        }

        [HideFromIl2Cpp]
        void HandleContextMenuSelection(int option)
        {
            switch (option)
            {
                case (int)GraphEditorAction.Create:
                    _showSubContextMenu = true;
                    break;
                case (int)GraphEditorAction.Delete:
                {
                    GraphEditorActionContext context = CreateActionContext((_contextMenuPosition - _panOffset) / _scaleFactor);
                    new DeleteNodeActionStrategy().OnStart(context);
                    ApplyActionContext(context);
                    break;
                }
                case (int)GraphEditorAction.Duplicate:
                {
                    GraphEditorActionContext context = CreateActionContext((_contextMenuPosition - _panOffset) / _scaleFactor);
                    new DuplicateNodeActionStrategy().OnStart(context);
                    ApplyActionContext(context);
                    break;
                }
                case (int)GraphEditorAction.Connect:
                    if (_selectedNode == null) return;
                    _activeAction = new ConnectActionStrategy();
                    _isDraggingNode = false;
                    {
                        GraphEditorActionContext context = CreateActionContext((_contextMenuPosition - _panOffset) / _scaleFactor);
                        _activeAction.OnStart(context);
                        ApplyActionContext(context);
                    }
                    break;
            }
        }

        enum GraphEditorAction
        {
            Create,
            Delete,
            Duplicate,
            Connect
        }
        #endregion ContextMenu

        #region LineDrawing

        [HideFromIl2Cpp]
        private void DrawConnectionsInArea(Rect visibleArea)
        {
            int previousDepth = GUI.depth;
            GUI.depth = 50;
            foreach (KeyValuePair<string, DrawNodeEditor> element in _drawNodeEditors)
            {
                DrawNodeEditor editor = element.Value;
                if (editor == null || editor.ConnectedWith.Count == 0) continue;

                for (int i = 0; i < editor.ConnectedWith.Count; i++)
                {
                    DrawNodeEditor connectedEditor = editor.ConnectedWith[i];
                    if (connectedEditor == null) continue;

                    // Get the transformed positions of the nodes
                    Vector2 editorCenter = editor.Position + (editor.NodeSize / 2);
                    Vector2 connectedCenter = connectedEditor.Position + (connectedEditor.NodeSize / 2);

                    Vector2 editorEdge = GetEdgePoint(editorCenter, editor.NodeSize, connectedCenter);
                    Vector2 connectedEdge = GetEdgePoint(connectedCenter, connectedEditor.NodeSize, editorCenter);

                    Rect overlap = new(editorEdge, connectedEdge - editorEdge);

                    // Draw the line if either point is in the visible area
                    if (visibleArea.Contains(editorCenter) || visibleArea.Contains(connectedCenter) || visibleArea.Overlaps(overlap))
                    {
                        DrawLine(editorEdge, connectedEdge, Color.white, i + 1);
                    }
                }
            }
            GUI.depth = previousDepth;
        }

        /// <summary>
        /// Returns the point where the edge of the node intersects with the line to the target.
        /// </summary>
        /// <param name="nodeCenter">Center of the node</param>
        /// <param name="nodeSize">Size of the node</param>
        /// <param name="target">Node where the line will be drawn</param>
        /// <returns>Edge point</returns>
        [HideFromIl2Cpp]
        public static Vector2 GetEdgePoint(Vector2 nodeCenter, Vector2 nodeSize, Vector2 target)
        {
            Vector2 direction = target - nodeCenter;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return nodeCenter;
            }

            direction.Normalize();

            Vector2 halfSize = nodeSize / 2;

            float scaleX = Mathf.Abs(direction.x) > 0.0001f ? halfSize.x / Mathf.Abs(direction.x) : float.MaxValue;
            float scaleY = Mathf.Abs(direction.y) > 0.0001f ? halfSize.y / Mathf.Abs(direction.y) : float.MaxValue;

            float scale = Mathf.Min(scaleX, scaleY);

            return nodeCenter + (direction * scale);
        }

        [HideFromIl2Cpp]
        internal Vector2 GraphToScreen(Vector2 graphPosition)
        {
            return (graphPosition * _scaleFactor) + _panOffset;
        }

        [HideFromIl2Cpp]
        internal Vector2 ScreenToGraph(Vector2 screenPosition)
        {
            return (screenPosition - _panOffset) / _scaleFactor;
        }

        /// <summary>
        /// Draws a line between two points in the GUI.
        /// </summary>
        /// <param name="pointA">Line a</param>
        /// <param name="pointB">Line b</param>
        /// <param name="color">Which color it should have</param>
        /// <param name="index">Index of the line</param>
        [HideFromIl2Cpp]
        public void DrawLine(Vector2 pointA, Vector2 pointB, Color color, int index)
        {
            Color previousColor = GUI.color;
            GUI.color = color;

            Vector2 delta = pointB - pointA;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            Matrix4x4 matrixBackup = GUI.matrix;

            GUI.matrix = Matrix4x4.TRS((pointA * _scaleFactor) + _panOffset, Quaternion.Euler(0f, 0f, angle), new Vector3(1 * _scaleFactor, 1 * _scaleFactor, 1));

            GUI.Box(new Rect(0, 0, length, 2), "");

            // Draw a box with the index in the middle of the line
            Vector2 midPoint = (pointA + pointB) / 2;
            GUI.matrix = Matrix4x4.TRS((midPoint * _scaleFactor) + _panOffset, Quaternion.identity, new Vector3(1 * _scaleFactor, 1 * _scaleFactor, 1));
            GUI.color = Color.yellow;
            GUI.Box(new Rect(-10, -10, 20, 20), index.ToString());

            GUI.matrix = matrixBackup;
            GUI.color = previousColor;
        }

        [HideFromIl2Cpp]
        internal static void DrawLineScreenSpace(Vector2 pointA, Vector2 pointB, Color color, float thickness = 2f)
        {
            Color previousColor = GUI.color;
            Matrix4x4 matrixBackup = GUI.matrix;
            GUI.color = color;

            Vector2 delta = pointB - pointA;
            float length = delta.magnitude;
            if (length < 0.001f)
            {
                GUI.matrix = matrixBackup;
                GUI.color = previousColor;
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.matrix = Matrix4x4.TRS(pointA, Quaternion.Euler(0f, 0f, angle), Vector3.one);
            GUI.DrawTexture(new Rect(0f, -thickness * 0.5f, length, thickness), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);

            GUI.matrix = matrixBackup;
            GUI.color = previousColor;
        }

        #endregion LineDrawing
    }
}
