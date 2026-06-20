using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_OverrideFixCamPos"/>
    /// </summary>
    internal class DS_OverrideFixCamPosNodeEditor : DrawNodeEditor
    {

        private DS_OverrideFixCamPos? _castedNode;

        public DS_OverrideFixCamPosNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 60);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_OverrideFixCamPos>();
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

            GUI.Box(new Rect(position.x, position.y, 350, 60), "DS_OverrideFixCamPos");

            GUI.color = Color.white;
            _castedNode.OverrideFixCamPos = GUI.Toggle(new Rect(position.x + 5, position.y + 20, 200, 25), _castedNode.OverrideFixCamPos, "Override Fix Cam Pos");

            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }
    }
}
