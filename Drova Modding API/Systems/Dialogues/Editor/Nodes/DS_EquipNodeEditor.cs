using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppSystem.Linq;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for DS_EquipNode.
    /// </summary>
    internal class DS_EquipNodeEditor : DrawNodeEditor
    {
        private DS_EquipNode? _node;
        private Item[] _items = [];
        private readonly List<GUIDropdownWithFilter> _itemDropdowns = [];

        public DS_EquipNodeEditor()
        {
            NodeSizeInternal = new Vector2(420, 90);
        }

        public override void Init()
        {
            _node ??= Node.TryCast<DS_EquipNode>();
            if (_node == null)
            {
                return;
            }

            _items = ProviderAccess.GetGameDatabase().Items.GetItems().ToArray();

            if (_node.Items == null)
            {
                _node.Items = new Il2CppSystem.Collections.Generic.List<DialogItems>();
            }

            _itemDropdowns.Clear();
            for (int i = 0; i < _node.Items.Count; i++)
            {
                DialogItems dialogItem = _node.Items[i];
                int selectedIndex = Array.FindIndex(_items, item => item.Guid == dialogItem.Item?.Guid);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }

                _itemDropdowns.Add(new GUIDropdownWithFilter(_items.Select(e => e.ReadableId).ToArray(), selectedIndex, 20));
            }
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

            float height = 80 + (30 * _node.Items.Count);
            NodeSizeInternal = new Vector2(420, height);
            GUI.Box(new Rect(position.x, position.y, 420, height), "DS_EquipNode");

            GUI.color = Color.white;
            if (_items.Length == 0)
            {
                GUI.Label(new Rect(position.x + 10, position.y + 25, 380, 20), "No items found in database.");
                GUI.color = color;
                GUI.depth = depth;
                return;
            }

            for (int i = 0; i < _node.Items.Count; i++)
            {
                float y = position.y + 25 + (i * 30);
                DialogItems dialogItem = _node.Items[i];

                if (GUI.Button(new Rect(position.x + 380, y, 25, 20), "X"))
                {
                    _node.Items.RemoveAt(i);
                    _itemDropdowns.RemoveAt(i);
                    i--;
                    continue;
                }

                if (_itemDropdowns[i].Draw(new Rect(position.x + 10, y, 360, 20)))
                {
                    dialogItem.Item = _items[_itemDropdowns[i].SelectedIndex];
                }
            }

            if (GUI.Button(new Rect(position.x + 10, position.y + height - 30, 100, 20), "Add Item"))
            {
                DialogItems dialogItem = new()
                {
                    Item = _items[0]
                };
                _node.Items.Add(dialogItem);
                _itemDropdowns.Add(new GUIDropdownWithFilter(_items.Select(e => e.ReadableId).ToArray(), 0, 20));
            }

            GUI.color = color;
            GUI.depth = depth;
        }
    }
}

