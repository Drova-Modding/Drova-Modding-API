using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GlobalVarSystem;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CheckGVarListConditionTask"/>
    /// </summary>
    internal class DS_CheckGVarListConditionTaskEditor : DrawTaskEditor
    {
        private DS_CheckGVarListConditionTask? _castedTask;
        private GUIGvarSelectionEditor? _gvarSelectionEditor;

        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CheckGVarListConditionTask>();
            if (_castedTask == null) return;

            GVarList? target = _castedTask.Target;
            _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.NONE, target == null ? null : target.name, true);
        }

        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null || _gvarSelectionEditor == null)
            {
                return default;
            }

            Rect rect = new(position.x, position.y, 220, 20);

            if (_gvarSelectionEditor.DrawGvarEditor(new(rect.x, rect.y + 20, rect.width, rect.height)))
            {
                _castedTask.Target = _gvarSelectionEditor.CurrentSelectedGvarList;
            }

            // An open list dropdown shows up to 20 rows of 20 px.
            rect.height += 20 * (_gvarSelectionEditor.GvarListDropdown.IsDropdownShown ? 20 : 1);
            Color previousColor = GUI.color;

            GUI.color = Color.blue;

            Rect drawRect = new(position.x, position.y, 400, rect.height);

            GUI.Box(drawRect, "Check GVar List Condition Task");

            GUI.color = previousColor;

            return drawRect;
        }
    }
}
