using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CheckBbIntegerConditionTask"/>
    /// </summary>
    public class DS_CheckBbIntegerConditionTaskEditor : DrawTaskEditor
    {
        private DS_CheckBbIntegerConditionTask? _castedTask;

        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CheckBbIntegerConditionTask>();
            if (_castedTask == null) return;

            var allParams = GraphEditorManager.DialogueTree!.allParameters;
            if (!allParams.Contains(_castedTask.Integer))
            {
                allParams.Add(_castedTask.Integer);
            }
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            float totalHeight = 85f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_CheckBbIntegerCondition", "Check a BlackBoard Integer"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "Integer:");
            string integerInput = GUI.TextField(new Rect(x + 110, y, 220, rowH), _castedTask.Integer.value.ToString());
            if (int.TryParse(integerInput, out int integerResult))
            {
                _castedTask.Integer.value = integerResult;
            }

            y += rowH + 5;

            GUI.Label(new Rect(x, y, 100, rowH), "CompareTo:");
            string compareToInput = GUI.TextField(new Rect(x + 110, y, 220, rowH), _castedTask.CompareTo.ToString());
            if (int.TryParse(compareToInput, out int compareToResult))
            {
                _castedTask.CompareTo = compareToResult;
            }

            return drawRect;
        }
    }
}