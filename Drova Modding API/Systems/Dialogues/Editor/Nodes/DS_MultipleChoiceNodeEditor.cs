using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_MultipleChoiceNodeEditor : DrawNodeEditor
    {
        DS_MultipleChoiceNode CastedNode;
        public override Rect DrawNode(Vector2 position)
        {
            CastedNode ??= Node.TryCast<DS_MultipleChoiceNode>();

            if (CastedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            CastedNode.availableChoices ??= new Il2CppSystem.Collections.Generic.List<DS_MultipleChoiceNode.Choice>();
            var additionalHeight = CastedNode.availableChoices.Count * 75 + 20;
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.black;
            GUI.color = Color.white;
            int step = 20;
            for (int i = 0; i < CastedNode.availableChoices.Count; i++)
            {
                var choice = CastedNode.availableChoices[i];
                choice.statement._globalPath = GUI.TextField(new Rect(position.x + 5, position.y + step, 200, 20), choice.statement._globalPath);
                step += 35;
                choice.statement._locaKey = GUI.TextField(new Rect(position.x + 5, position.y + step, 200, 20), choice.statement._locaKey);
                step += 35;
            }
            // Add choice button if there are less than 8 choices
            if (CastedNode.availableChoices.Count < 8) {
                if (GUI.Button(new Rect(position.x, position.y + CastedNode.availableChoices.Count * 70 + 20, 200, 20), "Add Choice"))
                {
                    CastedNode.availableChoices.Add(new DS_MultipleChoiceNode.Choice(new DS_Statement()));
                }
                additionalHeight += 20;
            }
            GUI.backgroundColor = previousBackgroundColor;
            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 220f, 10f + additionalHeight);
            GUI.Box(rect, "DS_MultipleChoiceNode");
            GUI.color = previousColor;
            return rect;
        }
    }
}
