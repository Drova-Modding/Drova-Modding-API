using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.GlobalVarSystem;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_SetGBoolNode"/>
    /// </summary>
    internal class DS_SetGBoolNodeEditor : DrawNodeEditor
    {
        private DS_SetGBoolNode _castedNode;
        private GUIGvarSelectionEditor _gvarSelectionEditor;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetGBoolNode>();
            _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.BOOL, _castedNode.Variable.GetValue().GetParent().name, false, _castedNode.Variable.GetValue());

        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            Rect rect = new(position.x, position.y + 20, 220, 20);

            GUI.color = Color.white;
            GUI.depth = 10;
            if (_gvarSelectionEditor.DrawGvarEditor(rect))
            {
                _castedNode.Variable = _gvarSelectionEditor.CurrentSelectedGvar.TryCast<GBool>();
            }

            rect.y = position.y;

            _castedNode.Value = GUI.Toggle(new Rect(position.x + 10, position.y + 60, 200, 20), _castedNode.Value.value, "Value to set");

            GUI.color = Color.green;
            Rect drawRect = new(position.x, position.y, 450, rect.height + 100);
            GUI.Box(drawRect, "DS_SetGBoolNode");

            GUI.color = previousColor;
            GUI.depth = previousDepth;

            return drawRect;

        }
    }
}
