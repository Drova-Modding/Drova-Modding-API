using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_OpenTradeWindowNode"/>
    /// </summary>
    internal class DS_OpenTradeWindowNodeEditor : DrawNodeEditor
    {
        private DS_OpenTradeWindowNode _castedNode;

        public DS_OpenTradeWindowNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 60);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_OpenTradeWindowNode>();
        }
        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.depth = 10;
            GUI.color = Color.green;

            GUI.Box(new Rect(position.x, position.y, 350, 60), "DS_OpenTradeWindowNode");

            GUI.color = Color.white;

            _castedNode._canTradeWithMoney = GUI.Toggle(new Rect(position.x + 5, position.y + 20, 250, 25), _castedNode._canTradeWithMoney, "Can trade with money");

            GUI.depth = previousDepth;
            GUI.color = previousColor;

        }
    }
}
