using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.GlobalVarSystem.Graphs;
using Il2CppDrova.QuestSystem.Graphs;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="GIntConditionTask"/>
    /// </summary>
    internal class GIntConditionTaskEditor : DrawTaskEditor
    {

        private GIntConditionTask _castedTask;
        private readonly List<GUIDropdown> _comparerDropdowns = [];
        private readonly List<GUIGvarSelectionEditor> _gvarEditors = [];

        public override void Init()
        {
            _castedTask ??= Task.TryCast<GIntConditionTask>();
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                var task = _castedTask.Conditions[i];
                _comparerDropdowns.Add(new GUIDropdown(Enum.GetNames<GInt.Comparer>(), (int)task.Comparison.value));
                _gvarEditors.Add(new GUIGvarSelectionEditor(GvarType.INT, task.Variable.GetValue().GetParent().name, false, task.Variable.GetValue()));
            }
        }
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            Rect rect = new(position.x, position.y + 20, 220, 20);
            Rect size = new(position.x, position.y, 220, 40);
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                var condition = _castedTask.Conditions[i];
                var comparerDropdown = _comparerDropdowns[i];
                var gvarEditor = _gvarEditors[i];


                if (GUI.Button(new Rect(position.x, rect.y + 100, 120, 20), "Remove"))
                {
                    _castedTask.Conditions.RemoveAt(i);
                    _comparerDropdowns.RemoveAt(i);
                    _gvarEditors.RemoveAt(i);
                    i--;
                    continue;
                }

                Rect gvarRect = new(position.x, rect.y + 20, 220, 20);

                if (gvarEditor.DrawGvarEditor(rect, gvarRect))
                {
                    condition.Variable.SetValue(gvarEditor.CurrentSelectedGvar.TryCast<GInt>());
                }
                rect.y += 60;
                if (comparerDropdown.Draw(rect))
                {
                    condition.Comparison = (GInt.Comparer)comparerDropdown.SelectedIndex;
                }
                rect.y += 20;

                GUI.Label(new Rect(rect.x + 5, rect.y, 70, 20), "Value:");
                var tempInputValue = GUI.TextField(new Rect(rect.x + 80, rect.y, 220 - 70, 20), condition.Value.value.ToString());
                if (int.TryParse(tempInputValue, out int result))
                {
                    condition.Value.value = result;
                }

                rect.y += 30;

                size.height += 130;
            }

            rect.y += 30;

            if (GUI.Button(rect, "Add Condition"))
            {
                var service = new GraphGIntCompService
                {
                    Value = 0,
                    Comparison = GInt.Comparer.Equals,
                    GIntValue = default
                };
                _castedTask.Conditions.Add(service);
                _comparerDropdowns.Add(new GUIDropdown(Enum.GetNames<GInt.Comparer>(), 0));
                _gvarEditors.Add(new GUIGvarSelectionEditor(GvarType.INT));
            }

            size.height += 30;

            Color previousColor = GUI.color;

            GUI.color = Color.blue;
            var drawRect = new Rect(position.x, position.y, 380, size.height);
            GUI.Box(drawRect, "GIntConditionTask");

            GUI.color = previousColor;

            return drawRect;
        }
    }
}
