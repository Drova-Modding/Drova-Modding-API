using Il2CppNodeCanvas.DialogueTrees;
using System.Text;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="SubDialogueTree"/>
    /// </summary>
    internal class SubDialogueTreeEditor : DrawNodeEditor
    {
        private SubDialogueTree _castedNode;
        private readonly GUIContent GUIContent = new("SubDialogueTree", "Execute a Sub Dialogue Tree. When that Dialogue Tree is finished, this node will continue either in Success or Failure if it has any connections. Useful for making reusable and self-contained Dialogue Trees.");

        public SubDialogueTreeEditor()
        {
            NodeSizeInternal = new Vector2(850, 90);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<SubDialogueTree>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;

            GUI.Box(new Rect(position.x, position.y, 850, 90), GUIContent);

            GUI.color = Color.white;

            StringBuilder sb = new();
            if (_castedNode.subGraph == null)
            {
                sb.Append("No Sub Dialogue Tree selected");
            }
            else
                sb.Append("Full name: ").Append(_castedNode.subGraph.Key);

            GUI.Label(new Rect(position.x, position.y + 25, 800, 50), sb.ToString());

            GUI.depth = previousDepth;
            GUI.color = previousColor;

        }

        public override void OnDoubleClick(Vector2 mousePosition)
        {
            base.OnDoubleClick(mousePosition);
            GraphEditorManager.GoIntoSubGraph(_castedNode);
        }
    }
}
