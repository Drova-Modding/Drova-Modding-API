using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using UnityEngine;
using static Il2CppDrova.Actor;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_ChangeStanceNodeEditor : DrawNodeEditor
    {

        GUIDropdown StanceDropdown;

        DS_ChangeStanceNode CastedNode;
        public override Rect DrawNode(Vector2 position)
        {
            if (CastedNode == null)
            {
                CastedNode = Node.TryCast<DS_ChangeStanceNode>();
                var options = Enum.GetNames<EInteractionMode>();
                StanceDropdown = new GUIDropdown(options, (int)CastedNode._interactionMode);
            }
            if (CastedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            Color previousBackgroundColor = GUI.backgroundColor;

            GUI.color = Color.green;
            GUI.backgroundColor = Color.black;

            Rect rect = new(position.x, position.y, 200, 200);
            GUI.Box(rect, "DS_ChangeStanceNode");
            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 5, position.y + 25, 200, 20), "Stance");
            if (StanceDropdown.Draw(new Rect(position.x + 5, position.y + 45, 200, 20)))
            {
                CastedNode._interactionMode = (EInteractionMode)StanceDropdown.SelectedIndex;
            }
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackgroundColor;
            return rect;
        }
    }
}
