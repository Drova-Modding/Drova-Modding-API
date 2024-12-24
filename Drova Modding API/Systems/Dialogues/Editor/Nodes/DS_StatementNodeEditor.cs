using Il2CppNodeCanvas.DialogueTrees;

using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_StatementNodeEditor: DrawNodeEditor
    {
        protected Vector2 nodeSize = new(200, 50);
        DS_StatementNode CastedNode;
        public DS_StatementNodeEditor() { }
        public override Rect DrawNode(Vector2 position)
        {
            CastedNode ??= Node.TryCast<DS_StatementNode>();
            if (CastedNode == null) return default;

            Color previousColor = GUI.color;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, nodeSize.x, nodeSize.y + 50);

            // Draw the node as a rectangle
            GUI.Box(rect, "DS_StatementNode");
            Color previousBackgroundColor = GUI.backgroundColor;

            GUI.backgroundColor = Color.black;
            GUI.color = Color.white;

            // Editable TextFields for type and property
            CastedNode.statement._globalPath = GUI.TextField(new Rect(position.x + 5, position.y + 25, nodeSize.x - 10, 20), CastedNode.statement._globalPath);
            CastedNode.statement._locaKey = GUI.TextField(new Rect(position.x + 5, position.y + 55, nodeSize.x - 10, 20), CastedNode.statement._locaKey);

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackgroundColor;

            return rect;
        }
    }
}
