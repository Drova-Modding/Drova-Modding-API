using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Items;
using Il2CppSystem.Linq;
using System.Globalization;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for DS_UseItem.
    /// </summary>
    internal class DS_UseItemNodeEditor : DrawNodeEditor
    {
        private DS_UseItem? _node;
        private Item[] _items = [];
        private GUIDropdownWithFilter? _itemDropdown;

        public DS_UseItemNodeEditor()
        {
            NodeSizeInternal = new Vector2(420, 90);
        }

        public override void Init()
        {
            _node ??= Node.TryCast<DS_UseItem>();
            if (_node == null)
            {
                return;
            }

            _items = ProviderAccess.GetGameDatabase().Items.GetItems().ToArray();
            int selectedIndex = Array.FindIndex(_items, item => item.Guid == _node._itemToUse?.Guid);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            _itemDropdown = new GUIDropdownWithFilter([.. _items.Select(e => e.ReadableId)], selectedIndex, 20);
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
            GUI.Box(new Rect(position.x, position.y, 420, 90), "DS_UseItem");

            GUI.color = Color.white;

            GUI.Label(new Rect(position.x + 10, position.y + 25, 80, 20), "Item:");
            if (_itemDropdown != null && _items.Length > 0 && _itemDropdown.Draw(new Rect(position.x + 90, position.y + 25, 310, 20)))
            {
                _node._itemToUse = _items[_itemDropdown.SelectedIndex];
            }

            GUI.Label(new Rect(position.x + 10, position.y + 55, 80, 20), "Wait Time:");
            string waitTimeText = GUI.TextField(new Rect(position.x + 90, position.y + 55, 100, 20), _node._waitTime.ToString(CultureInfo.InvariantCulture));
            if (float.TryParse(waitTimeText, NumberStyles.Float, CultureInfo.InvariantCulture, out float waitTime))
            {
                _node._waitTime = waitTime;
            }

            GUI.color = color;
            GUI.depth = depth;
        }
    }
}



