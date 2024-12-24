using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_GiveExpNodeEditor : DrawNodeEditor
    {
        DS_GiveExp CastedNode;

        public override Rect DrawNode(Vector2 position)
        {
            CastedNode ??= Node.TryCast<DS_GiveExp>();

            if (CastedNode == null) return default;

            var giveExpNodeRect = new Rect(
                position.x,
                position.y,
                200,
                50
            );

            GUI.Box(giveExpNodeRect, "DS_GiveExpNode");

            string expPointsStr = CastedNode.ExpPoints.ToString();
            expPointsStr = GUI.TextField(new Rect(position.x + 5, position.y + 25, 190, 20), expPointsStr);
            if (int.TryParse(expPointsStr, out int expPoints))
            {
                CastedNode.ExpPoints = expPoints;
            }

            return giveExpNodeRect;
        }
    }
}
