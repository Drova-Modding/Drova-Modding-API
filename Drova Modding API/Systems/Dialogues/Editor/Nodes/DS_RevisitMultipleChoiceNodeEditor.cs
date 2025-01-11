using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_RevisitMultipleChoiceNodeEditor : DrawNodeEditor
    {
        private DS_RevisitMultipleChoiceNode _castedNode;

        public DS_RevisitMultipleChoiceNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 70);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_RevisitMultipleChoiceNode>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            var rect = new Rect(position.x, position.y, 350, 70);
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(rect, "DS_RevisitMultipleChoiceNode");

            GUI.color = Color.white;

            GUI.Label(new Rect(position.x + 10, position.y + 30, 130, 20), "Revisit Tag:");
            _castedNode._nodeTag = GUI.TextField(new Rect(position.x + 120, position.y + 30, 200, 20), _castedNode._nodeTag);

            // TODO: Maybe add a dropdown for the revisit type

            // TODO: Check if jumpNode and so on ever get used and if they should be editable

            GUI.color = previousColor;

        }
    }
}
