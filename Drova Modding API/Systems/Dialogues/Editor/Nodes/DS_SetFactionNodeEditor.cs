using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.Factions;
using Il2CppNodeCanvas.DialogueTrees;
using System.Collections.Immutable;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_SetFactionNodeEditor : DrawNodeEditor
    {
        private DS_SetFactionNode _castedNode;
        private GUIDropdown _factionDropdown;
        private Faction[] _factions;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetFactionNode>();
            _factions = Resources.FindObjectsOfTypeAll<Faction>().ToArray();
            var selectedIndex = Array.FindIndex(_factions, Faction => Faction.name == _castedNode._faction.name);
            if (selectedIndex == -1)
            {
                selectedIndex = 0;
            }
            _factionDropdown = new GUIDropdown(_factions.Select(e => e.name).ToArray(), selectedIndex);
        }

        public override Rect DrawNode(Vector2 position)
        {
            if(_castedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            var additionalHeight = _factionDropdown.IsDropdownShown ? 20 * _factionDropdown.OptionsCount : 0;
            var rect = new Rect(position.x, position.y, 340, 70 + additionalHeight);
            GUI.Box(rect, "DS_SetFactionNode");

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 10, position.y + 30, 100, 20), "Faction:");
            if (_factionDropdown.Draw(new Rect(position.x + 120, position.y + 30, 200, 20)))
            {
                _castedNode._faction = _factions[_factionDropdown.SelectedIndex];
            }
            GUI.color = previousColor;
            return rect;
        }
    }
}
