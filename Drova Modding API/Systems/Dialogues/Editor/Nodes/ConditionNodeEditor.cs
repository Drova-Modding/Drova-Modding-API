using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class ConditionNodeEditor : DrawNodeEditor
    {
        private ConditionNode? _castedNode;
        private GUICreateConditionTask _guiCreateConditionTask;
        private DrawTaskEditor _drawTaskEditor;
        private bool _changeCondition = false;
        private float _lastTaskHeight = 60;

        public ConditionNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 120);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<ConditionNode>();
            if (_castedNode == null) return;
            _guiCreateConditionTask = new GUICreateConditionTask();
            if (_castedNode._condition != null)
            {
                _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(_castedNode._condition.GetIl2CppType());
                if (_drawTaskEditor == null)
                {
                    MelonLogger.Warning("No DrawTaskEditor found for type: " + _castedNode._condition.GetIl2CppType());
                    return;
                }
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

            float contentHeight = _changeCondition
                ? _guiCreateConditionTask.Size.y
                : _lastTaskHeight + 30;
            float boxHeight = contentHeight + 30;
            Rect rect = new(position.x, position.y, 350, boxHeight);
            NodeSizeInternal = new Vector2(350, boxHeight);

            Color previousColor = GUI.color;
            Color previousBg = GUI.backgroundColor;
            GUI.color = Color.green;
            GUI.backgroundColor = Color.clear;
            GUI.Box(rect, "ConditionNode");
            GUI.color = previousColor;
            GUI.backgroundColor = previousBg;

            if (!_changeCondition)
            {
                Rect size = _drawTaskEditor.DrawTask(new Vector2(position.x, position.y + 20));
                _lastTaskHeight = size.height;
                if (GUI.Button(new Rect(position.x + 10, position.y + 30 + size.height, 200, 20), "Change Condition"))
                {
                    _changeCondition = true;
                }
            }
            else
            {
                Il2CppNodeCanvas.Framework.ConditionTask conditionTask = _guiCreateConditionTask.Draw(new Vector2(position.x, position.y + 20));
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
