using Il2CppNodeCanvas.DialogueTrees;

using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_StatementNodeEditor : DrawNodeEditor
    {
        protected Vector2 nodeSize = new(200, 50);
        DS_StatementNode CastedNode;
        public DS_StatementNodeEditor()
        {
            NodeSizeInternal = new Vector2(200, 100);
        }

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_StatementNode>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (CastedNode == null) return;

            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, nodeSize.x, nodeSize.y + 50), new GUIContent("DS_StatementNode", CastedNode.GetLocalizedString()));

            Color previousBackgroundColor = GUI.backgroundColor;

            GUI.backgroundColor = Color.black;
            GUI.color = Color.white;

            // Editable TextFields for type and property
            if (CastedNode.statement.useGlobalLoca)
            {
                CastedNode.statement._globalPath = GUI.TextField(new Rect(position.x + 5, position.y + 25, nodeSize.x - 10, 20), CastedNode.statement._globalPath);
            }
            else
            {
                GUI.Label(new Rect(position.x + 5, position.y + 25, nodeSize.x - 10, 20), "LocaPath: " + CastedNode.DLGTree.LocaPath);
            }
            CastedNode.statement._locaKey = GUI.TextField(new Rect(position.x + 5, position.y + 55, nodeSize.x - 10, 20), CastedNode.statement._locaKey);

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackgroundColor;
        }
    }
}
