using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{

    /// <summary>
    /// Node editor for <see cref="ProbabilitySelector"/>
    /// </summary>
    internal class ProbabilitySelectorNodeEditor : DrawNodeEditor
    {
        private ProbabilitySelector _castedNode;

        public ProbabilitySelectorNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 180);
        }

        public override void Init()
        {
            _castedNode = Node.TryCast<ProbabilitySelector>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null) return;
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, 350, 30 + (_castedNode.childOptions.Count * 30)), "ProbabilitySelectorNode");
            NodeSizeInternal = new Vector2(350, 30 + (_castedNode.childOptions.Count * 30));
            GUI.color = Color.white;

            for (int i = 0; i < _castedNode.childOptions.Count; i++)
            {
                Il2CppNodeCanvas.Framework.BBParameter<float> option = _castedNode.childOptions[i].weight;
                GUI.Label(new Rect(position.x + 10, position.y + 20 + (i * 20), 200, 20), $"Option {i + 1} Weight:");
                string tempWeigth = GUI.TextField(new Rect(position.x + 220, position.y + 20 + (i * 20), 100, 20), option.value.ToString());
                if (float.TryParse(tempWeigth, out float result))
                {
                    option.value = result;
                }
            }
            GUI.color = previousColor;

        }

    }
}
