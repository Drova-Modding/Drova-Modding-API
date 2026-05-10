using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Items;
using Il2CppSystem.Linq;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for DS_SetSpellToActiveAbiSlot.
    /// </summary>
    internal class DS_SetSpellToActiveAbiSlotNodeEditor : DrawNodeEditor
    {
        private DS_SetSpellToActiveAbiSlot? _node;
        private Item[] _items = [];
        private GUIDropdownWithFilter? _spellDropdown;

        public DS_SetSpellToActiveAbiSlotNodeEditor()
        {
            NodeSizeInternal = new Vector2(420, 60);
        }

        public override void Init()
        {
            _node ??= Node.TryCast<DS_SetSpellToActiveAbiSlot>();
            if (_node == null)
            {
                return;
            }

            _items = ProviderAccess.GetGameDatabase().Items.GetItems().ToArray();
            int selectedIndex = Array.FindIndex(_items, item => item.Guid == _node._spell?.Guid);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            _spellDropdown = new GUIDropdownWithFilter(_items.Select(e => e.ReadableId).ToArray(), selectedIndex, 20);
        }

        public override void DrawNode(Vector2 position)
        {
            if (_node == null)
            {
                return;
            }

            Color color = GUI.color;
            int depth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            GUI.Box(new Rect(position.x, position.y, 420, 60), "DS_SetSpellToActiveAbiSlot");

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 10, position.y + 25, 80, 20), "Spell:");
            if (_spellDropdown != null && _items.Length > 0 && _spellDropdown.Draw(new Rect(position.x + 90, position.y + 25, 310, 20)))
            {
                _node._spell = _items[_spellDropdown.SelectedIndex];
            }

            GUI.color = color;
            GUI.depth = depth;
        }
    }
}

