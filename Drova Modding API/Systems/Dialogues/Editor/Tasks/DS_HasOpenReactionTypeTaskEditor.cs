using System;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.Crimes;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_HasOpenReactionType"/>
    /// </summary>
    public class DS_HasOpenReactionTypeTaskEditor : DrawTaskEditor
    {
        private DS_HasOpenReactionType? _castedTask;
        private GUIDropdown? _reactionTypeDropdown;

        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask = Task.TryCast<DS_HasOpenReactionType>();
            if (_castedTask != null)
            {
                _reactionTypeDropdown = new GUIDropdown(Enum.GetNames<CrimeOpenReactionType>(), (int)_castedTask.ReactionType);
            }
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null || _reactionTypeDropdown == null)
            {
                return default;
            }

            const float width = 300f;
            const float height = 60f;
            const float rowH = 22f;

            Rect drawRect = new(position.x, position.y, width, height);

            Color previousColor = GUI.color;
            GUI.color = Color.cyan;
            GUI.Box(drawRect, "Has Open Reaction Type");
            GUI.color = previousColor;

            float y = position.y + 25f;

            GUI.Label(new Rect(position.x + 5, y, 100, rowH), "Reaction Type:");
            if (_reactionTypeDropdown.Draw(new Rect(position.x + 110, y, width - 115, rowH)))
            {
                _castedTask.ReactionType = (CrimeOpenReactionType)_reactionTypeDropdown.SelectedIndex;
            }

            return drawRect;
        }
    }
}