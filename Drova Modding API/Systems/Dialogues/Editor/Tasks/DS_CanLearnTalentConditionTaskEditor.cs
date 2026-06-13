using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CanLearnTalentConditionTask"/>
    /// </summary>
    public class DS_CanLearnTalentConditionTaskEditor : DrawTaskEditor
    {
        private DS_CanLearnTalentConditionTask? _castedTask;
        private float _lastHeight = 100f;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CanLearnTalentConditionTask>();
        }

        /// <inheritdoc />
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float rowStep = 27f;

            float x = position.x;
            float y = position.y + rowStep;

            Color previousColor = GUI.color;
            GUI.color = Color.blue;
            Rect drawRect = new(x, position.y, width, _lastHeight);
            GUI.Box(drawRect, "Can Learn Talent Condition Task");
            GUI.color = previousColor;

            GUIContent nodeIdContent = new("NodeId", "Check if Talents, defined in following TeachTalentNode can be learned. (NodeId of a DS_TeachTalentNode)");
            GUI.Label(new Rect(x + 5, y, 100, rowH), nodeIdContent);
            
            string nodeIdString = _castedTask.NodeID.ToString();
            string newNodeIdString = GUI.TextField(new Rect(x + 110, y, width - 115, rowH), nodeIdString);
            if (newNodeIdString != nodeIdString && int.TryParse(newNodeIdString, out int newNodeId))
            {
                _castedTask.NodeID = newNodeId;
            }
            
            y += rowStep + 5;

            _lastHeight = y - position.y;
            drawRect.height = _lastHeight;
            return drawRect;
        }
    }
}