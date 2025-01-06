using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_SetTextSpeedNode"/>
    /// </summary>
    internal class DS_SetTextSpeedNodeEditor : DrawNodeEditor
    {
        private DS_SetTextSpeedNode _castedNode;
        private GUIDropdown _tempoDropdown;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetTextSpeedNode>();
            _tempoDropdown = new GUIDropdown(Enum.GetNames<Tempo>(), (int)_castedNode.TextSpeed);
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            var rect = new Rect(position.x, position.y, 350, 60);
            GUI.Box(rect, "DS_SetTextSpeedNode");
            GUI.color = Color.white;
            if (_tempoDropdown.Draw(new Rect(position.x + 5, position.y + 20, 250, 25)))
            {
                _castedNode.TextSpeed = (Tempo)_tempoDropdown.SelectedIndex;
            }
            GUI.depth = previousDepth;
            GUI.color = previousColor;
            return rect;
        }
    }
}
