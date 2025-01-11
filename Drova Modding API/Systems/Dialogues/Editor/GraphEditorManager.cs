using Il2Cpp;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;
using Drova_Modding_API.Extensions;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Factories;
using Il2CppInterop.Runtime.Attributes;
using Drova_Modding_API.Systems.Editor;

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
        protected List<GraphEditorSnapshot> GraphEditorSnapshots = [];


        private DialogueTree _dialogueTree;

        /// <summary>
        /// The dialogue tree that is being edited
        /// </summary>
        public DialogueTree DialogueTree
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
                    return;
                }
                if (value.IsLazyLoading || value.allNodes.Count == 0)
                {
                    value.SelfDeserialize();
                }
                value.DeserializeIfNotDoneYet(true);
                BuildNodeEditorsFromFirstInit(value);
                _dialogueTree = value;
                EditorManager.AllowNpcSelection = false;
                InputAccess.ToggleGampeplayActionMaps(false);
                Time.timeScale = 0;
            }
        }

        // Currently selected node for dragging
        private DrawNodeEditor _selectedNode = null;

        // Offset between the mouse position and node position
        private Vector2 _dragOffset;

        /// <summary>
        /// Factory for creating node editors
        /// </summary>
        public DrawNodeEditorFactory DrawNodeEditorFactory { get; set; }

        /// <summary>
        /// Factory for creating task editors
        /// </summary>
        public DrawTaskEditorFactory DrawTaskEditorFactory { get; set; }

        // Track whether the context menu is open
        private bool _showContextMenu = false;

        // Track whether the sub-context menu is open
        private bool _showSubContextMenu = false;

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
        private bool _isDragging = false;

        private bool _isFirstDraw = true;

        // List of context menu options
        private readonly LocalizedString[] _contextMenuOptions = [new("Modding_API/GraphEditor", "Create"), new("Modding_API/GraphEditor", "Delete"), new("Modding_API/GraphEditor", "Duplicate")];

        private readonly Dictionary<string, DrawNodeEditor> drawNodeEditors = [];

        internal void Awake()
        {
            DrawNodeEditorFactory = new DrawNodeEditorFactory();
            DrawTaskEditorFactory = new DrawTaskEditorFactory();
        }

        internal void Start()
        {
            useGUILayout = false;
        }

        /// <summary>
        /// Initialize the graph editor with the actor
        /// </summary>
        /// <param name="actor">Actor to initalize</param>
        public void Init(Actor actor)
        {
            if (actor == null) return;
            var dialogueTreeController = actor.GetComponentInChildren<DS_DialogueTreeController>();
            if (dialogueTreeController == null) return;
            if (dialogueTreeController.graph == null) return;
            DialogueTree = dialogueTreeController.graph.Cast<DialogueTree>();
        }

        internal void Update()
        {
            // Detect right-click (mouse button 1)
            if (Input.GetMouseButtonDown(1))
            {
                _showContextMenu = true;
                _contextMenuPosition = Input.mousePosition;
                // Convert to GUI coordinates (flip the Y-axis)
                _contextMenuPosition.y = Screen.height - _contextMenuPosition.y;
                // Ensure the menu doesn't go off the screen
                _contextMenuPosition.x = Mathf.Clamp(_contextMenuPosition.x, 0, Screen.width - 10) + 10;
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

        [HideFromIl2Cpp]
        static bool IsPointInRect(Vector2 point, Rect rect)
        {
            if (rect == default) return false;
            return rect.Contains(point);
        }

        [HideFromIl2Cpp]
        private void BuildNodeEditorsFromFirstInit(DialogueTree value)
        {
            for (int i = 0; i < value.allConnections.Count; i++)
            {
                var connection = value.allConnections[i];
                if (connection == null) continue;
                if (connection.sourceNode == null || connection.targetNode == null) continue;
                if (!drawNodeEditors.ContainsKey(connection.sourceNode.UID))
                {
                    var editorSource = DrawNodeEditorFactory.GetDrawNodeEditorFromType(connection.sourceNode.GetIl2CppType());
                    if (editorSource == null)
                    {
                        MelonLogger.Warning("Editor Source is null for {0}", connection.sourceNode.GetIl2CppType().Name);
                        continue;
                    }
                    editorSource.Node = connection.sourceNode.Cast<DTNode>();
                    drawNodeEditors.TryAdd(connection.sourceNode.UID, editorSource);
                    editorSource.Position = new Vector2(Screen.width / 2, 100 * (i + 1));
                    editorSource.GraphEditorManager = this;
                }
                if (!drawNodeEditors.ContainsKey(connection.targetNode.UID))
                {
                    var editorTarget = DrawNodeEditorFactory.GetDrawNodeEditorFromType(connection.targetNode.GetIl2CppType());
                    if (editorTarget == null)
                    {
                        MelonLogger.Warning("Editor Target is null for {0}", connection.targetNode.GetIl2CppType().Name);
                        continue;
                    }
                    editorTarget.Node = connection.targetNode.Cast<DTNode>();
                    drawNodeEditors.TryAdd(connection.targetNode.UID, editorTarget);
                    editorTarget.Position = new Vector2(Screen.width / 2 + (i * 50), 200 * (i + 1));
                    editorTarget.GraphEditorManager = this;
                }

                if (drawNodeEditors.TryGetValue(connection.sourceNode.UID, out DrawNodeEditor sourceConnection) && drawNodeEditors.TryGetValue(connection.targetNode.UID, out DrawNodeEditor targetConnection))
                {
                    sourceConnection.ConnectedWith.Add(targetConnection);
                }
            }
        }

        /// <summary>
        /// Go into a subgraph of a node
        /// </summary>
        /// <param name="node">The node to go into</param>
        public void GoIntoSubGraph(SubDialogueTree node)
        {
            GraphEditorSnapshots.Add(new GraphEditorSnapshot(DialogueTree, _scaleFactor, _panOffset));
            drawNodeEditors.Clear();
            _scaleFactor = 1;
            _panOffset = new Vector2(200, -200);
            _isFirstDraw = true;
            DialogueTree = node.subGraph;
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
                for (var i = 0; i < drawNodeEditors.Count; i++)
                {
                    drawNodeEditors.Values.ElementAt(i).Init();
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

            // Draw only nodes and connections in the visible area
            DrawNodesInArea(visibleArea);
            DrawConnectionsInArea(visibleArea);

            // Draw the tooltip
            if (GUI.tooltip != "")
            {
                GUIStyle style = new(GUI.skin.label);
                style.normal.textColor = Color.white;
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;

                var width = style.CalcSize(new GUIContent(GUI.tooltip)).x;
                var height = style.CalcHeight(new GUIContent(GUI.tooltip), width);
                GUI.Label(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - 50, width, height), GUI.tooltip, style);
            }

            // Reset transformations
            GUI.matrix = Matrix4x4.identity;

            // Draw context menus if needed
            if (_showContextMenu) DrawContextMenu();
            if (_showSubContextMenu) DrawSubContextMenu();

            DrawControls();
        }

        [HideFromIl2Cpp]
        private void DrawControls()
        {
            if (GUI.Button(new(Screen.width - 110, 10, 100, 60), "Close Graph"))
            {
                CloseGraph();
            }
            if (GUI.Button(new(Screen.width - 110, 70, 100, 60), "Save Graph"))
            {
                DialogueTree.Serialize(null);
            }
        }

        [HideFromIl2Cpp]
        private void CloseGraph()
        {
            DialogueTree = null;
            _showContextMenu = false;
            _showSubContextMenu = false;
            drawNodeEditors.Clear();
            EditorManager.AllowNpcSelection = true;
            _scaleFactor = 1;
            _panOffset = new(200, -200);
            _isFirstDraw = true;
            EditorManager.ResetLastInvoked();
            // Restore the previous timescale if the game is paused which is the case when the graph is opened
            if (Time.timeScale == 0)
                Time.timeScale = 1;
            InputAccess.ToggleGampeplayActionMaps(true);
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
            Vector2 adjustedMousePosition = (e.mousePosition - _panOffset) / _scaleFactor;

            // On mouse down, check if we clicked on a node
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                bool isDoubleClick = e.clickCount == 2;

                foreach (var element in drawNodeEditors)
                {
                    var editor = element.Value;
                    Rect nodeRect = new(editor.Position, editor.NodeSize);

                    if (nodeRect.Contains(adjustedMousePosition))
                    {
                        if (isDoubleClick)
                        {
                            editor.OnDoubleClick(adjustedMousePosition);
                            e.Use();
                        }
                        else
                        {
                            _selectedNode = editor;
                            _dragOffset = adjustedMousePosition - editor.Position;
                        }
                        break;
                    }
                }
            }

            // On mouse drag, update the node's position
            if (e.type == EventType.MouseDrag && _selectedNode != null)
            {
                _selectedNode.Position = adjustedMousePosition - _dragOffset;
                e.Use();
            }

            // On mouse up, stop dragging
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _selectedNode = null;
            }
        }

        [HideFromIl2Cpp]
        private void DrawInfiniteBackground()
        {
            float gridSize = 50;
            Color gridColor = new(0.7f, 0.7f, 0.7f, 0.5f); // semi-transparent gray

            // Calculate the top-left position of the grid based on the pan offset
            float startX = Mathf.Floor((-_panOffset.x / _scaleFactor) / gridSize) * gridSize;
            float startY = Mathf.Floor((-_panOffset.y / _scaleFactor) / gridSize) * gridSize;

            // Calculate how many lines we need to draw on the screen
            int verticalLinesCount = Mathf.CeilToInt(Screen.width / (_scaleFactor * gridSize)) + 2;
            int horizontalLinesCount = Mathf.CeilToInt(Screen.height / (_scaleFactor * gridSize)) + 2;

            // Draw vertical grid lines
            for (int i = -1; i < verticalLinesCount; i++)
            {
                float x = startX + i * gridSize;
                Vector2 start = new(x, -10000);
                Vector2 end = new(x, 10000);

                GUIExtensions.DrawLine(start, end, gridColor, 1.0f);
            }

            // Draw horizontal grid lines
            for (int i = -1; i < horizontalLinesCount; i++)
            {
                float y = startY + i * gridSize;
                Vector2 start = new(-10000, y);
                Vector2 end = new(10000, y);

                GUIExtensions.DrawLine(start, end, gridColor, 1.0f);
            }
        }

        [HideFromIl2Cpp]
        private void DrawNodesInArea(Rect visibleArea)
        {
            for (int i = 0; i < drawNodeEditors.Values.Count; i++)
            {
                var element = drawNodeEditors.Values.ElementAt(i);
                if (element == null) continue;

                Rect nodeRect = new(
                    element.Position.x,
                    element.Position.y,
                    element.NodeSize.x,
                    element.NodeSize.y
                );

                if (visibleArea.Overlaps(nodeRect))
                {
                    element.DrawNode(element.Position);
                }
            }
        }


        #region ContextMenu

        [HideFromIl2Cpp]
        void DrawContextMenu()
        {
            float menuWidth = 120;

            DrawContextMenu(_contextMenuOptions.Length, menuWidth);


            for (int i = 0; i < _contextMenuOptions.Length; i++)
            {
                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + i * 20, menuWidth, 20), _contextMenuOptions[i].GetLocalizedString(null)))
                {
                    HandleContextMenuSelection(i);
                    _showContextMenu = false;
                }
            }
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
            var _subContextMenuOptions = DrawNodeEditorFactory.GetNodeNames();

            DrawContextMenu(_subContextMenuOptions.Length, menuWidth);

            // Display each option as a button
            for (int i = 0; i < _subContextMenuOptions.Length; i++)
            {
                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + i * 20, menuWidth, 20), _subContextMenuOptions[i]))
                {
                    HandleSubContextMenuSelection(_subContextMenuOptions[i]);
                    _showSubContextMenu = false; // Close the menu after selection
                }
            }
        }


        [HideFromIl2Cpp]
        void HandleSubContextMenuSelection(string option)
        {
            var drawNodeEditor = DrawNodeEditorFactory.GetDrawNodeEditorByName(option);
            if (drawNodeEditor != null)
            {

            }
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
                    MelonLogger.Msg("Delete selected");
                    break;
                case (int)GraphEditorAction.Duplicate:
                    MelonLogger.Msg("Duplicate selected");
                    break;
            }
        }

        enum GraphEditorAction
        {
            Create,
            Delete,
            Duplicate
        }
        #endregion ContextMenu


        #region LineDrawing

        [HideFromIl2Cpp]
        private void DrawConnectionsInArea(Rect visibleArea)
        {
            foreach (var element in drawNodeEditors)
            {
                var editor = element.Value;
                if (editor == null || editor.ConnectedWith.Count == 0) continue;

                for (int i = 0; i < editor.ConnectedWith.Count; i++)
                {
                    DrawNodeEditor connectedEditor = editor.ConnectedWith[i];
                    if (connectedEditor == null) continue;

                    // Get the transformed positions of the nodes
                    Vector2 editorCenter = editor.Position + (editor.NodeSize / 2);
                    Vector2 connectedCenter = connectedEditor.Position + (connectedEditor.NodeSize / 2);

                    var editorEdge = GetEdgePoint(editorCenter, editor.NodeSize, connectedCenter);
                    var connectedEdge = GetEdgePoint(connectedCenter, connectedEditor.NodeSize, editorCenter);

                    var overlap = new Rect(editorEdge, connectedEdge - editorEdge);

                    // Draw the line if either point is in the visible area
                    if (visibleArea.Contains(editorCenter) || visibleArea.Contains(connectedCenter) || visibleArea.Overlaps(overlap))
                    {
                        DrawLine(editorEdge, connectedEdge, Color.white, i + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the point where the edge of the node intersects with the line to the target.
        /// </summary>
        /// <param name="nodeCenter">Center of the node</param>
        /// <param name="nodeSize">Size of the node</param>
        /// <param name="target">Node where the line will be drawn</param>
        /// <returns>Edge point</returns>
        [HideFromIl2Cpp]
        static Vector2 GetEdgePoint(Vector2 nodeCenter, Vector2 nodeSize, Vector2 target)
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

            return nodeCenter + direction * scale;
        }

        /// <summary>
        /// Draws a line between two points in the GUI.
        /// </summary>
        /// <param name="pointA">Line a</param>
        /// <param name="pointB">Line b</param>
        /// <param name="color">Which color it should have</param>
        /// <param name="index">Index of the line</param>
        [HideFromIl2Cpp]
        void DrawLine(Vector2 pointA, Vector2 pointB, Color color, int index)
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

        #endregion LineDrawing
    }
}
