using Drova_Modding_API.Systems.DebugUtils;
using Drova_Modding_API.Systems.Dialogues.Editor.Nodes;
using Il2Cpp;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;
using Drova_Modding_API.Extensions;
using UnityEngine.UIElements;
using Il2CppMemoryPack.Internal;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// Class that manages the context menu for the graph editor window
    /// </summary>
    /// <param name="ptr">Do not try to create this object with new()!</param>
    [RegisterTypeInIl2Cpp]
    internal class GraphEditorManager(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private DialogueTree _dialogueTree;
        // Reference to the dialogue tree being edited
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
                BuildNodeEditorsFromFirstInit(value);
                _dialogueTree = value;
                DebugManager.AllowNpcSelection = false;
                Time.timeScale = 0;
            }
        }

        // Currently selected node for dragging
        private DrawNodeEditor _selectedNode = null;

        // Offset between the mouse position and node position
        private Vector2 _dragOffset;

        // Reference to the factory for creating node editors
        public DrawNodeEditorFactory DrawNodeEditorFactory { get; set; }


        // Track whether the context menu is open
        private bool _showContextMenu = false;

        // Track whether the sub-context menu is open
        private bool _showSubContextMenu = false;

        // Position where the right-click occurred
        private Vector2 _contextMenuPosition;

        // Current rect of the context menu
        private Rect _rect;

        private readonly float _scaleFactor = 1f;               // Zoom level
        private Vector2 _panOffset = new Vector2(200, -200);      // Offset for panning
        private Vector2 _dragStart;                     // Start point for panning drag
        private bool _isDragging = false;               // Is panning in progress?

        // List of context menu options
        private readonly LocalizedString[] _contextMenuOptions = [new("Modding_API/GraphEditor", "Create"), new("Modding_API/GraphEditor", "Delete"), new("Modding_API/GraphEditor", "Duplicate")];

        private List<LocalizedString> _subContextMenuOptions = [new("Modding_API/GraphEditor", "CreateStatementNode")];

        private readonly Dictionary<string, DrawNodeEditor> drawNodeEditors = [];

        internal void Awake()
        {
            DrawNodeEditorFactory = new DrawNodeEditorFactory();
            DebugManager.OnNpcSelected += DebugManager_OnNpcSelected;
        }

        internal void OnDestroy()
        {
            DebugManager.OnNpcSelected -= DebugManager_OnNpcSelected;
        }

        private void DebugManager_OnNpcSelected(Actor actor)
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

        bool IsPointInRect(Vector2 point, Rect rect)
        {
            if (rect == default) return false;
            return rect.Contains(point);
        }

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
                    editorSource.Node = connection.sourceNode.Cast<DTNode>();
                    drawNodeEditors.TryAdd(connection.sourceNode.UID, editorSource);
                    editorSource.Position = new Vector2(Screen.width / 2, 100 * (i + 1));
                    editorSource.NodeSize = new Vector2(10, 10);
                }
                if (!drawNodeEditors.ContainsKey(connection.targetNode.UID))
                {
                    var editorTarget = DrawNodeEditorFactory.GetDrawNodeEditorFromType(connection.targetNode.GetIl2CppType());
                    editorTarget.Node = connection.targetNode.Cast<DTNode>();
                    drawNodeEditors.TryAdd(connection.targetNode.UID, editorTarget);
                    editorTarget.Position = new Vector2(Screen.width / 2 + (i * 50), 200 * (i + 1));
                    editorTarget.NodeSize = new Vector2(10, 10);
                }

                if (drawNodeEditors.TryGetValue(connection.sourceNode.UID, out DrawNodeEditor sourceConnection) && drawNodeEditors.TryGetValue(connection.targetNode.UID, out DrawNodeEditor targetConnection))
                {
                    sourceConnection.ConnectedWith.Add(targetConnection);
                }
            }
        }


        internal void OnGUI()
        {
            if (DialogueTree == null) return;

            // Define the visible screen rect for dynamic rendering
            Rect visibleArea = new(
                -_panOffset.x / _scaleFactor,
                -_panOffset.y / _scaleFactor,
                Screen.width / _scaleFactor,
                Screen.height / _scaleFactor
            );

            // Handle user input for zoom and pan
            HandleZoomAndPan();

            // Handle node dragging
            HandleNodeDragging();

            // Apply transformations for zoom and pan
            GUI.matrix = Matrix4x4.TRS(_panOffset, Quaternion.identity, new Vector3(1 * _scaleFactor, 1 * _scaleFactor, 1));

            // Draw the background
            DrawInfiniteBackground();

            // Draw only nodes and connections in the visible area
            DrawNodesInArea(visibleArea);
            DrawConnectionsInArea(visibleArea);

            // Reset transformations
            GUI.matrix = Matrix4x4.identity;

            // Draw context menus if needed
            if (_showContextMenu) DrawContextMenu();
            if (_showSubContextMenu) DrawSubContextMenu();

            // Draw close button
            if (GUI.Button(new Rect(Screen.width - 110, 10, 100, 60), "Close Graph"))
            {
                CloseGraph();
            }
        }

        private void CloseGraph()
        {
            DialogueTree = null;
            _showContextMenu = false;
            _showSubContextMenu = false;
            drawNodeEditors.Clear();
            DebugManager.AllowNpcSelection = true;
            DebugManager.ResetLastInvoked();
            // Restore the previous timescale if the game is paused which is the case when the graph is opened
            if (Time.timeScale == 0)
                Time.timeScale = 1;
        }

        private void HandleZoomAndPan()
        {
            Event e = Event.current;

            // Zoom with mouse scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = e.delta.y * -0.01f;
                float oldScaleFactor = _scaleFactor;
                //_scaleFactor = Mathf.Clamp(_scaleFactor + zoomDelta, 0.1f, 5.0f); // Allow more extreme zoom levels

                // Adjust pan offset to zoom around the mouse position
                Vector2 mousePosition = e.mousePosition;
                _panOffset += (mousePosition - _panOffset) * (1 - oldScaleFactor / _scaleFactor);
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

        void HandleNodeDragging()
        {
            Event e = Event.current;
            Vector2 adjustedMousePosition = (e.mousePosition - _panOffset) / _scaleFactor;
            // On mouse down, check if we clicked on a node
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Adjust the mouse position to account for zoom and pan
                MelonLogger.Msg($"HandleNodeDragging called. Event: {e.type}");
                foreach (var element in drawNodeEditors)
                {
                    var editor = element.Value;
                    Rect nodeRect = new(editor.Position, editor.NodeSize);

                    if (nodeRect.Contains(adjustedMousePosition))
                    {
                        _selectedNode = editor;
                        _dragOffset = adjustedMousePosition - editor.Position;
                        break;
                    }
                }
            }

            // On mouse drag, update the node's position
            if (e.type == EventType.MouseDrag && _selectedNode != null)
            {
                MelonLogger.Msg($"New Position: {adjustedMousePosition - _dragOffset} , adjustedMousePosition: {adjustedMousePosition}, dragOffset: {_dragOffset}");
                MelonLogger.Msg($"PanOffset: {_panOffset}");
                _selectedNode.Position = adjustedMousePosition - _dragOffset;
                e.Use(); // Mark the event as used to prevent further processing
            }

            // On mouse up, stop dragging
            if (_selectedNode != null && e.type == EventType.MouseUp && e.button == 0)
            {
                _selectedNode = null;
            }
        }

        private void DrawInfiniteBackground()
        {
            float gridSize = 50; // Size of each grid cell
            Color gridColor = new(0.7f, 0.7f, 0.7f, 0.5f); // Semi-transparent gray

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

        // Method to draw the nodes
        private void DrawNodesInArea(Rect visibleArea)
        {
            for (int i = 0; i < drawNodeEditors.Values.Count; i++)
            {
                var element = drawNodeEditors.Values.ElementAt(i);
                if (element == null) continue;

                // Check if the node is within the visible area
                Rect nodeRect = new(
                    element.Position.x,
                    element.Position.y,
                    element.NodeSize.x,
                    element.NodeSize.y
                );


                if (visibleArea.Overlaps(nodeRect))
                {
                    var rect = element.DrawNode(element.Position);
                    element.Position = rect.position;
                    element.NodeSize = rect.size;
                }
            }
        }


        #region ContextMenu

        // Method to draw the context menu
        void DrawContextMenu()
        {
            float menuWidth = 120;

            DrawContextMenu(_contextMenuOptions.Length, menuWidth);

            // Display each option as a button
            for (int i = 0; i < _contextMenuOptions.Length; i++)
            {
                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + i * 20, menuWidth, 20), _contextMenuOptions[i].GetLocalizedString(null)))
                {
                    HandleContextMenuSelection(i);
                    _showContextMenu = false; // Close the menu after selection
                }
            }
        }

        void DrawContextMenu(int length, float menuWidth)
        {
            float menuHeight = 20 * length;
            _rect = new Rect(_contextMenuPosition.x, _contextMenuPosition.y, menuWidth, menuHeight);

            // Draw the background box
            GUI.Box(_rect, "");
        }

        void DrawSubContextMenu()
        {

            float menuWidth = 120;

            DrawContextMenu(_subContextMenuOptions.Count, menuWidth);

            // Display each option as a button
            for (int i = 0; i < _subContextMenuOptions.Count; i++)
            {
                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + i * 20, menuWidth, 20), _subContextMenuOptions[i].GetLocalizedString(null)))
                {
                    HandleSubContextMenuSelection(i);
                    _showSubContextMenu = false; // Close the menu after selection
                }
            }
        }

        // Handle the sub-context menu option selection
        void HandleSubContextMenuSelection(int option)
        {
            MelonLogger.Msg(option + " created");
            // Implement your logic for creating nodes here
        }

        // Handle the context menu option selection
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


                    // Draw the line if either point is in the visible area
                    if (visibleArea.Contains(editorCenter) || visibleArea.Contains(connectedCenter))
                    {
                        DrawLine(editorCenter, connectedCenter, Color.white);
                    }
                }
            }
        }

        // Method to draw a line connecting two points
        void DrawLine(Vector2 pointA, Vector2 pointB, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;

            Vector2 delta = pointB - pointA;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            Matrix4x4 matrixBackup = GUI.matrix;

            GUI.matrix = Matrix4x4.TRS(pointA + _panOffset, Quaternion.Euler(0f, 0f, angle), Vector3.one);

            GUI.Box(new Rect(0, 0, length, 2), "");

            GUI.matrix = matrixBackup;
            GUI.color = previousColor;
        }

        #endregion LineDrawing
    }
}
