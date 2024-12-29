using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_DebugNodeEditor : DrawNodeEditor
    {
        DS_DebugNode CastedNode;

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_DebugNode>();
        }

        public override Rect DrawNode(Vector2 position)
        {

            if (CastedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 400, 50);
            GUI.Box(rect, "DS_DebugNode");
            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 5, position.y + 25, 100, 20), "Wait time");
            string text = GUI.TextField(new Rect(position.x + 115, position.y + 25, 200, 20), CastedNode._waitTime.ToString());
            if (float.TryParse(text, out float waitTime))
            {
                CastedNode._waitTime = waitTime;
            }
            GUI.color = previousColor;
            return rect;
        }
    }
}
