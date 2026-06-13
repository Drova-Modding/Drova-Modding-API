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

        private GIntConditionTask? _castedTask;
        private readonly List<GUIDropdown> _comparerDropdowns = [];
        private readonly List<GUIGvarSelectionEditor> _gvarEditors = [];

        public override void Init()
        {
            _castedTask ??= Task.TryCast<GIntConditionTask>();
            var allpParams = GraphEditorManager.DialogueTree!.allParameters;
            for (int i = 0; i < _castedTask!.Conditions.Count; i++)
            {
                GraphGIntCompService task = _castedTask.Conditions[i];
                if (!allpParams.Contains(task.Comparison))
                {
                    allpParams.Add(task.Comparison);
                }
                if (!allpParams.Contains(task.Value))
                {
                    allpParams.Add(task.Value);
                }
                if (!allpParams.Contains(task.GIntValue))
                {
                    allpParams.Add(task.GIntValue);
                }
                if (!allpParams.Contains(task.Variable))
                {
                    allpParams.Add(task.Variable);
                }
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
                GraphGIntCompService condition = _castedTask.Conditions[i];
                GUIDropdown comparerDropdown = _comparerDropdowns[i];
                GUIGvarSelectionEditor gvarEditor = _gvarEditors[i];

                if (GUI.Button(new Rect(position.x, rect.y + 100, 120, 20), "Remove"))
                {
                    GraphEditorManager.DialogueTree!.allParameters.Remove(condition.Variable);
                    GraphEditorManager.DialogueTree.allParameters.Remove(condition.GIntValue);
                    GraphEditorManager.DialogueTree.allParameters.Remove(condition.Comparison);
                    GraphEditorManager.DialogueTree.allParameters.Remove(condition.Value);
                    _castedTask.Conditions.RemoveAt(i);
                    _comparerDropdowns.RemoveAt(i);
                    _gvarEditors.RemoveAt(i);
                    i--;
                    continue;
                }

                Rect gvarRect = new(position.x, rect.y + 20, 220, 20);

                if (gvarEditor.DrawGvarEditor(rect, gvarRect))
                {
                    condition.Variable.SetValue(gvarEditor.CurrentSelectedGvar!.TryCast<GInt>());
                }
                rect.y += 60;
                if (comparerDropdown.Draw(rect))
                {
                    condition.Comparison.SetValue((GInt.Comparer)comparerDropdown.SelectedIndex);

                }
                rect.y += 20;

                GUI.Label(new Rect(rect.x + 5, rect.y, 70, 20), "Value:");
                string tempInputValue = GUI.TextField(new Rect(rect.x + 80, rect.y, 220 - 70, 20), condition.Value.value.ToString());
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
                GraphGIntCompService service = new()
                {
                    Value = 0, Comparison = GInt.Comparer.Equals, GIntValue = default
                };
                var allpParams = GraphEditorManager.DialogueTree!.allParameters;
                allpParams.Add(service.Comparison);
                allpParams.Add(service.Value);
                allpParams.Add(service.GIntValue);
                allpParams.Add(service.Variable);
                _castedTask.Conditions.Add(service);
                _comparerDropdowns.Add(new GUIDropdown(Enum.GetNames<GInt.Comparer>(), 0));
                _gvarEditors.Add(new GUIGvarSelectionEditor(GvarType.INT));
            }

            size.height += 30;

            Color previousColor = GUI.color;

            GUI.color = Color.blue;
            Rect drawRect = new(position.x, position.y, 380, size.height);
            GUI.Box(drawRect, "GIntConditionTask");

            GUI.color = previousColor;

            return drawRect;
        }
    }
}