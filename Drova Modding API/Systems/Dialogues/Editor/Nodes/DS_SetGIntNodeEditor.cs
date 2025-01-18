using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.GlobalVarSystem;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{

    /// <summary>
    /// Node editor for <see cref="DS_SetGIntNode"/>
    /// </summary>
    internal class DS_SetGIntNodeEditor : DrawNodeEditor
    {

        private DS_SetGIntNode _castedNode;
        private GUIDropdown _operatorDropdown;
        private GUIGvarSelectionEditor _gvarSelectionEditor;
        private GUIGvarSelectionEditor _valueSelectionEditor;

        public DS_SetGIntNodeEditor()
        {
            NodeSizeInternal = new Vector2(480, 110);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetGIntNode>();
            if (_castedNode._useGInt)
            {
                _valueSelectionEditor = new GUIGvarSelectionEditor(GvarType.INT, _castedNode.GIntValue.GetParent().name, false, _castedNode.GIntValue);
            }
            _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.INT, _castedNode.Variable.GetValue().GetParent().name, false, _castedNode.Variable.GetValue());
            _operatorDropdown = new GUIDropdown(Enum.GetNames<GInt.Operator>(), (int)_castedNode.Operation.GetValue());

        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            Rect drawRect = new(position.x, position.y, 480, 110);
            GUI.Box(drawRect, "DS_SetGIntNode");
            GUI.color = previousColor;
            GUI.depth = previousDepth;

            _castedNode._useGInt = GUI.Toggle(new Rect(position.x + 10, position.y + 100, 200, 20), _castedNode._useGInt, "Use GInt");

            if (_operatorDropdown.Draw(new Rect(position.x, position.y + 80, 200, 20)))
            {
                _castedNode.Operation = (GInt.Operator)_operatorDropdown.SelectedIndex;
            }
            GUI.Label(new Rect(position.x + 10, position.y + 60, 200, 20), "Value to set");
            if (_castedNode._useGInt)
            {
                if (_valueSelectionEditor.DrawGvarEditor(new Rect(position.x, position.y + 60, 220, 20), new Rect(position.x, position.y + 80, 220, 20)))
                {
                    _castedNode.GIntValue = _valueSelectionEditor.CurrentSelectedGvar.TryCast<GInt>();
                }

            }
            else
            {
                string tempValue = GUI.TextField(new Rect(position.x + 10, position.y + 60, 200, 20), _castedNode.Value.GetValue().ToString());
                if (int.TryParse(tempValue, out int result))
                {
                    _castedNode.Value = result;
                }

            }
            Rect gvarDropdownRect = new(position.x, position.y + 40, 220, 20);
            if (_gvarSelectionEditor.DrawGvarEditor(new Rect(position.x, position.y + 20, 220, 20), gvarDropdownRect))
            {
                _castedNode.Variable = _gvarSelectionEditor.CurrentSelectedGvar.TryCast<GInt>();
            }
        }
    }
}
