using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.GlobalVarSystem.Graphs;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.QuestSystem.Graphs;
using Il2CppNodeCanvas.Framework;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Editor for GQuestStateConditionTask.
    /// TODO: Hope the devs are moving the Enum to a better place :D
    /// </summary>
    public class GQuestStateConditionTaskEditor : DrawTaskEditor
    {
        private GQuestStateConditionTask _castedTask;
        private readonly List<GUIGvarSelectionEditor> _gvarSelectionEditors = [];
        //private readonly List<GUIDropdown> _comparisionDropdowns = [];
        private readonly List<GUIDropdown> _valueDropdown = [];

        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<GQuestStateConditionTask>();
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                GraphGQuestCompService condition = _castedTask.Conditions[i];
                _gvarSelectionEditors.Add(new GUIGvarSelectionEditor(GvarType.QUEST, condition.Variable.GetValue().GetParent().name, false, condition.Variable.GetValue()));

                //BBParameter<AGEnum<QuestState>.Comparer> comparision = condition.Comparison;
                //_comparisionDropdowns.Add(new GUIDropdown(Enum.GetNames<AGEnum<QuestState>.Comparer>(), (int)comparision.TryCast<BBParameter<GQuestState.Comparer>>().GetValue()));
                _valueDropdown.Add(new GUIDropdown(Enum.GetNames<QuestState>(), (int)condition.Value.GetValue()));
            }
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }
            Rect rect = new(position.x, position.y + 20, 220, 20);
            Rect size = new(position.x, position.y, 220, (120 * _castedTask.Conditions.Count) + 120);
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                GraphGQuestCompService condition = _castedTask.Conditions[i];
                GUIGvarSelectionEditor gvarEditor = _gvarSelectionEditors[i];
                //var comparerDropdown = _comparisionDropdowns[i];
                GUIDropdown valueDropdown = _valueDropdown[i];
                if (GUI.Button(new Rect(position.x, rect.y + 100, 120, 20), "Remove"))
                {
                    _castedTask.Conditions.RemoveAt(i);
                    _gvarSelectionEditors.RemoveAt(i);
                    //_comparisionDropdowns.RemoveAt(i);
                    _valueDropdown.RemoveAt(i);
                    i--;
                    continue;
                }
                Rect gvarRect = new(position.x, rect.y + 40, 220, 20);
                if (gvarEditor.DrawGvarEditor(rect, gvarRect))
                {
                    condition.Variable.SetValue(gvarEditor.CurrentSelectedGvar.TryCast<GQuestState>());
                }
                rect.y += 60;
                //if (comparerDropdown.Draw(rect))
                //{
                //    condition.Comparison = (AGEnum<QuestState>.Comparer)comparerDropdown.SelectedIndex;
                //}
                //rect.y += 20;
                if (valueDropdown.Draw(rect))
                {
                    condition.Value = (QuestState)valueDropdown.SelectedIndex;
                }
            }
            // Add Button
            if (GUI.Button(new Rect(position.x, rect.y + 100, 120, 20), "Add"))
            {
                _castedTask.Conditions.Add(new GraphGQuestCompService());
                _gvarSelectionEditors.Add(new GUIGvarSelectionEditor(GvarType.QUEST, _castedTask.Conditions[^1].Variable.GetValue().GetParent().name, false, _castedTask.Conditions[^1].Variable.GetValue()));
                //_comparisionDropdowns.Add(new GUIDropdown(Enum.GetNames<AGEnum<QuestState>.Comparer>(), (int)_castedTask.Conditions[^1].Comparison.GetValue()));
                _valueDropdown.Add(new GUIDropdown(Enum.GetNames<QuestState>(), (int)_castedTask.Conditions[^1].Value.GetValue()));
            }
            return size;
        }
    }
}
