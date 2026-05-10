using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_GiveExpNodeEditor : DrawNodeEditor
    {
        private DS_GiveExp _castedNode;

        public DS_GiveExpNodeEditor()
        {
            NodeSizeInternal = new Vector2(200, 50);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_GiveExp>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null) return;

            Color previousColor = GUI.color;
            GUI.color = Color.green;

            GUI.Box(new Rect(
                position.x,
                position.y,
                200,
                50
            ), "DS_GiveExpNode");

            GUI.color = Color.white;

            string expPointsStr = _castedNode.ExpPoints.ToString();
            expPointsStr = GUI.TextField(new Rect(position.x + 5, position.y + 25, 190, 20), expPointsStr);
            if (int.TryParse(expPointsStr, out int expPoints))
            {
                _castedNode.ExpPoints = expPoints;
            }

            GUI.color = previousColor;

        }
    }
}
