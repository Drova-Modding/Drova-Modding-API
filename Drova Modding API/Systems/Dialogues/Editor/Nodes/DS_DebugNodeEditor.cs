using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_DebugNodeEditor : DrawNodeEditor
    {
        private DS_DebugNode _castedNode;

        public DS_DebugNodeEditor()
        {
            NodeSizeInternal = new Vector2(400, 50);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_DebugNode>();
        }

        public override void DrawNode(Vector2 position)
        {

            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, 400, 50), "DS_DebugNode");
            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 5, position.y + 25, 100, 20), "Wait time");
            string text = GUI.TextField(new Rect(position.x + 115, position.y + 25, 200, 20), _castedNode._waitTime.ToString());
            if (float.TryParse(text, out float waitTime))
            {
                _castedNode._waitTime = waitTime;
            }
            GUI.color = previousColor;
        }
    }
}
