using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using UnityEngine;
using static Il2CppDrova.Actor;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_ChangeStanceNodeEditor : DrawNodeEditor
    {

        private GUIDropdown _stanceDropdown;
        private DS_ChangeStanceNode _castedNode;

        public DS_ChangeStanceNodeEditor()
        {
            NodeSizeInternal = new Vector2(220, 80);
        }

        public override void Init()
        {
            _castedNode = Node.TryCast<DS_ChangeStanceNode>();
            string[] options = Enum.GetNames<EInteractionMode>();
            _stanceDropdown = new GUIDropdown(options, (int)_castedNode._interactionMode);
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            Color previousBackgroundColor = GUI.backgroundColor;

            GUI.color = Color.green;
            GUI.backgroundColor = Color.black;

            Rect rect = new(position.x, position.y, 220, 80 + (_stanceDropdown.IsDropdownShown ? 20 * _stanceDropdown.OptionsCount : 0));
            GUI.Box(rect, "DS_ChangeStanceNode");

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 5, position.y + 25, 200, 20), "Stance");
            if (_stanceDropdown.Draw(new Rect(position.x + 5, position.y + 45, 200, 20)))
            {
                _castedNode._interactionMode = (EInteractionMode)_stanceDropdown.SelectedIndex;
            }
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackgroundColor;
        }
    }
}
