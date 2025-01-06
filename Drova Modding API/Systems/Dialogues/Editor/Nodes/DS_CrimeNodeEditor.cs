using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="Il2CppNodeCanvas.DialogueTrees.DS_CrimeNode"/>
    /// </summary>
    internal class DS_CrimeNodeEditor : DrawNodeEditor
    {
        public override Rect DrawNode(Vector2 position)
        {
            Color color = GUI.color;
            int depth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 250, 30);
            GUI.Box(rect, "DS_CrimeNode");
            GUI.color = color;
            GUI.depth = depth;
            return rect;
        }
    }
}
