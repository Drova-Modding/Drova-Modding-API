using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_MultipleChoiceNodeEditor : DrawNodeEditor
    {
        private DS_MultipleChoiceNode CastedNode;

        public DS_MultipleChoiceNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 150);
        }

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_MultipleChoiceNode>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (CastedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            CastedNode.availableChoices ??= new Il2CppSystem.Collections.Generic.List<DS_MultipleChoiceNode.Choice>();
            var additionalHeight = CastedNode.availableChoices.Count * 75 + 65;
            Color previousBackgroundColor = GUI.backgroundColor;

            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 350, additionalHeight);
            NodeSizeInternal = new Vector2(350, additionalHeight);
            GUI.Box(rect, "DS_MultipleChoiceNode");


            GUI.backgroundColor = Color.black;
            GUI.color = Color.white;
            int step = 20;
            for (int i = 0; i < CastedNode.availableChoices.Count; i++)
            {
                var choice = CastedNode.availableChoices[i];
                GUI.Label(new Rect(position.x + 5, position.y + step, 100, 20), "Globapath:");
                choice.statement._globalPath = GUI.TextField(new Rect(position.x + 110, position.y + step, 200, 20), choice.statement._globalPath);
                step += 35;
                GUI.Label(new Rect(position.x + 5, position.y + step, 100, 20), "Statement:");
                choice.statement._locaKey = GUI.TextField(new Rect(position.x + 110, position.y + step, 200, 20), choice.statement._locaKey);
                step += 35;
            }
            // Add choice button if there are less than 8 choices
            if (CastedNode.availableChoices.Count < 8)
            {
                if (GUI.Button(new Rect(position.x, position.y + CastedNode.availableChoices.Count * 70 + 20, 200, 20), "Add Choice"))
                {
                    CastedNode.availableChoices.Add(new DS_MultipleChoiceNode.Choice(new DS_Statement()));
                }
            }

            GUI.Label(new Rect(position.x + 5, position.y + step + 40, 50, 20), "Tag:");
            CastedNode.tag = GUI.TextField(new Rect(position.x + 110, position.y + step + 40, 200, 20), CastedNode.tag);
            GUI.backgroundColor = previousBackgroundColor;

            GUI.color = previousColor;
        }
    }
}
