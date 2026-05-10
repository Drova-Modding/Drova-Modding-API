using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_SetFirstChapterNodeEditor : DrawNodeEditor
    {

        public DS_SetFirstChapterNodeEditor()
        {
            NodeSizeInternal = new Vector2(200, 20);
        }

        public override void DrawNode(Vector2 position)
        {
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, 200, 20), "DS_SetFirstChapter");
            GUI.color = previousColor;
        }
    }
}
