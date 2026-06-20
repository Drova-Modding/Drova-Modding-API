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
        private DS_SetGBoolNode? _castedNode;
        private GUIGvarSelectionEditor _gvarSelectionEditor;

        public DS_SetGBoolNodeEditor()
        {
            NodeSizeInternal = new Vector2(450, 120);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetGBoolNode>();
            if (_castedNode == null) return;

            var variable = _castedNode.Variable != null && !_castedNode.Variable.isNoneOrNull ? _castedNode.Variable.GetValue() : null;
            if (variable == null)
            {
                _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.BOOL);
            }
            else
            {
                _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.BOOL, variable.GetParent().name, false, variable);
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            Rect drawRect = new(position.x, position.y, 450, 120);
            GUI.color = Color.green;
            GUI.Box(drawRect, "DS_SetGBoolNode");

            GUI.color = Color.white;
            GUI.depth = 10;
            Rect gvarDropdownRect = new(position.x, position.y + 40, 220, 20);
            if (_gvarSelectionEditor.DrawGvarEditor(new(position.x, position.y + 20, 220, 20), gvarDropdownRect))
            {
                _castedNode.Variable = _gvarSelectionEditor.CurrentSelectedGvar.TryCast<GBool>();
            }

            bool val = _castedNode.Value != null && _castedNode.Value.value;
            _castedNode.Value = GUI.Toggle(new Rect(position.x + 10, position.y + 60, 200, 20), val, "Value to set");



            GUI.color = previousColor;
            GUI.depth = previousDepth;

        }
    }
}