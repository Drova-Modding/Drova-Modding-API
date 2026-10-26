using Drova_Modding_API.Systems.Dialogues.Editor;
using Drova_Modding_API.Systems.Dialogues.Editor.Tasks;
using Il2CppNodeCanvas.Framework;
using UnityEngine;
using Task = Il2CppNodeCanvas.Framework.Task;

namespace Drova_Modding_API.Systems.Routines.Editor
{
    /// <summary>
    /// Fallback editor for any node without a dedicated one. Shows the node type, its task and its public settings.
    /// Double-click opens a nested graph.
    /// </summary>
    internal class GenericNodeEditor : DrawNodeEditor
    {
        private const float Width = 400;

        private string _title = string.Empty;
        private string _detail = string.Empty;
        private GenericTaskEditor? _settings;
        private float _settingsHeight;

        public GenericNodeEditor()
        {
            NodeSizeInternal = new Vector2(Width, 60);
        }

        public override void Init()
        {
            _title = Node.GetIl2CppType().Name;
            string nodeName = Node.name;
            Task? task = Node.TryCast<ITaskAssignable>()?.task;
            _detail = task != null ? "Task: " + task.GetIl2CppType().Name : nodeName != _title ? nodeName : string.Empty;
            if (Node.TryCast<IGraphAssignable>() != null) _detail += "  (double-click to open)";

            GenericTaskEditor settings = new() { GraphEditorManager = GraphEditorManager };
            settings.InitFor(Node, true);
            _settings = settings.HasFields ? settings : null;
        }

        public override void DrawNode(Vector2 position)
        {
            NodeSizeInternal = new Vector2(Width, 60 + _settingsHeight);
            GUI.Box(new Rect(position.x, position.y, NodeSize.x, NodeSize.y), _title);
            GUI.Label(new Rect(position.x + 10, position.y + 25, NodeSize.x - 20, 25), _detail);
            if (_settings != null) _settingsHeight = _settings.DrawTask(new Vector2(position.x + 10, position.y + 50)).height + 5;
        }

        public override void OnDoubleClick(Vector2 mousePosition)
        {
            GraphEditorManager.GoIntoSubGraph(Node.TryCast<IGraphAssignable>()?.subGraph);
        }
    }
}
