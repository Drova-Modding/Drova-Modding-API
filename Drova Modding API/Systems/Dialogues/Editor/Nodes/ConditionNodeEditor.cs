using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Execute the first child node if a Condition is true, or the second one if that Condition is false. The Actor selected is used for the Condition check
    /// </summary>
    internal class ConditionNodeEditor : DrawNodeEditor
    {
        private ConditionNode? _castedNode;
        private GUICreateConditionTask _guiCreateConditionTask = null!;
        private DrawTaskEditor? _drawTaskEditor;
        private bool _changeCondition;
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
            Il2CppNodeCanvas.Framework.ConditionTask condition = _castedNode._condition;
            if (condition != null)
            {
                _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(condition.GetIl2CppType());
                if (_drawTaskEditor == null)
                {
                    MelonLogger.Warning("No DrawTaskEditor found for type: " + condition.GetIl2CppType());
                    return;
                }
                _drawTaskEditor.Task = condition;
                _drawTaskEditor.GraphEditorManager = GraphEditorManager;
                _drawTaskEditor.Init();
            }
            else
            {
                _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(null);
                if (_drawTaskEditor == null) return;

                _drawTaskEditor.GraphEditorManager = GraphEditorManager;
                _drawTaskEditor.Init();
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null || _drawTaskEditor == null)
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
                Il2CppNodeCanvas.Framework.ConditionTask? conditionTask = _guiCreateConditionTask.Draw(new Vector2(position.x, position.y + 20));
                DialogueTree? dialogueTree = GraphEditorManager.DialogueTree;
                if (conditionTask != null && dialogueTree != null)
                {
                    dialogueTree.allTasks.Add(conditionTask);
                    _changeCondition = false;
                    _castedNode._condition = conditionTask;
                    _drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(conditionTask.GetIl2CppType());
                    if (_drawTaskEditor == null) return;

                    _drawTaskEditor.Task = conditionTask;
                    _drawTaskEditor.GraphEditorManager = GraphEditorManager;
                    _drawTaskEditor.Init();
                }
            }
        }
    }
}
