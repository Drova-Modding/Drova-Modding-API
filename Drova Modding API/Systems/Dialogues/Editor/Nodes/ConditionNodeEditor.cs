using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;

using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class ConditionNodeEditor : DrawNodeEditor
    {
        private ConditionNode _castedNode;
        private GUICreateConditionTask _guiCreateConditionTask;
        private DrawTaskEditor _drawTaskEditor;

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

            if (_castedNode._condition != null)
            {
                _drawTaskEditor.DrawTask(new Vector2(position.x, position.y + 20));
                if(GUI.Button(new Rect(position.x + 10, position.y + 60, 200, 20), "Change Condition"))
                {
                    _castedNode._condition = null;
                }
            }
            else
            {
                var conditionTask = _guiCreateConditionTask.Draw(new Vector2(position.x, position.y + 20));
                if (conditionTask != null)
                {
                    _castedNode._condition = conditionTask;
                    _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(_castedNode._condition.GetIl2CppType());
                }
            }
        }
    }
}
