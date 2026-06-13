using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GlobalVarSystem;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CheckAndResetGBoolConditionTask"/>
    /// </summary>
    public class DS_CheckAndResetGBoolConditionTaskEditor : DrawTaskEditor
    {
        private DS_CheckAndResetGBoolConditionTask? _castedTask;
        private GUIGvarSelectionEditor? _gvarEditor;

        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CheckAndResetGBoolConditionTask>();
            if (_castedTask == null) return;

            var allParams = GraphEditorManager.DialogueTree!.allParameters;
            if (!allParams.Contains(_castedTask.IsBoolSet))
            {
                allParams.Add(_castedTask.IsBoolSet);
            }

            GBool? variable = _castedTask.IsBoolSet.GetValue();
            _gvarEditor = new GUIGvarSelectionEditor(GvarType.BOOL, variable?.GetParent()?.name, false, variable);
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null || _gvarEditor == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float rowStep = 27f;
            float totalHeight = 80f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_CheckAndResetGBool", "Check then Reset a variable in global variables"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "IsBoolSet:");

            Rect gvarListRect = new(x + 110, y, width - 120, rowH);
            Rect gvarRect = new(x + 110, y + rowStep, width - 120, rowH);

            if (_gvarEditor.DrawGvarEditor(gvarListRect, gvarRect))
            {
                _castedTask.IsBoolSet.SetValue(_gvarEditor.CurrentSelectedGvar!.TryCast<GBool>());
            }

            return drawRect;
        }
    }
}