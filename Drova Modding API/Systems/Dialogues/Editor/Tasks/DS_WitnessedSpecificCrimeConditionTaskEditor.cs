using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.Crimes;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_WitnessedSpecificCrimeConditionTask"/>
    /// </summary>
    public class DS_WitnessedSpecificCrimeConditionTaskEditor : DrawTaskEditor
    {
        private DS_WitnessedSpecificCrimeConditionTask? _castedTask;
        private GUIDropdown? _crimeTypeDropdown;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_WitnessedSpecificCrimeConditionTask>();
            if (_castedTask == null) return;

            _crimeTypeDropdown = new GUIDropdown(System.Enum.GetNames<CrimeType>(), (int)_castedTask.CrimeType);
        }

        /// <inheritdoc />
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null || _crimeTypeDropdown == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float totalHeight = 60f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_WitnessedSpecificCrimeConditionTask", "Check if owner witnessed a specific crime of player"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "CrimeType:");
            if (_crimeTypeDropdown.Draw(new Rect(x + 110, y, width - 120, rowH)))
            {
                _castedTask.CrimeType = (CrimeType)_crimeTypeDropdown.SelectedIndex;
            }

            return drawRect;
        }
    }
}