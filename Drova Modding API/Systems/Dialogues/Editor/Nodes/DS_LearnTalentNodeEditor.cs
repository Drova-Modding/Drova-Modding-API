using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="Il2CppNodeCanvas.DialogueTrees.DS_LearnTalentNode"/>
    /// </summary>
    internal class DS_LearnTalentNodeEditor : DrawNodeEditor
    {

        public DS_LearnTalentNodeEditor()
        {
            NodeSizeInternal = new Vector2(250, 30);
        }

        public override void DrawNode(Vector2 position)
        {
            Color color = GUI.color;
            int depth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, 250, 30), "DS_LearnTalentNode");
            GUI.color = color;
            GUI.depth = depth;
        }
    }
}
