using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="Il2CppNodeCanvas.DialogueTrees.DS_DelayedTeach_Statement"/>
    /// </summary>
    internal class DS_DelayedTeach_StatementNodeEditor : DrawNodeEditor
    {
        public DS_DelayedTeach_StatementNodeEditor()
        {
            NodeSizeInternal = new Vector2(250, 30);
        }

        public override void DrawNode(Vector2 position)
        {
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.depth = 10;
            GUI.color = Color.green;
            GUI.Box(new Rect(position.x, position.y, 250, 30), new GUIContent("DS_DelayedTeach_Statement", "Say Teach Statement"));
        }
    }
}
