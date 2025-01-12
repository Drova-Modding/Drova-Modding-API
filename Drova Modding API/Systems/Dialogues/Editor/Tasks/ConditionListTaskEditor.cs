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
        private readonly GUICreateConditionTask _createConditionTask = new();
        private GUIDropdown _conditionsCheckModeDropdown;

        public override void Init()
        {
            _castedTask = Task.TryCast<ConditionList>();
            _conditionsCheckModeDropdown = new GUIDropdown(Enum.GetNames<ConditionsCheckMode>(), (int)_castedTask.checkMode);
            for (int i = 0; i < _castedTask.conditions.Count; i++)
            {
                var drawTaskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(_castedTask.conditions[i].GetIl2CppType());
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
            Rect rect = new(position, new Vector2(250, (_drawTaskEditors.Count * 30) + 100));
            GUI.Box(rect, "Condition List");
            for (int i = 0; i < _drawTaskEditors.Count; i++)
            {
                _drawTaskEditors[i].DrawTask(new Vector2(position.x, position.y + 20 + i * 20));
            }

            var task = _createConditionTask.Draw(new Vector2(position.x, position.y + (_drawTaskEditors.Count * 30)));
            if (task != null)
            {
                var editor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(task.GetIl2CppType());
                editor.Task = task;
                editor.GraphEditorManager = GraphEditorManager;
                editor.Init();
                _drawTaskEditors.Add(editor);
                _castedTask.conditions.Add(task);
            }

            if (_conditionsCheckModeDropdown.Draw(new Rect(position.x, position.y + (_drawTaskEditors.Count * 30) + 50, 200, 20)))
            {
                _castedTask.checkMode = (ConditionsCheckMode)_conditionsCheckModeDropdown.SelectedIndex;
            }

            return rect;
        }
    }
}
