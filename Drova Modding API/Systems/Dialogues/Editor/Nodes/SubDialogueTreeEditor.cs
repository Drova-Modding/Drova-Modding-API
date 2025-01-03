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

        public override void Init()
        {
            _castedNode ??= Node.TryCast<SubDialogueTree>();
        }
        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }
            var rect = new Rect(position.x, position.y, 450, 90);

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;

            GUI.Box(rect, GUIContent);

            GUI.color = Color.white;

            GUI.Label(new Rect(position.x, position.y + 20, 70, 25), "Name: ");
            _castedNode.name = GUI.TextField(new Rect(position.x + 75, position.y + 20, 200, 25), _castedNode.name);

            StringBuilder sb = new();
            sb.Append("Full name: ").Append(_castedNode.DLGTree.Key).Append(" ( ").Append(_castedNode.name).Append(" )");

            GUI.Label(new Rect(position.x, position.y + 55, 400, 25), sb.ToString());

            GUI.depth = previousDepth;
            GUI.color = previousColor;
            return rect;

        }

        public override void OnDoubleClick(Vector2 mousePosition)
        {
            base.OnDoubleClick(mousePosition);
            GraphEditorManager.GoIntoSubGraph(_castedNode);
        }
    }
}
