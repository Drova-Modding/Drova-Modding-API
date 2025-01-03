using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_HubJumpNode"/>
    /// </summary>
    internal class DS_HubJumpNodeEditor : DrawNodeEditor
    {

        private DS_HubJumpNode _castedNode;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_HubJumpNode>();
        }

        public override Rect DrawNode(Vector2 position)
        {
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;

            GUI.depth = 10;
            GUI.color = Color.green;

            var rect = new Rect(position.x, position.y, 420, 60);

            GUI.Box(rect, "DS_HubJumpNode");

            GUI.color = Color.white;

            GUI.Label(new Rect(position.x + 5, position.y + 20, 200, 20), "Jump Target Tag:");
            _castedNode._targetNodeTag = GUI.TextField(new Rect(position.x + 210, position.y + 20, 200, 20), _castedNode._targetNodeTag);

            GUI.color = previousColor;
            GUI.depth = previousDepth;

            return rect;
        }
    }
}
