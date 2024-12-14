using Il2CppNodeCanvas.DialogueTrees;

using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_StatementNodeEditor: DrawNodeEditor
    {
        protected Vector2 nodeSize = new(200, 50);
        public DS_StatementNodeEditor() { }
        public override Rect DrawNode(Vector2 position)
        {
            var statementNode = Node.TryCast<DS_StatementNode>();
            if (statementNode == null) return default;

            Color previousColor = GUI.color;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, nodeSize.x, nodeSize.y + 50);

            // Draw the node as a rectangle
            GUI.Box(rect, "DS_StatementNode");
            Color previousBackgroundColor = GUI.backgroundColor;

            GUI.backgroundColor = Color.black;
            GUI.color = Color.white;

            // Editable TextFields for type and property
            statementNode.statement._globalPath = GUI.TextField(new Rect(position.x + 5, position.y + 25, nodeSize.x - 10, 20), statementNode.statement._globalPath);
            statementNode.statement._locaKey = GUI.TextField(new Rect(position.x + 5, position.y + 55, nodeSize.x - 10, 20), statementNode.statement._locaKey);

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackgroundColor;

            return rect;
        }
    }
}
