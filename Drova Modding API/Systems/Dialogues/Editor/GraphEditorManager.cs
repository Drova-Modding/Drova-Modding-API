using Drova_Modding_API.Access;
using Drova_Modding_API.Extensions;
using Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies;
using static Drova_Modding_API.Systems.Dialogues.ActorParameterHelper;
using Drova_Modding_API.Systems.Dialogues.Editor.Factories;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Drova_Modding_API.Systems.Dialogues.Store;
using Drova_Modding_API.Systems.Editor;
using Drova_Modding_API.Systems.Routines;
using Drova_Modding_API.Systems.Routines.Editor;
using Drova_Modding_API.Systems.Routines.Store;
using Il2Cpp;
using Il2CppDrova;
using Il2CppInterop.Runtime.Attributes;
using Il2CppNodeCanvas.BehaviourTrees;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// IMGUI editor for NodeCanvas graphs: <see cref="Il2CppNodeCanvas.DialogueTrees.DialogueTree"/>s and the
    /// <see cref="Il2CppNodeCanvas.BehaviourTrees.BehaviourTree"/>s that drive routines. The node editor factory is
    /// picked by the type of the open graph.
    /// </summary>
    /// <param name="ptr">Do not try to create this object with new()!</param>
    [RegisterTypeInIl2Cpp]
    public class GraphEditorManager(IntPtr ptr) : MonoBehaviour(ptr)
    {
        /// <summary>
        /// Graphs the editor went into a sub graph from, so it can go back.
        /// </summary>
        protected readonly List<GraphEditorSnapshot> GraphEditorSnapshots = [];

        private const float StatusSeconds = 6f;

        private Graph? _graph;
        private DialogueTree? _dialogueTree;
        private BehaviourTree? _behaviourTree;
        private bool _isActive;
        private IActionStrategy? _activeAction;
        private string? _activeActionHint;
        private string? _status;
        private bool _statusIsError;
        private float _statusUntil;

        /// <summary>
        /// The dialogue tree that is being edited, or null when no graph or a non-dialogue graph is open.
        /// Setting it opens the tree like <see cref="Graph"/>.
        /// </summary>
        public DialogueTree? DialogueTree
        {
            get => _dialogueTree;
            set => Graph = value;
        }

        /// <summary>
        /// The behaviour tree that is being edited, or null when no graph or a non-behaviour-tree graph is open.
        /// </summary>
        public BehaviourTree? BehaviourTree => _behaviourTree;

        /// <summary>
        /// The graph that is being edited. Setting a graph opens the editor on it and pauses the game; null clears it.
        /// Only dialogue trees and behaviour trees can be edited, any other graph is refused.
        /// </summary>
        public Graph? Graph
        {
            get
            {
                return _graph;
            }
            set
            {
                _nodeActorDropdowns.Clear();
                InvalidateActorLayoutCache();
                _brokenEditors.Clear();
                if (value == null)
                {
                    _graph = null;
                    _dialogueTree = null;
                    _behaviourTree = null;
                    return;
                }

                if (!IsEditable(value))
                {
                    MelonLogger.Warning($"Graph editor: '{value.name}' is a {value.GetIl2CppType().Name}, which this editor cannot edit.");
                    return;
                }

                if (value.IsLazyLoading || value.allNodes.Count == 0)
                {
                    value.SelfDeserialize();
                }
                value.DeserializeIfNotDoneYet(true);
                _graph = value;
                _dialogueTree = value.TryCast<DialogueTree>();
                _behaviourTree = value.TryCast<BehaviourTree>();
                BuildNodeEditorsFromFirstInit(value);
                if (_isActive) return;
                EditorManager.AllowNpcSelection = false;
                InputAccess.SetGameplayInputBlocked("GraphEditor", true);
                Time.timeScale = 0;
                _isActive = true;
            }
        }

        private DrawNodeEditor? _selectedNode;

        private Vector2 _dragOffset;

        /// <summary>
        /// Factory for creating node editors
        /// </summary>
        [HideFromIl2Cpp]
        public DrawNodeEditorFactory DrawNodeEditorFactory { get; set; }

        /// <summary>
        /// Factory for creating node editors of behaviour trees
        /// </summary>
        [HideFromIl2Cpp]
        public DrawNodeEditorFactory BehaviourTreeNodeEditorFactory { get; set; }

        /// <summary>
        /// The node editor factory for the open graph
        /// </summary>
        [HideFromIl2Cpp]
        public DrawNodeEditorFactory ActiveNodeEditorFactory => BehaviourTree != null ? BehaviourTreeNodeEditorFactory : DrawNodeEditorFactory;

        /// <summary>
        /// Factory for creating task editors
        /// </summary>
        [HideFromIl2Cpp]
        public DrawTaskEditorFactory DrawTaskEditorFactory { get; set; }

        private bool _showContextMenu;

        private bool _showSubContextMenu;

        private Vector2 _contextMenuPosition;

        private Rect _rect;
        private float _scaleFactor = 1f;
        private Vector2 _panOffset = new(200, -200);
        private Vector2 _dragStart;
        private bool _isDragging;
        private bool _isDraggingNode;

        private bool _isFirstDraw = true;

        private readonly LocalizedString[] _contextMenuOptions = [new("Modding_API/GraphEditor", "Create"), new("Modding_API/GraphEditor", "Delete"), new("Modding_API/GraphEditor", "Duplicate"), new("Modding_API/GraphEditor", "Connect"), new("Modding_API/GraphEditor", "MainNode")];

        private readonly Dictionary<string, DrawNodeEditor> _drawNodeEditors = [];

        // Reused each draw, because a node can open another graph and clear _drawNodeEditors mid-loop.
        private readonly List<DrawNodeEditor> _drawOrder = [];

        // Editors that threw while drawing, with the message shown in their place.
        private readonly Dictionary<DrawNodeEditor, string> _brokenEditors = [];

        private readonly HashSet<DrawNodeEditor> _overlappingEditors = [];

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
            BehaviourTreeNodeEditorFactory = new BehaviourTreeNodeEditorFactory();
            DrawTaskEditorFactory = new DrawTaskEditorFactory();
        }

        internal void Start()
        {
            useGUILayout = false;
        }

        internal void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Vector2 guiMousePosition = Input.mousePosition;
                guiMousePosition.y = Screen.height - guiMousePosition.y;

                DrawNodeEditor? nodeUnderCursor = FindNodeAt((guiMousePosition - _panOffset) / _scaleFactor);
                SetSelectedNode(nodeUnderCursor);

                _showContextMenu = true;
                _contextMenuPosition = guiMousePosition;
                _contextMenuPosition.x = Mathf.Clamp(_contextMenuPosition.x, 0, Screen.width - 10) + 10;
                EndActiveAction();
            }
            if (Input.GetKey(KeyCode.Escape))
            {
                EndActiveAction();
            }
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
            if (Graph == null) return;

            Color prevoiousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.backgroundColor = prevoiousBackgroundColor;

            if (_isFirstDraw)
            {
                _isFirstDraw = false;
                _drawOrder.Clear();
                _drawOrder.AddRange(_drawNodeEditors.Values);
                foreach (DrawNodeEditor editor in _drawOrder)
                {
                    try
                    {
                        editor.Init();
                    }
                    catch (Exception e)
                    {
                        MarkBroken(editor, e);
                    }
                }
            }

            // The Close and Save buttons are drawn in finally, so one failing node can never lock the paused game.
            try
            {
                DrawGraph();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GraphEditor] Drawing the graph failed: " + e);
            }
            finally
            {
                GUI.matrix = Matrix4x4.identity;
                if (Graph != null)
                {
                    if (_showContextMenu) DrawContextMenu();
                    if (_showSubContextMenu) DrawSubContextMenu();

                    DrawActiveActionHint();
                    DrawControls();
                }
            }
        }

        [HideFromIl2Cpp]
        private void DrawGraph()
        {
            HandleZoomAndPan();

            HandleMouseClick();

            Rect visibleArea = new(
                -_panOffset.x / _scaleFactor,
                -_panOffset.y / _scaleFactor,
                Screen.width / _scaleFactor,
                Screen.height / _scaleFactor
            );

            GUI.matrix = Matrix4x4.TRS(_panOffset, Quaternion.identity, new Vector3(1 * _scaleFactor, 1 * _scaleFactor, 1));

            if (Event.current.type == EventType.Repaint)
                DrawInfiniteBackground();

            DetectOverlaps();

            DrawConnectionsInArea(visibleArea);
            DrawNodesInArea(visibleArea);
            DrawOverlapHighlights(visibleArea);
            HandleActiveActionStrategy();

            GUIDropdown.FlushOverlays(visibleArea);
            GUICreateConditionTask.FlushOverlays();

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
        }

        /// <summary>
        /// Shows a message under the editor buttons for a few seconds. Errors are red and also logged as a warning.
        /// </summary>
        [HideFromIl2Cpp]
        public void ShowStatus(string message, bool isError = false)
        {
            _status = message;
            _statusIsError = isError;
            _statusUntil = Time.realtimeSinceStartup + StatusSeconds;
            if (isError) MelonLogger.Warning("[GraphEditor] " + message);
        }

        [HideFromIl2Cpp]
        private void MarkBroken(DrawNodeEditor editor, Exception e)
        {
            string typeName = editor.Node != null ? editor.Node.GetIl2CppType().Name : editor.GetType().Name;
            _brokenEditors[editor] = $"Could not draw {typeName}: {e.Message}";
            MelonLogger.Error($"[GraphEditor] Node editor for {typeName} failed, it is shown as a red box from now on: {e}");
            ShowStatus($"The {typeName} node cannot be shown. The rest of the graph can still be edited and saved.", true);
        }

        [HideFromIl2Cpp]
        private static bool IsEditable(Graph graph)
        {
            return graph.TryCast<DialogueTree>() != null || graph.TryCast<BehaviourTree>() != null;
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
        private void BuildNodeEditorsFromFirstInit(Graph value)
        {
            Vector2 lastPosition = new(100, 0);
            for (int i = 0; i < value.allNodes.Count; i++)
            {
                Il2CppNodeCanvas.Framework.Node node = value.allNodes[i];
                if (node == null) continue;
                if (!_drawNodeEditors.ContainsKey(node.UID))
                {
                    DrawNodeEditor? editor = ActiveNodeEditorFactory.GetDrawNodeEditorFromType(node.GetIl2CppType());
                    if (editor == null)
                    {
                        MelonLogger.Warning("Editor is null for {0}", node.GetIl2CppType().Name);
                        continue;
                    }
                    editor.Node = node;
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
                    DrawNodeEditor? connectedEditor = ActiveNodeEditorFactory.GetDrawNodeEditorFromType(targetNode.GetIl2CppType());
                    if (connectedEditor == null)
                    {
                        MelonLogger.Warning("Editor is null for {0}", targetNode.GetIl2CppType().Name);
                        continue;
                    }
                    connectedEditor.Node = targetNode;
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
        public void GoIntoSubGraph(SubDialogueTree node)
        {
            GoIntoSubGraph(node.subGraph);
        }

        /// <summary>
        /// Open a nested graph, remembering the current one so the editor can go back to it.
        /// </summary>
        [HideFromIl2Cpp]
        public void GoIntoSubGraph(Graph? subGraph)
        {
            if (Graph == null) return;
            if (subGraph == null)
            {
                ShowStatus("This node has no nested graph assigned.");
                return;
            }
            if (!IsEditable(subGraph))
            {
                ShowStatus($"'{subGraph.name}' is a {subGraph.GetIl2CppType().Name}. Only dialogues and behaviour trees can be edited here.", true);
                return;
            }
            GraphEditorSnapshots.Add(new GraphEditorSnapshot(Graph, _scaleFactor, _panOffset));
            _drawNodeEditors.Clear();
            _selectedNode = null;
            _isDraggingNode = false;
            _scaleFactor = 1;
            _panOffset = new Vector2(200, -200);
            _isFirstDraw = true;
            Graph = subGraph;
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
                SaveGraph();
            }
            if (GUI.Button(new(Screen.width - 160, 130, 150, 60), "Resolve Overlaps"))
            {
                ResolveOverlaps();
            }
            DrawActorParameterControls();
            DrawDialogueSettings();
            if (GraphEditorSnapshots.Count > 0)
            {
                GraphEditorSnapshot snapshot = GraphEditorSnapshots[^1];
                if (GUI.Button(new(10, 10, 550, 60), $"Go back to {snapshot.Graph.name}"))
                {
                    _drawNodeEditors.Clear();
                    _selectedNode = null;
                    _isDraggingNode = false;
                    _isFirstDraw = true;
                    _scaleFactor = snapshot.ScaleFactor;
                    _panOffset = snapshot.PanOffset;
                    Graph = snapshot.Graph;
                    GraphEditorSnapshots.RemoveAt(GraphEditorSnapshots.Count - 1);
                }
            }
            GUI.Label(new(10, 80, 550, 25), $"Current Editor: {Graph!.name}");
            string selectedNodeLabel = _selectedNode == null
                ? "Selected Node: none"
                : $"Selected Node: {_selectedNode.Node.GetIl2CppType().Name}";
            GUI.Label(new(10, 105, 550, 25), selectedNodeLabel);
            DrawStatus();
        }

        [HideFromIl2Cpp]
        private void DrawStatus()
        {
            if (_status == null) return;
            if (Time.realtimeSinceStartup > _statusUntil)
            {
                _status = null;
                return;
            }

            Rect rect = new(Screen.width - 520, 200, 510, 44);
            Color previousColor = GUI.color;
            GUI.color = _statusIsError ? new Color(1f, 0.35f, 0.35f) : new Color(0.6f, 1f, 0.6f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, rect.height - 8), _status);
            GUI.color = previousColor;
        }

        /// <summary>
        /// Saves the open graph to its store and patches the live asset, so everything referencing it picks the change up.
        /// A graph open here that already is the live asset is not re-deserialized, which would detach the node editors
        /// from its nodes and lose every later edit. Running routines of a behaviour tree are restarted instead.
        /// </summary>
        [HideFromIl2Cpp]
        private void SaveGraph()
        {
            Graph? graph = Graph;
            if (graph == null) return;
            if (!GraphStore.HasValidStartNode(graph))
            {
                ShowStatus("Not saved: the graph has no start node. Right-click the first node and pick 'MainNode'.", true);
                return;
            }

            DialogueTree? dialogueTree = DialogueTree;
            if (dialogueTree != null)
            {
                string dialogueId = string.IsNullOrWhiteSpace(dialogueTree.Key) ? dialogueTree.name : dialogueTree.Key;
                if (!DialogueStore.SaveDialogue(dialogueId, dialogueTree))
                {
                    ShowStatus($"Saving '{dialogueId}' failed, see the MelonLoader log.", true);
                    return;
                }
                if (!GraphStore.IsLiveAsset(dialogueId, dialogueTree))
                {
                    DialogueStore.TryPatchInMemoryDialogue(dialogueId);
                }
                ShowStatus($"Saved '{dialogueId}' to {DialogueStore.GetDialogueFilePath(dialogueId)}");
                return;
            }

            BehaviourTree? behaviourTree = BehaviourTree;
            if (behaviourTree == null) return;
            if (!BehaviourTreeStore.SaveBehaviourTree(behaviourTree.name, behaviourTree))
            {
                ShowStatus($"Saving '{behaviourTree.name}' failed, see the MelonLoader log.", true);
                return;
            }
            RoutineGraphAccess.Invalidate(behaviourTree.name);
            ShowStatus($"Saved '{behaviourTree.name}' to {BehaviourTreeStore.GetFilePath(behaviourTree.name)}");
        }

        [HideFromIl2Cpp]
        private void DrawDialogueSettings()
        {
            if (DialogueTree == null) return;

            GUI.Box(new Rect(380, 130, 360, 68), "Dialogue Settings");
            GUI.Label(new Rect(390, 152, 90, 20), "LocaPath");
            DialogueTree._locaPath = GUI.TextField(new Rect(460, 152, 270, 20), DialogueTree.LocaPath);
        }

        [HideFromIl2Cpp]
        private void DrawActorParameterControls()
        {
            if (DialogueTree == null) return;

            Il2CppSystem.Collections.Generic.List<DialogueTree.ActorParameter>? parameters = DialogueTree.actorParameters;
            int count = parameters?.Count ?? 0;
            const float rowHeight = 22f;
            float listRows = count > 0 ? count : 1;
            float boxHeight = 46f + (listRows * rowHeight);

            GUI.Box(new Rect(10, 130, 360, boxHeight), "Actor Parameters");
            GUI.Label(new Rect(20, 152, 90, 20), "Name");
            _newActorParameterName = GUI.TextField(new Rect(70, 152, 200, 20), _newActorParameterName);
            if (GUI.Button(new Rect(280, 152, 80, 20), "Add"))
            {
                AddActorParameter(_newActorParameterName);
            }

            if (parameters == null || count == 0)
            {
                GUI.Label(new Rect(20, 174, 330, 20), "No actor parameters");
                return;
            }

            int removeIndex = -1;
            for (int i = 0; i < count; i++)
            {
                float rowY = 174 + (i * rowHeight);
                string label = (i == 0 ? "Default: " : string.Empty) + GetActorParameterName(parameters[i]);
                GUI.Label(new Rect(20, rowY, 300, 20), label);
                if (GUI.Button(new Rect(326, rowY, 24, 20), "X"))
                {
                    removeIndex = i;
                }
            }

            if (removeIndex >= 0)
            {
                RemoveActorParameter(removeIndex);
            }
        }

        [HideFromIl2Cpp]
        private void AddActorParameter(string actorName)
        {
            if (DialogueTree == null) return;

            string trimmedName = actorName.Trim();
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
        private void RemoveActorParameter(int index)
        {
            if (DialogueTree == null || DialogueTree.actorParameters == null) return;
            if (index < 0 || index >= DialogueTree.actorParameters.Count) return;

            DialogueTree.actorParameters.RemoveAt(index);

            _nodeActorDropdowns.Clear();
            InvalidateActorLayoutCache();
        }

        /// <summary>
        /// Closes the editor and unpauses the game, like its Close button.
        /// </summary>
        [HideFromIl2Cpp]
        internal void CloseGraph()
        {
            _isActive = false;
            DialogueTree = null;
            GraphEditorSnapshots.Clear();
            _status = null;
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
            if (Time.timeScale == 0)
                Time.timeScale = 1;
            InputAccess.SetGameplayInputBlocked("GraphEditor", false);
            gameObject.SetActive(false);
        }

        [HideFromIl2Cpp]
        private void HandleZoomAndPan()
        {
            Event e = Event.current;

            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = e.delta.y * -0.01f;
                _scaleFactor = Mathf.Clamp(_scaleFactor + zoomDelta, 0.1f, 5.0f);

                e.Use();
            }

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

        [HideFromIl2Cpp]
        void HandleMouseClick()
        {
            Event e = Event.current;

            // A menu click would otherwise deselect the node before the action runs.
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

            // Blocks dragging or deselecting the source node while an action runs.
            if (_activeAction != null) return;

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

            if (e.type == EventType.MouseDrag && _isDraggingNode && _selectedNode != null)
            {
                _selectedNode.Position = adjustedMousePosition - _dragOffset;
                e.Use();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isDraggingNode = false;
            }
        }

        [HideFromIl2Cpp]
        private void DrawInfiniteBackground()
        {
            float gridSize = 50;
            Color gridColor = new(0.7f, 0.7f, 0.7f, 0.5f);

            // DrawLine rotates around a screen-space pivot, so the pan/scale matrix would skew vertical lines.
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;

            float worldLeft = -_panOffset.x / _scaleFactor;
            float worldTop = -_panOffset.y / _scaleFactor;
            float worldRight = worldLeft + (Screen.width / _scaleFactor);
            float worldBottom = worldTop + (Screen.height / _scaleFactor);

            float startWorldX = Mathf.Floor(worldLeft / gridSize) * gridSize;
            float startWorldY = Mathf.Floor(worldTop / gridSize) * gridSize;

            int verticalLinesCount = Mathf.CeilToInt((worldRight - startWorldX) / gridSize) + 1;
            int horizontalLinesCount = Mathf.CeilToInt((worldBottom - startWorldY) / gridSize) + 1;

            for (int i = 0; i < verticalLinesCount; i++)
            {
                float worldX = startWorldX + (i * gridSize);
                float screenX = (worldX * _scaleFactor) + _panOffset.x;
                GUIExtensions.DrawLine(new Vector2(screenX, 0), new Vector2(screenX, Screen.height), gridColor);
            }

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
            bool hasActors = TryGetActorOptions(out string[] actorNames, out string[] actorIds);
            _drawOrder.Clear();
            _drawOrder.AddRange(_drawNodeEditors.Values);
            foreach (DrawNodeEditor element in _drawOrder)
            {
                if (element == null) continue;

                Rect nodeRect = new(
                    element.Position.x,
                    element.Position.y,
                    element.NodeSize.x,
                    element.NodeSize.y
                );

                if (!visibleArea.Overlaps(nodeRect)) continue;
                if (_brokenEditors.TryGetValue(element, out string? error))
                {
                    DrawBrokenNode(element, error);
                    continue;
                }

                try
                {
                    element.DrawNode(element.Position);
                    if (hasActors) DrawNodeActorSelector(element, actorNames, actorIds);
                }
                catch (Exception e)
                {
                    MarkBroken(element, e);
                }
            }

            if (_nodeActorDropdowns.Count == 0) return;

            List<string>? staleUids = null;
            foreach (string uid in _nodeActorDropdowns.Keys)
            {
                if (!_drawNodeEditors.ContainsKey(uid))
                    (staleUids ??= []).Add(uid);
            }
            if (staleUids == null) return;
            for (int i = 0; i < staleUids.Count; i++)
            {
                _nodeActorDropdowns.Remove(staleUids[i]);
            }
        }

        [HideFromIl2Cpp]
        private static void DrawBrokenNode(DrawNodeEditor editor, string error)
        {
            Vector2 size = editor.NodeSize.x > 0 ? editor.NodeSize : new Vector2(300, 60);
            Rect rect = new(editor.Position, new Vector2(Mathf.Max(size.x, 300), Mathf.Max(size.y, 60)));
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.3f, 0.3f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12), error);
            GUI.color = previousColor;
        }

        [HideFromIl2Cpp]
        private void DrawNodeActorSelector(DrawNodeEditor editor, string[] actorNames, string[] actorIds)
        {
            if (DialogueTree == null || editor.Node == null) return;

            DTNode? node = editor.Node.TryCast<DTNode>();
            if (node == null) return;
            // Multiple choice is always player-driven and should not expose actor selection.
            if (node is DS_MultipleChoiceNode || !node.requireActorSelection) return;

            BindDefaultActorIfUnset(node, actorNames, actorIds);

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
        internal void ApplyDefaultActorParameter(Node graphNode)
        {
            DTNode? node = graphNode.TryCast<DTNode>();
            if (DialogueTree == null || node == null) return;
            if (node is DS_MultipleChoiceNode || !node.requireActorSelection) return;
            if (!TryGetActorOptions(out string[] actorNames, out string[] actorIds)) return;

            BindDefaultActorIfUnset(node, actorNames, actorIds);
        }

        [HideFromIl2Cpp]
        private static void BindDefaultActorIfUnset(DTNode node, string[] actorNames, string[] actorIds)
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
            Il2CppSystem.Collections.Generic.List<DialogueTree.ActorParameter>? parameters = DialogueTree?.actorParameters;
            if (parameters == null || parameters.Count == 0)
            {
                return false;
            }

            int count = parameters.Count;
            actorNames = new string[count];
            actorIds = new string[count];
            for (int i = 0; i < count; i++)
            {
                DialogueTree.ActorParameter actorParameter = parameters[i];
                actorNames[i] = GetActorParameterName(actorParameter);
                actorIds[i] = GetActorParameterId(actorParameter);
            }

            return actorNames.Length > 0;
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
            return index == (int)GraphEditorAction.Delete || index == (int)GraphEditorAction.Duplicate || index == (int)GraphEditorAction.Connect || index == (int)GraphEditorAction.SelectPrimeNode;
        }

        [HideFromIl2Cpp]
        void DrawContextMenu(int length, float menuWidth)
        {
            float menuHeight = 20 * length;
            _rect = new Rect(_contextMenuPosition.x, _contextMenuPosition.y, menuWidth, menuHeight);

            GUI.Box(_rect, "");
        }

        [HideFromIl2Cpp]
        void DrawSubContextMenu()
        {
            float menuWidth = 200;
            string[] subContextMenuOptions = ActiveNodeEditorFactory.GetNodeNames();

            DrawContextMenu(subContextMenuOptions.Length, menuWidth);

            for (int i = 0; i < subContextMenuOptions.Length; i++)
            {
                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + (i * 20), menuWidth, 20), subContextMenuOptions[i]))
                {
                    HandleSubContextMenuSelection(subContextMenuOptions[i]);
                    _showSubContextMenu = false;
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
                case (int)GraphEditorAction.SelectPrimeNode:
                    if (_selectedNode == null) return;
                    _graph!.primeNode = _selectedNode.Node;
                    break;
            }
        }

        enum GraphEditorAction
        {
            Create,
            Delete,
            Duplicate,
            Connect,
            SelectPrimeNode
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

                    Vector2 editorCenter = editor.Position + (editor.NodeSize / 2);
                    Vector2 connectedCenter = connectedEditor.Position + (connectedEditor.NodeSize / 2);

                    Vector2 editorEdge = GetEdgePoint(editorCenter, editor.NodeSize, connectedCenter);
                    Vector2 connectedEdge = GetEdgePoint(connectedCenter, connectedEditor.NodeSize, editorCenter);

                    Rect overlap = new(editorEdge, connectedEdge - editorEdge);

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
