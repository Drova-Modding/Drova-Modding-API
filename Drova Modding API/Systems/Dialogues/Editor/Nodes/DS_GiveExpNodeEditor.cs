using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_GiveExpNodeEditor : DrawNodeEditor
    {
        DS_GiveExp CastedNode;

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_GiveExp>();
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (CastedNode == null) return default;

            var giveExpNodeRect = new Rect(
                position.x,
                position.y,
                200,
                50
            );
            Color previousColor = GUI.color;
            GUI.color = Color.green;

            GUI.Box(giveExpNodeRect, "DS_GiveExpNode");

            GUI.color = Color.white;

            string expPointsStr = CastedNode.ExpPoints.ToString();
            expPointsStr = GUI.TextField(new Rect(position.x + 5, position.y + 25, 190, 20), expPointsStr);
            if (int.TryParse(expPointsStr, out int expPoints))
            {
                CastedNode.ExpPoints = expPoints;
            }

            GUI.color = previousColor;

            return giveExpNodeRect;
        }
    }
}
