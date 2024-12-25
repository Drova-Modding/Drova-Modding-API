using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_SetFactionNodeEditor : DrawNodeEditor
    {
        DS_SetFactionNode CastedNode;
        public override Rect DrawNode(Vector2 position)
        {
            CastedNode ??= Node.TryCast<DS_SetFactionNode>();
            if(CastedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            var rect = new Rect(position.x, position.y, 200, 100);
            GUI.Box(rect, "DS_SetFactionNode");
            GUI.Label(new Rect(position.x + 10, position.y + 20, 200, 20), "Faction: " + CastedNode._faction.name);
            GUI.color = previousColor;
            return rect;
        }
    }
}
