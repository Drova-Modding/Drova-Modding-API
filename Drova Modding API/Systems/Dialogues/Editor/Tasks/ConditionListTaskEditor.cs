using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.Framework;
using UnityEngine;
using static Il2CppNodeCanvas.Framework.ConditionList;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="ConditionList"/>
    /// </summary>
    internal class ConditionListTaskEditor : DrawTaskEditor
    {
        private ConditionList _castedTask;
        private readonly List<DrawTaskEditor> _drawTaskEditors = [];
        private readonly Dictionary<int, float> _taskHeights = [];
        private readonly GUICreateConditionTask _createConditionTask = new();
        private GUIDropdown _conditionsCheckModeDropdown;
        private const int HeaderHeight = 20;
        private const int Padding = 10;
        private const int DropdownHeight = 20;

        public override void Init()
        {
            _castedTask = Task.TryCast<ConditionList>();
            _conditionsCheckModeDropdown = new GUIDropdown(Enum.GetNames<ConditionsCheckMode>(), (int)_castedTask.checkMode);
            for (int i = 0; i < _castedTask.conditions.Count; i++)
            {
                DrawTaskEditor drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(_castedTask.conditions[i].GetIl2CppType());
                if (drawTaskEditor != null)
                {
                    drawTaskEditor.Task = _castedTask.conditions[i];
                    drawTaskEditor.GraphEditorManager = GraphEditorManager;
                    drawTaskEditor.Init();
                    _drawTaskEditors.Add(drawTaskEditor);
                }
            }
        }

        public override Rect DrawTask(Vector2 position)
        {
            float totalHeight = HeaderHeight;
            float maxWidth = 250;
            for (int i = 0; i < _drawTaskEditors.Count; i++)
            {
                if (_taskHeights.TryGetValue(i, out float cached))
                    totalHeight += cached + Padding;
            }
            float selectionHeight = _createConditionTask.Size.y;
            totalHeight += selectionHeight + Padding + DropdownHeight + Padding;

            Rect rect = new(position, new Vector2(maxWidth, totalHeight));
            GUI.Box(rect, "Condition List");

            float yOffset = HeaderHeight;
            for (int i = 0; i < _drawTaskEditors.Count; i++)
            {
                Rect drawn = _drawTaskEditors[i].DrawTask(new Vector2(position.x, position.y + yOffset));
                _taskHeights[i] = drawn.height;
                if (drawn.width > maxWidth) maxWidth = drawn.width;
                yOffset += drawn.height + Padding;
            }

            ConditionTask task = _createConditionTask.Draw(new Vector2(position.x, position.y + yOffset));
            if (task != null)
            {
                GraphEditorManager.DialogueTree.allTasks.Add(task);
                DrawTaskEditor editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(task.GetIl2CppType());
                editor.Task = task;
                editor.GraphEditorManager = GraphEditorManager;
                editor.Init();
                _drawTaskEditors.Add(editor);
                _castedTask.conditions.Add(task);
            }
            yOffset += selectionHeight + Padding;

            if (_conditionsCheckModeDropdown.Draw(new Rect(position.x, position.y + yOffset, 200, DropdownHeight)))
            {
                _castedTask.checkMode = (ConditionsCheckMode)_conditionsCheckModeDropdown.SelectedIndex;
            }

            rect.width = maxWidth;
            return rect;
        }
    }
}
