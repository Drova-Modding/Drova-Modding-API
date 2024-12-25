using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_HideDialogWindowNodeEditor : DrawNodeEditor
    {
        public override Rect DrawNode(Vector2 position)
        {
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 200, 50);
            GUI.Box(rect, "DS_HideDialogWindowNode");
            GUI.color = previousColor;
            return rect;
        }
    }
}
