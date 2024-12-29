using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.Factions;
using Il2CppNodeCanvas.DialogueTrees;
using System.Collections.Immutable;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_SetFactionNodeEditor : DrawNodeEditor
    {
        DS_SetFactionNode CastedNode;
        GUIDropdown FactionDropdown;
        ImmutableList<Faction> Factions;

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_SetFactionNode>();
            Factions = Resources.FindObjectsOfTypeAll<Faction>().ToImmutableList();
            var selectedIndex = Factions.FindIndex(Faction => Faction.name == CastedNode._faction.name);
            if (selectedIndex == -1)
            {
                selectedIndex = 0;
            }
            FactionDropdown = new GUIDropdown(Factions.Select(e => e.name).ToArray(), selectedIndex);
        }

        public override Rect DrawNode(Vector2 position)
        {
            if(CastedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            var additionalHeight = FactionDropdown.IsDropdownShown ? 20 * FactionDropdown.OptionsCount : 0;
            var rect = new Rect(position.x, position.y, 340, 70 + additionalHeight);
            GUI.Box(rect, "DS_SetFactionNode");

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 10, position.y + 30, 100, 20), "Faction:");
            if (FactionDropdown.Draw(new Rect(position.x + 120, position.y + 30, 200, 20)))
            {
                CastedNode._faction = Factions[FactionDropdown.SelectedIndex];
            }
            GUI.color = previousColor;
            return rect;
        }
    }
}
