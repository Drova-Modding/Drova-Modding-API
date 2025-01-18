using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.Items;
using Il2CppSystem.Linq;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_HasItems"/>
    /// </summary>
    internal class DS_HasItemsTaskEditor : DrawTaskEditor
    {

        private DS_HasItems _castedTask;

        private EntityInfo[] _entityInfos;
        private Item[] _items;
        private GUIDropdownWithFilter _entityInfoDropdown;
        private readonly List<GUIDropdownWithFilter> _itemDropdowns = [];
        private readonly List<GUIDropdown> _valueModeDropdowns = [];
        private readonly Dictionary<int, GUIGvarSelectionEditor> _gvarSelectionEditors = [];

        private readonly GUIContent _targetContent = new("Target Entity", "If empty, than the player is checked");

        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_HasItems>();
            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>()
                                    .GroupBy(e => e.name)
                                    .Select(g => g.First())
                                    .ToArray();
            _items = ProviderAccess.GetGameDatabase().Items.GetItems().ToArray();

            EntityInfo entityInfo = _castedTask.Target;
            _entityInfoDropdown = new GUIDropdownWithFilter(_entityInfos.Select(e => e.name).ToArray(), Array.IndexOf(_entityInfos, entityInfo), 20);
            for (int i = 0; i < _castedTask.Items.Count; i++)
            {
                DialogItems item = _castedTask.Items[i];
                int selectedIndex = Array.FindIndex(_items, id => id.Guid == item.Item.Guid);
                _itemDropdowns.Add(new GUIDropdownWithFilter(_items.Select(e => e.ReadableId).ToArray(), selectedIndex, 20));
                _valueModeDropdowns.Add(new GUIDropdown(Enum.GetNames<DialogItems.ValueMode>(), (int)item.Mode));
                if (item.Mode == DialogItems.ValueMode.GInt)
                {
                    _gvarSelectionEditors.TryAdd(i, new GUIGvarSelectionEditor(GvarType.INT, item.AmountVar.GetParent().name, false, item.AmountVar));
                }
            }
        }

        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }
            Rect rect = new(position.x, position.y, 220, 20);
            GUI.Label(new Rect(rect.x + 5, position.y + 20, 100, 20), _targetContent);
            if (_entityInfoDropdown.Draw(new(rect.x + 110, rect.y + 20, rect.width, rect.height)))
            {
                _castedTask.Target = _entityInfos[_entityInfoDropdown.SelectedIndex];
            }

            float itemHeight = 260f;

            for (int i = 0; i < _castedTask.Items.Count; i++)
            {
                DialogItems item = _castedTask.Items[i];

                if (GUI.Button(new(rect.x + 10, rect.y + 120 + (20 * i), 20, 20), "X"))
                {
                    _castedTask.Items.RemoveAt(i);
                    _itemDropdowns.RemoveAt(i);
                    _valueModeDropdowns.RemoveAt(i);
                    _gvarSelectionEditors.Remove(i);
                    i--;
                    continue;
                }
                float yOffset = position.y + 30 + (i * itemHeight);

                if (item.Mode == DialogItems.ValueMode.GInt)
                {
                    yOffset += 120;
                }

                GUI.Label(new Rect(position.x + 10, yOffset, 100, 20), "Item:");

                GUI.Label(new Rect(position.x + 10, yOffset + 20, 100, 20), "Valuemode:");

                if (item.Mode != DialogItems.ValueMode.GInt)
                {
                    GUI.Label(new Rect(position.x + 10, yOffset + 40, 100, 20), "Amount:");
                }

                DrawAmountEditFields(rect.position, i, item, rect.y + 0 + (20 * i));

                item.UseContainer = GUI.Toggle(new Rect(rect.x + 10, rect.y + 100 + (20 * i), 100, 20), item.UseContainer, "Use Container");

                if (item.Mode == DialogItems.ValueMode.GInt)
                {
                    if (_gvarSelectionEditors[i].DrawGvarEditor(new(rect.x, rect.y + 80 + (20 * i), rect.width, rect.height)))
                    {
                        item.AmountVar = _gvarSelectionEditors[i].CurrentSelectedGvar.TryCast<GInt>();
                    }
                }

                if (_valueModeDropdowns[i].Draw(new(rect.x, rect.y + 60 + (20 * i), rect.width, rect.height)))
                {
                    item.Mode = (DialogItems.ValueMode)_valueModeDropdowns[i].SelectedIndex;
                }

                if (_itemDropdowns[i].Draw(new(rect.x, rect.y + 40 + (20 * i), rect.width, rect.height)))
                {
                    item.Item = _items[_itemDropdowns[i].SelectedIndex];
                }
            }

            rect.height += itemHeight * _castedTask.Items.Count;

            // Add item button
            if (GUI.Button(new(rect.x + 10, rect.y + 20 + (itemHeight * _castedTask.Items.Count), 100, 20), "Add Item"))
            {
                _castedTask.Items.Add(new DialogItems());
                _itemDropdowns.Add(new GUIDropdownWithFilter(_items.Select(e => e.ReadableId).ToArray(), 0, 20));
                _valueModeDropdowns.Add(new GUIDropdown(Enum.GetNames<DialogItems.ValueMode>(), 0));
                _gvarSelectionEditors.TryAdd(_castedTask.Items.Count - 1, new GUIGvarSelectionEditor(GvarType.INT, null, false));
            }

            rect.height += 20 * (_entityInfoDropdown.IsDropdownShown ? 20 : 1);
            Color previousColor = GUI.color;
            GUI.color = Color.blue;
            Rect drawRect = new(position.x, position.y, 400, rect.height);
            GUI.Box(drawRect, "Has Items Task");
            GUI.color = previousColor;
            return drawRect;

        }

        private void DrawAmountEditFields(Vector2 position, int i, DialogItems itemStack, float yOffset)
        {
            switch (itemStack.Mode)
            {
                case DialogItems.ValueMode.Int:
                    string amountInt = GUI.TextField(
                        new Rect(position.x + 120, yOffset, 200, 20),
                        itemStack.Amount.ToString());
                    if (int.TryParse(amountInt, out int resultInt))
                    {
                        itemStack.Amount = resultInt;
                    }
                    break;

                case DialogItems.ValueMode.Percentage:
                    Rect amountRect = new(position.x + 120, yOffset, 180, 20);
                    Rect percentLabelRect = new(position.x + 305, yOffset, 20, 20);
                    string amountPercent = GUI.TextField(amountRect, itemStack.Amount.ToString());
                    GUI.Label(percentLabelRect, "%");
                    if (float.TryParse(amountPercent, out float resultPercent))
                    {
                        itemStack.Percentage = resultPercent;
                    }
                    break;
                case DialogItems.ValueMode.GInt:
                    if (_gvarSelectionEditors.TryGetValue(i, out GUIGvarSelectionEditor gvarSelectionEditor))
                    {
                        Rect dropdownListRect = new(position.x + 120, yOffset, 300, 20);
                        Rect dropdownValueRect = new(position.x + 120, yOffset + 20, 300, 20);

                        if (gvarSelectionEditor.DrawGvarEditor(dropdownListRect, dropdownValueRect))
                        {
                            itemStack.AmountVar = gvarSelectionEditor.CurrentSelectedGvar.TryCast<GInt>();
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
