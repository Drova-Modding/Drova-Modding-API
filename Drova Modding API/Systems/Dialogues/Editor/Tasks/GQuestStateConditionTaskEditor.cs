using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.GlobalVarSystem.Graphs;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.QuestSystem.Graphs;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNodeCanvas.Framework;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Editor for GQuestStateConditionTask.
    /// Comparison field is a BBParameter&lt;AGEnum&lt;QuestState&gt;.Comparer&gt; — Il2CppInterop
    /// fails to initialise the class pointer for this nested-in-generic enum, so we go through
    /// Il2CppSystem reflection and the non-generic <see cref="BBParameter"/> base instead of
    /// referencing the typed wrapper directly.
    /// </summary>
    public class GQuestStateConditionTaskEditor : DrawTaskEditor
    {
        private GQuestStateConditionTask? _castedTask;
        private readonly List<GUIGvarSelectionEditor> _gvarSelectionEditors = [];
        private readonly List<GUIDropdown> _comparisionDropdowns = [];
        private readonly List<GUIDropdown> _valueDropdown = [];
        private readonly List<BBParameter> _comparisonParams = [];

        private static Il2CppSystem.Reflection.FieldInfo? _comparisonField;
        private static Il2CppSystem.Reflection.FieldInfo ComparisonField =>
            _comparisonField ??= Il2CppType.Of<GraphGQuestCompService>().GetField("Comparison",
                Il2CppSystem.Reflection.BindingFlags.Instance |
                Il2CppSystem.Reflection.BindingFlags.Public |
                Il2CppSystem.Reflection.BindingFlags.NonPublic);

        private static BBParameter ReadComparison(GraphGQuestCompService condition)
        {
            if (ComparisonField == null) return null;
            Il2CppSystem.Object boxed = ComparisonField.GetValue(condition);
            return boxed?.Cast<BBParameter>();
        }

        private static (string[] names, int index) ReadEnumState(BBParameter param)
        {
            if (param == null || param.varType == null) return ([], -1);
            Il2CppStringArray il2cppNames = Il2CppSystem.Enum.GetNames(param.varType);
            string[] names = new string[il2cppNames.Length];
            for (int n = 0; n < il2cppNames.Length; n++) names[n] = il2cppNames[n];
            int index = param.value != null ? Il2CppSystem.Convert.ToInt32(param.value) : -1;
            return (names, index);
        }

        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<GQuestStateConditionTask>();
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                GraphGQuestCompService condition = _castedTask.Conditions[i];
                _gvarSelectionEditors.Add(new GUIGvarSelectionEditor(GvarType.QUEST, condition.Variable?.GetValue().GetParent().name, false, condition.Variable?.GetValue()));

                BBParameter bbComparison = ReadComparison(condition);
                _comparisonParams.Add(bbComparison);
                (string[] comparerNames, int comparerIndex) = ReadEnumState(bbComparison);
                _comparisionDropdowns.Add(new GUIDropdown(comparerNames, comparerIndex));

                int valueIndex = condition.Value != null ? (int)condition.Value?.GetValue() : -1;
                _valueDropdown.Add(new GUIDropdown(Enum.GetNames<QuestState>(), valueIndex));
            }
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            float totalHeight = (120 * _castedTask.Conditions.Count) + 120;

            Color previousColor = GUI.color;
            Color previousBg = GUI.backgroundColor;

            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.4f, 0.15f, 0.45f, 0.95f);
            Rect drawRect = new(position.x, position.y, 380, totalHeight);
            GUI.Box(drawRect, "GQuestStateConditionTask");
            GUI.backgroundColor = previousBg;

            Rect rect = new(position.x, position.y + 20, 220, 20);
            Rect size = new(position.x, position.y, 380, totalHeight);
            for (int i = 0; i < _castedTask.Conditions.Count; i++)
            {
                GraphGQuestCompService condition = _castedTask.Conditions[i];
                GUIGvarSelectionEditor gvarEditor = _gvarSelectionEditors[i];
                var comparerDropdown = _comparisionDropdowns[i];
                GUIDropdown valueDropdown = _valueDropdown[i];
                BBParameter bbComparison = _comparisonParams[i];
                if (GUI.Button(new Rect(position.x, rect.y + 100, 120, 20), "Remove"))
                {
                    _castedTask.Conditions.RemoveAt(i);
                    _gvarSelectionEditors.RemoveAt(i);
                    _comparisionDropdowns.RemoveAt(i);
                    _valueDropdown.RemoveAt(i);
                    _comparisonParams.RemoveAt(i);
                    i--;
                    continue;
                }
                Rect gvarRect = new(position.x, rect.y + 40, 220, 20);
                if (gvarEditor.DrawGvarEditor(rect, gvarRect))
                {
                    condition.Variable.SetValue(gvarEditor.CurrentSelectedGvar.TryCast<GQuestState>());
                }
                rect.y += 60;
                if (comparerDropdown.Draw(rect) && bbComparison?.varType != null)
                {
                    bbComparison.value = Il2CppSystem.Enum.ToObject(bbComparison.varType, comparerDropdown.SelectedIndex);
                }
                rect.y += 20;
                if (valueDropdown.Draw(rect))
                {
                    condition.Value = (QuestState)valueDropdown.SelectedIndex;
                }
            }
            // Add Button
            if (GUI.Button(new Rect(position.x, rect.y + 100, 120, 20), "Add"))
            {
                GraphGQuestCompService newCondition = new();
                _castedTask.Conditions.Add(newCondition);
                _gvarSelectionEditors.Add(new GUIGvarSelectionEditor(GvarType.QUEST, newCondition.Variable.GetValue().GetParent().name, false, newCondition.Variable.GetValue()));

                BBParameter bbComparison = ReadComparison(newCondition);
                _comparisonParams.Add(bbComparison);
                (string[] comparerNames, int comparerIndex) = ReadEnumState(bbComparison);
                _comparisionDropdowns.Add(new GUIDropdown(comparerNames, comparerIndex));

                _valueDropdown.Add(new GUIDropdown(Enum.GetNames<QuestState>(), (int)newCondition.Value.GetValue()));
            }

            GUI.color = previousColor;

            return size;
        }
    }
}
