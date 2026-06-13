using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_WitnessedCrimeConditionTask"/>
    /// </summary>
    public class DS_WitnessedCrimeConditionTaskEditor : DrawTaskEditor
    {
        private DS_WitnessedCrimeConditionTask? _castedTask;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_WitnessedCrimeConditionTask>();
        }

        /// <inheritdoc />
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            const float width = 340f;
            const float totalHeight = 40f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_WitnessedCrimeConditionTask", "Check if owner witnessed crime of player"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            return drawRect;
        }
    }
}