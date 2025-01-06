using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="Il2CppNodeCanvas.DialogueTrees.DS_LearnStatNode"/>
    /// </summary>
    internal class DS_LearnStatNodeEditor : DrawNodeEditor
    {
        public override Rect DrawNode(Vector2 position)
        {
            Color color = GUI.color;
            int depth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 250, 30);
            GUI.Box(rect, "DS_LearnStatNode");
            GUI.color = color;
            GUI.depth = depth;
            return rect;

        }
    }
}
