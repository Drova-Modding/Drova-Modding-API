using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_HasNextMPCNodeValidChoices"/>
    /// </summary>
    public class DS_HasNextMPCNodeValidChoicesTaskEditor : DrawTaskEditor
    {
        private DS_HasNextMPCNodeValidChoices? _castedTask;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_HasNextMPCNodeValidChoices>();
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
            GUI.Box(drawRect, new GUIContent("DS_HasNextMPCNodeValidChoices", "Check if Multiple Choice node following after statement Node has valid choices"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            return drawRect;
        }
    }
}