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
        private MultipleConditionNode? _castedNode;
        private readonly List<DrawTaskEditor> _drawTaskEditors = [];
        private readonly GUICreateConditionTask _guiCreateConditionTask = new();
        private const int SpaceBetweenTasks = 50;

        public MultipleConditionNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 500);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<MultipleConditionNode>();
            if (_castedNode == null) return;
            for (int i = 0; i < _castedNode.Conditions.Count; i++)
            {
                Il2CppNodeCanvas.Framework.ConditionTask condition = _castedNode.Conditions[i];
                if (condition != null)
                {
                    DrawTaskEditor? editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(condition.GetIl2CppType());
                    if (editor == null) continue;

                    _drawTaskEditors.Add(editor);
                    editor.Task = condition;
                    editor.GraphEditorManager = GraphEditorManager;
                    editor.Init();
                }
                else
                {
                    DrawTaskEditor? editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(null);
                    if (editor == null) continue;

                    _drawTaskEditors.Add(editor);
                    editor.GraphEditorManager = GraphEditorManager;
                    editor.Init();
                }
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.color = Color.green;
            GUI.depth = 20;
            GUI.Box(new Rect(position.x, position.y, NodeSizeInternal.x, NodeSizeInternal.y), "MultipleConditionNode");

            GUI.depth = 10;
            GUI.color = Color.white;
            Vector2 taskStartPosition = new(position.x, position.y + 30);
            Rect rect = new(position.x, position.y, 350, 70);

            for (int i = 0; i < _drawTaskEditors.Count; i++)
            {
                DrawTaskEditor taskEditor = _drawTaskEditors[i];
                Rect size = taskEditor.DrawTask(taskStartPosition);
                taskStartPosition.y += size.height + SpaceBetweenTasks;
                rect.height += size.height + SpaceBetweenTasks;
                if (size.width > rect.width)
                {
                    rect.width = size.width + 20;
                }
            }

            Il2CppNodeCanvas.Framework.ConditionTask? conditionTask = _guiCreateConditionTask.Draw(new Vector2(position.x, position.y + rect.height));
            DialogueTree? dialogueTree = GraphEditorManager.DialogueTree;
            if (conditionTask != null && dialogueTree != null)
            {
                dialogueTree.allTasks.Add(conditionTask);
                _castedNode.Conditions.Add(conditionTask);
                DrawTaskEditor? editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(conditionTask.GetIl2CppType());
                if (editor != null)
                {
                    _drawTaskEditors.Add(editor);
                    editor.Task = conditionTask;
                    editor.GraphEditorManager = GraphEditorManager;
                    editor.Init();
                }
            }

            rect.height += _guiCreateConditionTask.Size.y + SpaceBetweenTasks;

            NodeSizeInternal = new Vector2(rect.width, rect.height);

            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }
    }
}
