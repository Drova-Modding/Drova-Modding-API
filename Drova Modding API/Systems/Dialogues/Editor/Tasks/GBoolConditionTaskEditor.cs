using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.GlobalVarSystem.Graphs;
using Il2CppDrova.QuestSystem.Graphs;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    internal class GBoolConditionTaskEditor : DrawTaskEditor
    {
        private GBoolConditionTask _castedTask;

        private readonly List<GUIDropdown> _comparerDropdowns = [];
        private readonly List<GUIGvarSelectionEditor> _gvarEditors = [];

        public override void Init()
        {
            _castedTask ??= Task.TryCast<GBoolConditionTask>();
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                var task = _castedTask.Conditions[i];
                _comparerDropdowns.Add(new GUIDropdown(Enum.GetNames<GBool.Comparer>(), (int)task.Comparison.value));

                _gvarEditors.Add(new GUIGvarSelectionEditor(GvarType.BOOL, task.Variable.GetValue().GetParent().name, false, task.Variable.GetValue()));
            }
        }

        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            Rect rect = new(position.x, position.y, 220, 20);
            Rect size = new(position.x, position.y, 220, 20);
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                var condition = _castedTask.Conditions[i];
                var comparerDropdown = _comparerDropdowns[i];
                var gvarEditor = _gvarEditors[i];
                

                if(GUI.Button(rect, "Remove"))
                {
                    _castedTask.Conditions.RemoveAt(i);
                    _comparerDropdowns.RemoveAt(i);
                    _gvarEditors.RemoveAt(i);
                    i--;
                    continue;
                }

                rect.y += 20;

                Rect gvarRect = new(position.x, rect.y, 220, 20);

                if (gvarEditor.DrawGvarEditor(rect, gvarRect))
                {
                    condition.Variable.SetValue(gvarEditor.CurrentSelectedGvar.TryCast<GBool>());
                }
                rect.y += 60;
                if (comparerDropdown.Draw(rect))
                {
                    condition.Comparison = (GBool.Comparer)comparerDropdown.SelectedIndex;
                }
                rect.y += 20;
                condition.Value.value = GUI.Toggle(rect, condition.Value.value, "Compare value");

                rect.y += 30;

                size.height += 130;
            }

            if (GUI.Button(rect, "Add Condition"))
            {
                _castedTask.Conditions.Add(new GraphGBoolCompService());
                _comparerDropdowns.Add(new GUIDropdown(Enum.GetNames<GBool.Comparer>(), 0));
                _gvarEditors.Add(new GUIGvarSelectionEditor(GvarType.BOOL));
            }

            size.height += 30;

            return size;
        }
    }
}
