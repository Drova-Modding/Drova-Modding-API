using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    internal class NullTaskEditor : DrawTaskEditor
    {
        public override Rect DrawTask(Vector2 position)
        {
            var rect = new Rect(position.x, position.y, 200, 20);

            GUI.Box(rect, "Null Task");

            return rect;
        }
    }
}
