using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="Il2CppNodeCanvas.DialogueTrees.DS_HideDialogWindow"/>
    /// </summary>
    internal class DS_HideDialogWindowNodeEditor : DrawNodeEditor
    {
        public DS_HideDialogWindowNodeEditor()
        {
            NodeSizeInternal = new Vector2(200, 30);
        }

        public override void DrawNode(Vector2 position)
        {
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, 200, 30), "DS_HideDialogWindowNode");
            GUI.color = previousColor;
        }
    }
}
