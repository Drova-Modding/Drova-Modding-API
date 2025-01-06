using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_SetDialogueRequirementsNode"/>
    /// </summary>
    internal class DS_SetDialogueRequirementsNodeEditor : DrawNodeEditor
    {

        private DS_SetDialogueRequirementsNode _castedNode;
        private GUIDropdown _styleDropdown;
        private GUIDropdown _tempoDropdown;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetDialogueRequirementsNode>();
            _styleDropdown = new GUIDropdown(Enum.GetNames<Style>(), (int)_castedNode.style);
            _tempoDropdown = new GUIDropdown(Enum.GetNames<Tempo>(), (int)_castedNode.tempo);
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            Rect rect = new(position.x, position.y, 320, 20);

            GUI.color = Color.white;
            GUI.depth = 10;

            _castedNode.Autoplay = GUI.Toggle(new Rect(position.x + 5, position.y + 20, 130, 20), _castedNode.Autoplay, "Autoplay");

            _castedNode.PlayerToNormalStance = GUI.Toggle(new Rect(position.x + 5, position.y + 40, 250, 20), _castedNode.PlayerToNormalStance, "Player to normal Stance");

            _castedNode.OverrideWaitForInput = GUI.Toggle(new Rect(position.x + 5, position.y + 60, 250, 20), _castedNode.OverrideWaitForInput, "Override wait for Input");

            GUI.Label(new Rect(position.x + 10, position.y + 110, 85, 20), "Tempo:");
            if (_tempoDropdown.Draw(new Rect(position.x + 100, position.y + 110, 200, 20)))
            {
                _castedNode.tempo = (Tempo)_tempoDropdown.SelectedIndex;
            }

            GUI.Label(new Rect(position.x + 10, position.y + 80, 85, 20), "Style:");
            if (_styleDropdown.Draw(new Rect(position.x + 100, position.y + 80, 200, 20)))
            {
                _castedNode.style = (Style)_styleDropdown.SelectedIndex;
            }

            GUI.color = Color.green;
            bool anyDropdownShown = _styleDropdown.IsDropdownShown || _tempoDropdown.IsDropdownShown;
            int additionalHeight = anyDropdownShown ? 20 * 5 : 20;

            Rect drawRect = new(position.x, position.y, 320, rect.height + additionalHeight);

            GUI.Box(drawRect, "DS_SetDialogueRequirementsNode");

            GUI.color = previousColor;
            GUI.depth = previousDepth;

            return drawRect;

        }
    }
}
