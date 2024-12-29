using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppSystem.Collections.Immutable;
using Il2CppSystem.Linq;
using System.Collections.Immutable;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_GiveItemNodeEditor : DrawNodeEditor
    {

        DS_GiveItemNode CastedNode;
        private System.Collections.Immutable.ImmutableList<Item> _items;
        private readonly List<GUIDropdownWithFilter> _itemDropdowns = [];
        private readonly List<GUIDropdown> _directionDropdowns = [];
        private readonly List<GUIDropdown> _valueModeDropdowns = [];
        private readonly Dictionary<int, GUIGvarSelectionEditor> _gvarSelectionEditors = [];

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_GiveItemNode>();
            _items = ProviderAccess.GetGameDatabase().Items.GetItems().ToArray().ToImmutableList();

            for (int i = 0; i < CastedNode.ItemStacks.Count; i++)
            {
                var itemStack = CastedNode.ItemStacks[i];
                var direction = itemStack.Exchange;
                var selectedIndex = _items.FindIndex(id => id.Guid == itemStack.Item.Guid);
                if (selectedIndex == -1)
                {
                    selectedIndex = 0;
                }
                _itemDropdowns.Add(new GUIDropdownWithFilter(_items.Select(e => e.ReadableId).ToArray(), selectedIndex, 20));

                _directionDropdowns.Add(new GUIDropdown(Enum.GetNames<DialogItemsExchange.ExchangeDirection>(), (int)direction));

                _valueModeDropdowns.Add(new GUIDropdown(Enum.GetNames<DialogItems.ValueMode>(), (int)itemStack.Mode));

                if (itemStack.Mode == DialogItems.ValueMode.GInt)
                {
                    _gvarSelectionEditors.TryAdd(i, new GUIGvarSelectionEditor(GvarType.INT));
                }
            }
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (CastedNode == null)
            {
                return default;
            }

            int previousDepth = GUI.depth;
            GUI.depth = 10;

            Color previousColor = GUI.color;
            GUI.color = Color.green;

            int itemCount = CastedNode.ItemStacks.Count;
            float itemHeight = 260f;
            float baseHeight = 60f;
            float rectHeight = baseHeight + itemCount * itemHeight + 30f;

            var rect = new Rect(position.x, position.y, 440, rectHeight);

            GUI.Box(rect, "DS_GiveItemNode");

            GUI.color = Color.white;

            for (int i = 0; i < CastedNode.ItemStacks.Count; i++)
            {
                var itemStack = CastedNode.ItemStacks[i];
                var itemDropdown = _itemDropdowns[i];

                float yOffset = position.y + 30 + i * itemHeight;

                GUI.Label(new Rect(position.x + 10, yOffset, 100, 20), "Item:");

                GUI.Label(new Rect(position.x + 10, yOffset + 20, 100, 20), "Valuemode:");

                if (itemStack.Mode != DialogItems.ValueMode.GInt)
                {
                    GUI.Label(new Rect(position.x + 10, yOffset + 40, 100, 20), "Amount:");
                }

                DrawAmountEditFields(position, i, itemStack, yOffset + 40);

                float useContainerYOffset = yOffset + 60;
                if (itemStack.Mode == DialogItems.ValueMode.GInt)
                {
                    useContainerYOffset += 120;
                }

                itemStack.UseContainer = GUI.Toggle(
                    new Rect(position.x + 10, useContainerYOffset, 150, 20),
                    itemStack.UseContainer,
                    "Use Container");

                GUI.Label(new Rect(position.x + 10, useContainerYOffset + 20, 100, 20), "Direction:");

                if (GUI.Button(new Rect(position.x + 240, useContainerYOffset + 50, 80, 20), "Delete"))
                {
                    CastedNode.ItemStacks.RemoveAt(i);
                    _itemDropdowns.RemoveAt(i);
                    _directionDropdowns.RemoveAt(i);
                    _valueModeDropdowns.RemoveAt(i);
                    _gvarSelectionEditors.Remove(i);

                    for (int j = i; j < _gvarSelectionEditors.Count; j++)
                    {
                        if (_gvarSelectionEditors.ContainsKey(j + 1))
                        {
                            _gvarSelectionEditors[j] = _gvarSelectionEditors[j + 1];
                            _gvarSelectionEditors.Remove(j + 1);
                        }
                    }
                    // Break to avoid modifying collection during iteration
                    break;
                }

                if (_directionDropdowns[i].Draw(new Rect(position.x + 120, useContainerYOffset + 20, 200, 20)))
                {
                    itemStack.Exchange = (DialogItemsExchange.ExchangeDirection)_directionDropdowns[i].SelectedIndex;
                }

                if (_valueModeDropdowns[i].Draw(new Rect(position.x + 120, yOffset + 20, 200, 20)))
                {
                    itemStack.Mode = (DialogItems.ValueMode)_valueModeDropdowns[i].SelectedIndex;
                    if (itemStack.Mode == DialogItems.ValueMode.GInt)
                    {
                        _gvarSelectionEditors.TryAdd(i, new GUIGvarSelectionEditor(GvarType.INT));
                    }
                    else
                    {
                        _gvarSelectionEditors.Remove(i);
                    }
                }

                if (itemDropdown.Draw(new Rect(position.x + 120, yOffset, 300, 20)))
                {
                    itemStack.Item = _items[itemDropdown.SelectedIndex];
                }
            }

            if (GUI.Button(new Rect(position.x + 10, position.y + rectHeight - 30, 80, 20), "Add"))
            {
                var newItemStack = new DialogItemsExchange
                {
                    Item = _items.FirstOrDefault(),
                    Mode = DialogItems.ValueMode.Int,
                    Amount = 1
                };
                CastedNode.ItemStacks.Add(newItemStack);

                _itemDropdowns.Add(new GUIDropdownWithFilter(_items.Select(e => e.ReadableId).ToArray(), 0, 20));
                _directionDropdowns.Add(new GUIDropdown(Enum.GetNames<DialogItemsExchange.ExchangeDirection>(), 0));
                _valueModeDropdowns.Add(new GUIDropdown(Enum.GetNames<DialogItems.ValueMode>(), 0));
            }

            GUI.depth = previousDepth;
            GUI.color = previousColor;

            return rect;
        }

        private void DrawAmountEditFields(Vector2 position, int i, DialogItemsExchange itemStack, float yOffset)
        {
            switch (itemStack.Mode)
            {
                case DialogItems.ValueMode.Int:
                    var amountInt = GUI.TextField(
                        new Rect(position.x + 120, yOffset, 200, 20),
                        itemStack.Amount.ToString());
                    if (int.TryParse(amountInt, out int resultInt))
                    {
                        itemStack.Amount = resultInt;
                    }
                    break;

                case DialogItems.ValueMode.Percentage:
                    var amountRect = new Rect(position.x + 120, yOffset, 180, 20);
                    var percentLabelRect = new Rect(position.x + 305, yOffset, 20, 20);
                    var amountPercent = GUI.TextField(amountRect, itemStack.Amount.ToString());
                    GUI.Label(percentLabelRect, "%");
                    if (int.TryParse(amountPercent, out int resultPercent))
                    {
                        itemStack.Amount = resultPercent;
                    }
                    break;
                case DialogItems.ValueMode.GInt:
                    if (_gvarSelectionEditors.TryGetValue(i, out var gvarSelectionEditor))
                    {
                        var dropdownListRect = new Rect(position.x + 120, yOffset, 300, 20);
                        var dropdownValueRect = new Rect(position.x + 120, yOffset + 20, 300, 20);

                        if (gvarSelectionEditor.DrawGvarEditor(dropdownListRect, dropdownValueRect))
                        {
                            itemStack.AmountVar = gvarSelectionEditor.CurrentSelectedGvar.Cast<GInt>();
                        }
                    }
                    else
                    {
                        // In case of switching from another mode to GInt
                        _gvarSelectionEditors.TryAdd(i, new GUIGvarSelectionEditor(GvarType.INT));
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
