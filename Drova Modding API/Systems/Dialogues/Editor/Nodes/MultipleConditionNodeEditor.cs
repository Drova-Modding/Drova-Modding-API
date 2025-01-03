using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="MultipleConditionNode"/>
    /// </summary>
    internal class MultipleConditionNodeEditor : DrawNodeEditor
    {
        private MultipleConditionNode _castedNode;
        private readonly List<DrawTaskEditor> _drawTaskEditors = [];
        private readonly GUICreateConditionTask _guiCreateConditionTask = new();
        private const int _spaceBetweenTasks = 50;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<MultipleConditionNode>();
            for (int i = 0; i < _castedNode.Conditions.Count; i++)
            {
                var condition = _castedNode.Conditions[i];
                if (condition != null)
                {
                    var editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(condition.GetIl2CppType());
                    if (editor == null) continue;

                    var task = condition.TryCast<Il2CppNodeCanvas.Framework.Task>();
                    _drawTaskEditors.Add(editor);
                    editor.Task = task;
                    editor.Init();
                }
                else
                {
                    var editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(null);
                    _drawTaskEditors.Add(editor);
                    editor.Init();
                }
            }
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.white;
            Vector2 taskStartPosition = new(position.x, position.y + 30);
            var rect = new Rect(position.x, position.y, 350, 70);

            for (int i = 0; i < _drawTaskEditors.Count; i++)
            {
                var taskEditor = _drawTaskEditors[i];
                var size = taskEditor.DrawTask(taskStartPosition);
                taskStartPosition.y += size.height + _spaceBetweenTasks;
                rect.height += size.height + _spaceBetweenTasks;
                if (size.width > rect.width)
                {
                    rect.width = size.width + 20;
                }
            }

            var conditionTask = _guiCreateConditionTask.Draw(new Vector2(position.x, position.y + rect.height));
            if (conditionTask != null)
            {
                _castedNode.Conditions.Add(conditionTask);
                var editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(conditionTask.GetIl2CppType());
                _drawTaskEditors.Add(editor);
                editor.Task = conditionTask;
                editor.Init();
            }

            rect.height += 20 + _spaceBetweenTasks;


            GUI.color = Color.green;
            GUI.depth = 20;
            GUI.Box(rect, "MultipleConditionNode");

            GUI.depth = previousDepth;
            GUI.color = previousColor;

            return rect;
        }
    }
}
