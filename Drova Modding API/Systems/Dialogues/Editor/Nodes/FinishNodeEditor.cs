using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="Il2CppNodeCanvas.DialogueTrees.FinishNode"/>
    /// </summary>
    internal class FinishNodeEditor : DrawNodeEditor
    {
        private readonly GUIContent _content = new("Finish Node", "End the dialogue in Success or Failure.\r\nNote: A Dialogue will anyway End in Succcess if it has reached a node without child connections. Thus this node is mostly useful if you want to end a Dialogue in Failure.");

        public FinishNodeEditor()
        {
            NodeSizeInternal = new Vector2(200, 30);
        }

        public override void DrawNode(Vector2 position)
        {
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;

            GUI.color = Color.green;
            GUI.depth = 10;

            GUI.Box(new Rect(position.x, position.y, 200, 30), _content);

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
