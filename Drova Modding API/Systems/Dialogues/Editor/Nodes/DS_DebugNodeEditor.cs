using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_DebugNodeEditor : DrawNodeEditor
    {
        DS_DebugNode CastedNode;
        public override Rect DrawNode(Vector2 position)
        {

            CastedNode ??= Node.TryCast<DS_DebugNode>();
            if (CastedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 400, 50);
            GUI.Box(rect, "DS_DebugNode");
            GUI.Label(new Rect(position.x + 5, position.y + 25, 50, 20), "Wait time");
            string text = GUI.TextField(new Rect(position.x + 110, position.y + 25, 200, 20), CastedNode._waitTime.ToString());
            if (float.TryParse(text, out float waitTime))
            {
                CastedNode._waitTime = waitTime;
            }
            GUI.color = previousColor;
            return rect;
        }
    }
}
