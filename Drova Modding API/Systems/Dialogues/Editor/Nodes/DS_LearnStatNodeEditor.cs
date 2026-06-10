using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Let teacher try to teach player preselected attribute stat. Needs a <see cref="DS_TeachStatsNode"/> as In-Connection
    /// </summary>
    internal class DS_LearnStatNodeEditor : DrawNodeEditor
    {
        private DS_LearnStatNode? _castedNode;

        public DS_LearnStatNodeEditor()
        {
            NodeSizeInternal = new Vector2(250, 80);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_LearnStatNode>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null) return;

            Color color = GUI.color;
            int depth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;

            GUI.Box(new Rect(position.x, position.y, 250, 80), "DS_LearnStatNode", EditorBoxStyles.GenericNode);

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 5, position.y + 25, 240, 50), "Description: Let teacher try to teach player preselected attribute stat. Needs a DS_TeachStatsNode as In-Connection.");

            GUI.color = color;
            GUI.depth = depth;
        }
    }
}
