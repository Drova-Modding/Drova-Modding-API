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
                GraphGBoolCompService task = _castedTask.Conditions[i];
                int comparerIndex = task.Comparison != null ? (int)task.Comparison.value : 0;
                _comparerDropdowns.Add(new GUIDropdown(Enum.GetNames<GBool.Comparer>(), comparerIndex));

                GBool variable = task.Variable?.GetValue();
                _gvarEditors.Add(new GUIGvarSelectionEditor(GvarType.BOOL, variable?.GetParent()?.name, false, variable));
            }
        }

        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            int conditionCount = _castedTask.Conditions.Count;
            float totalHeight = 70 + (130 * conditionCount);

            Color previousColor = GUI.color;
            Color previousBg = GUI.backgroundColor;

            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.15f, 0.2f, 0.45f, 0.95f);
            Rect drawRect = new(position.x, position.y, 380, totalHeight);
            GUI.Box(drawRect, "GBoolConditionTask");
            GUI.backgroundColor = previousBg;

            Rect rect = new(position.x, position.y + 20, 220, 20);
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                GraphGBoolCompService condition = _castedTask.Conditions[i];
                GUIDropdown comparerDropdown = _comparerDropdowns[i];
                GUIGvarSelectionEditor gvarEditor = _gvarEditors[i];

                if (GUI.Button(new Rect(position.x, rect.y + 100, 120, 20), "Remove"))
                {
                    _castedTask.Conditions.RemoveAt(i);
                    _comparerDropdowns.RemoveAt(i);
                    _gvarEditors.RemoveAt(i);
                    i--;
                    continue;
                }

                Rect gvarRect = new(position.x, rect.y + 40, 220, 20);

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
            }

            rect.y += 30;

            if (GUI.Button(rect, "Add Condition"))
            {
                _castedTask.Conditions.Add(new GraphGBoolCompService());
                _comparerDropdowns.Add(new GUIDropdown(Enum.GetNames<GBool.Comparer>(), 0));
                _gvarEditors.Add(new GUIGvarSelectionEditor(GvarType.BOOL));
            }

            GUI.color = previousColor;

            return drawRect;
        }
    }
}
