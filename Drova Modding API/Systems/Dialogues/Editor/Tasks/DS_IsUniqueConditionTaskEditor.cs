using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Editor for the <see cref="Il2Cpp.DS_IsUniqueConditionTask"/>. Used with <see cref="Il2CppNodeCanvas.DialogueTrees.MultipleChoiceNode.Choice"/>
    /// </summary>
    public class DS_IsUniqueConditionTaskEditor : DrawTaskEditor
    {

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            GUI.Box(new Rect(position.x, position.y, 200, 20), "DS_IsUniqueConditionTask");

            return new Rect(position.x, position.y, 200, 20);
        }
    }
}
