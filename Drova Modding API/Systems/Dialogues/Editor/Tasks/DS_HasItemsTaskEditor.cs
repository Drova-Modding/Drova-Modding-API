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

        private DS_HasItems? _castedTask;

        private EntityInfo[] _entityInfos;
        private Item[] _items;
        private GUIDropdownWithFilter _entityInfoDropdown;
        private readonly List<GUIDropdownWithFilter> _itemDropdowns = [];
        private readonly List<GUIDropdown> _valueModeDropdowns = [];
        private readonly Dictionary<int, GUIGvarSelectionEditor> _gvarSelectionEditors = [];

        // Cached height from previous frame so we can draw the background box first
        private float _lastHeight = 150f;

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

            const float width = 340f;
            const float rowH = 22f;
            const float rowStep = 27f; // row height + small gap

            float x = position.x;

            // Draw background box FIRST (using last frame's height) so controls appear on top
            Color previousColor = GUI.color;
            GUI.color = Color.blue;
            Rect drawRect = new(x, position.y, width, _lastHeight);
            GUI.Box(drawRect, "Has Items Task");
            GUI.color = previousColor;

            // Controls — start below the box title
            float y = position.y + rowStep;

            // Target Entity
            GUI.Label(new Rect(x + 5, y, 100, rowH), _targetContent);
            if (_entityInfoDropdown.Draw(new Rect(x + 110, y, width - 115, rowH)))
            {
                _castedTask.Target = _entityInfos[_entityInfoDropdown.SelectedIndex];
            }
            y += rowStep + 5;

            for (int i = 0; i < _castedTask.Items.Count; i++)
            {
                DialogItems item = _castedTask.Items[i];

                // Item
                GUI.Label(new Rect(x + 5, y, 100, rowH), "Item:");
                if (_itemDropdowns[i].Draw(new Rect(x + 110, y, width - 115, rowH)))
                {
                    item.Item = _items[_itemDropdowns[i].SelectedIndex];
                }
                y += rowStep;

                // Valuemode
                GUI.Label(new Rect(x + 5, y, 100, rowH), "Valuemode:");
                if (_valueModeDropdowns[i].Draw(new Rect(x + 110, y, width - 115, rowH)))
                {
                    item.Mode = (DialogItems.ValueMode)_valueModeDropdowns[i].SelectedIndex;
                }
                y += rowStep;

                // Amount / GVar
                if (item.Mode != DialogItems.ValueMode.GInt)
                {
                    GUI.Label(new Rect(x + 5, y, 100, rowH), "Amount:");
                    DrawAmountEditFields(new Vector2(x, y), i, item);
                    y += rowStep;
                }
                else if (_gvarSelectionEditors.TryGetValue(i, out GUIGvarSelectionEditor gvarEditor))
                {
                    if (gvarEditor.DrawGvarEditor(new Rect(x + 110, y, width - 115, rowH), new Rect(x + 110, y + rowStep, width - 115, rowH)))
                    {
                        item.AmountVar = gvarEditor.CurrentSelectedGvar!.TryCast<GInt>();
                    }
                    y += rowStep * 2;
                }

                // Use Container
                item.UseContainer = GUI.Toggle(new Rect(x + 5, y, 160, rowH), item.UseContainer, "Use Container");
                y += rowStep;

                // Remove button
                if (GUI.Button(new Rect(x + 5, y, 80, rowH), "X"))
                {
                    _castedTask.Items.RemoveAt(i);
                    _itemDropdowns.RemoveAt(i);
                    _valueModeDropdowns.RemoveAt(i);
                    _gvarSelectionEditors.Remove(i);
                    i--;
                    continue;
                }
                y += rowStep + 8; // extra gap between items
            }

            // Add Item button
            if (GUI.Button(new Rect(x + 5, y, 100, rowH), "Add Item"))
            {
                _castedTask.Items.Add(new DialogItems());
                _itemDropdowns.Add(new GUIDropdownWithFilter([.. _items.Select(e => e.ReadableId)], 0, 20));
                _valueModeDropdowns.Add(new GUIDropdown(Enum.GetNames<DialogItems.ValueMode>(), 0));
                _gvarSelectionEditors.TryAdd(_castedTask.Items.Count - 1, new GUIGvarSelectionEditor(GvarType.INT));
            }
            y += rowStep + 5;

            // Cache computed height for next frame's background box
            _lastHeight = y - position.y;
            drawRect.height = _lastHeight;
            return drawRect;
        }

        private void DrawAmountEditFields(Vector2 position, int i, DialogItems itemStack)
        {
            switch (itemStack.Mode)
            {
                case DialogItems.ValueMode.Int:
                    string amountInt = GUI.TextField(
                        new Rect(position.x + 110, position.y, 220, 22),
                        itemStack.Amount.ToString());
                    if (int.TryParse(amountInt, out int resultInt))
                    {
                        itemStack.Amount = resultInt;
                    }
                    break;

                case DialogItems.ValueMode.Percentage:
                    string amountPercent = GUI.TextField(
                        new Rect(position.x + 110, position.y, 195, 22),
                        itemStack.Amount.ToString());
                    GUI.Label(new Rect(position.x + 310, position.y, 20, 22), "%");
                    if (float.TryParse(amountPercent, out float resultPercent))
                    {
                        itemStack.Percentage = resultPercent;
                    }
                    break;

                case DialogItems.ValueMode.GInt:
                    // Handled inline in DrawTask (needs two rows)
                    if (!_gvarSelectionEditors.ContainsKey(i))
                    {
                        _gvarSelectionEditors.TryAdd(i, new GUIGvarSelectionEditor(GvarType.INT));
                    }
                    break;
            }
        }
    }
}
