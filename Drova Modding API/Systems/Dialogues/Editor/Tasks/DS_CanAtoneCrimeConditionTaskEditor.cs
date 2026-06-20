using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Items;
using Il2CppSystem.Linq;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CanAtoneCrimeConditionTask"/>
    /// </summary>
    public class DS_CanAtoneCrimeConditionTaskEditor : DrawTaskEditor
    {
        private DS_CanAtoneCrimeConditionTask _castedTask;
        private Item[] _items;
        private GUIDropdownWithFilter _itemDropdown;
        private float _lastHeight = 100f;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CanAtoneCrimeConditionTask>();
            _items = ProviderAccess.GetGameDatabase().Items.GetItems().ToArray();

            int selectedIndex = -1;
            if (_castedTask.Shard != null)
            {
                selectedIndex = Array.FindIndex(_items, i => i.Guid == _castedTask.Shard.Guid);
            }

            _itemDropdown = new GUIDropdownWithFilter(_items.Select(i => i.ReadableId).ToArray(), selectedIndex, 20);
        }

        /// <inheritdoc />
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float rowStep = 27f;

            float x = position.x;
            float y = position.y + rowStep;

            Color previousColor = GUI.color;
            GUI.color = Color.blue;
            Rect drawRect = new(x, position.y, width, _lastHeight);
            GUI.Box(drawRect, "Can Atone Crime Condition Task");
            GUI.color = previousColor;

            GUI.Label(new Rect(x + 5, y, 100, rowH), new GUIContent("Shard", "Check if owner witnessed crime of player"));
            if (_itemDropdown.Draw(new Rect(x + 110, y, width - 115, rowH)))
            {
                _castedTask.Shard = _items[_itemDropdown.SelectedIndex];
            }
            y += rowStep + 5;

            _lastHeight = y - position.y;
            drawRect.height = _lastHeight;
            return drawRect;
        }
    }
}