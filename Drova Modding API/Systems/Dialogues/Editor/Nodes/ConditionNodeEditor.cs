using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class ConditionNodeEditor : DrawNodeEditor
    {
        private ConditionNode _castedNode;
        private GUICreateConditionTask _guiCreateConditionTask;
        private DrawTaskEditor _drawTaskEditor;
        private bool _changeCondition = false;

        public ConditionNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 120);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<ConditionNode>();
            _guiCreateConditionTask = new GUICreateConditionTask();
            if (_castedNode._condition != null)
            {
                _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(_castedNode._condition.GetIl2CppType());
                _drawTaskEditor.Task = _castedNode._condition;
                _drawTaskEditor.GraphEditorManager = GraphEditorManager;
                _drawTaskEditor.Init();
            }
            else
            {
                _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(null);
                _drawTaskEditor.Task = null;
                _drawTaskEditor.GraphEditorManager = GraphEditorManager;
                _drawTaskEditor.Init();
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            var rect = new Rect(position.x, position.y, 350, 120);
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(rect, "ConditionNode");
            GUI.color = previousColor;

            if (!_changeCondition)
            {
                var size = _drawTaskEditor.DrawTask(new Vector2(position.x, position.y + 20));
                if (GUI.Button(new Rect(position.x + 10, position.y + 60 + size.y, 200, 20), "Change Condition"))
                {
                    _changeCondition = true;
                }
            }
            else
            {
                var conditionTask = _guiCreateConditionTask.Draw(new Vector2(position.x, position.y + 20));
                if (conditionTask != null)
                {
                    _changeCondition = false;
                    _castedNode._condition = conditionTask;
                    _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(conditionTask.GetIl2CppType());
                    _drawTaskEditor.Task = conditionTask;
                    _drawTaskEditor.GraphEditorManager = GraphEditorManager;
                    _drawTaskEditor.Init();
                }
            }
        }
    }
}
