using Il2Cpp;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Editor for the <see cref="Il2Cpp.DS_IsUniqueConditionTask"/>. Used with <see cref="Il2CppNodeCanvas.DialogueTrees.MultipleChoiceNode.Choice"/>
    /// </summary>
    public class DS_IsUniqueConditionTaskEditor : DrawTaskEditor
    {

        private DS_IsUniqueConditionTask? _task;

        /// <inheritdoc/>
        public override void Init()
        {
            _task = this.Task.TryCast<DS_IsUniqueConditionTask>();
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_task == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float rowStep = 27f;

            GUI.color = Color.blue;
            Rect drawRect = new(position.x, position.y, width, rowStep * 2 + 5);
            GUI.Box(drawRect, "DS_IsUniqueConditionTask");
            GUI.color = Color.white;

            float x = position.x + 5;
            float y = position.y + rowStep;

            GUI.Label(new Rect(x, y, 100, rowH), "Choice UID:");
            _task.choiceUID = GUI.TextField(new Rect(x + 105, y, width - 115, rowH), _task.choiceUID);

            return drawRect;
        }
    }
}
