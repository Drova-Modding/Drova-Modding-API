using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_SetFirstChapterNodeEditor : DrawNodeEditor
    {
        public override Rect DrawNode(Vector2 position)
        {
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 200, 20);
            GUI.Box(rect, "DS_SetFirstChapter");
            GUI.color = previousColor;
            return rect;
        }
    }
}
