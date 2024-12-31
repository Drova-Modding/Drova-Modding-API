using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    internal class DS_CheckGVarListConditionTaskEditor : DrawTaskEditor
    {
        private DS_CheckGVarListConditionTask _castedTask;
        private GUIGvarSelectionEditor _gvarSelectionEditor;

        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CheckGVarListConditionTask>();
            _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.NONE, _castedTask.Target.name, true);
        }

        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            Rect rect = new(position.x, position.y, 220, 20);


            if (_gvarSelectionEditor.DrawGvarEditor(rect))
            {
                _castedTask.Target = _gvarSelectionEditor.CurrentSelectedGvarList;
            }

            rect.height += 20 * 20; // 20 is the height of the dropdown list * 20 is the number of elements in the list

            return rect;
        }
    }
}
