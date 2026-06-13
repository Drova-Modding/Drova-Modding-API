using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CheckAndResetGlobalBBVarTask"/>
    /// </summary>
    public class DS_CheckAndResetGlobalBBVarTaskEditor : DrawTaskEditor
    {
        private DS_CheckAndResetGlobalBBVarTask? _castedTask;
        
        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CheckAndResetGlobalBBVarTask>();
            if (_castedTask == null) return;

            var allParams = GraphEditorManager.DialogueTree!.allParameters;
            if (!allParams.Contains(_castedTask.IsBoolSet))
            {
                allParams.Add(_castedTask.IsBoolSet);
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
            float totalHeight = 60f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_CheckAndResetGlobalBBVar", "Check then Reset a variable in global variables"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "IsBoolSet:");
            
            _castedTask.IsBoolSet.value = GUI.Toggle(new Rect(x + 110, y, 100, rowH), _castedTask.IsBoolSet.value, "");

            return drawRect;
        }
    }
}