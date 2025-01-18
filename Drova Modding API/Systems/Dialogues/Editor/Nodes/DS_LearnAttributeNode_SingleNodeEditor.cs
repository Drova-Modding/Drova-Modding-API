using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_LearnAttributeNode_Single"/>
    /// </summary>
    internal class DS_LearnAttributeNode_SingleNodeEditor : DrawNodeEditor
    {
        private DS_LearnAttributeNode_Single _castedNode;
        private GUIDropdown _attributeDropdown;
        private GenericStatDesc[] _genericStatDecs;

        public DS_LearnAttributeNode_SingleNodeEditor()
        {
            NodeSizeInternal = new Vector2(450, 80);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_LearnAttributeNode_Single>();
            _genericStatDecs = Resources.FindObjectsOfTypeAll<GenericStatDesc>();
            _attributeDropdown = new GUIDropdown(_genericStatDecs.Select(s => s.name).ToArray(), Array.FindIndex(_genericStatDecs, (s) => s.Guid == _castedNode._stat.Guid));
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, 450, 80), "DS_LearnAttributeNode_Single");

            GUI.color = Color.white;
            GUI.depth = 10;
            GUI.Label(new Rect(position.x + 10, position.y + 20, 85, 20), "Amount:");
            string tempAmount = GUI.TextField(new Rect(position.x + 100, position.y + 20, 200, 20), _castedNode._amount.ToString());
            if (int.TryParse(tempAmount, out int result))
            {
                _castedNode._amount = result;
            }

            GUI.Label(new Rect(position.x + 10, position.y + 50, 85, 20), "Attribute:");
            if (_attributeDropdown.Draw(new Rect(position.x + 100, position.y + 50, 200, 20)))
            {
                _castedNode._stat = _genericStatDecs[_attributeDropdown.SelectedIndex];
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;

        }
    }
}
