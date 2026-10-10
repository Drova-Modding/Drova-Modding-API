using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Factions;
using Il2CppDrova.Items;
using Il2CppSystem.Linq;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_JudgeCanAtoneCrimeConditionTask"/>
    /// </summary>
    public class DS_JudgeCanAtoneCrimeConditionTaskEditor : DrawTaskEditor
    {
        private DS_JudgeCanAtoneCrimeConditionTask? _castedTask;
        private Item[]? _items;
        private Faction[]? _factions;
        private GUIDropdownWithFilter? _itemDropdown;
        private GUIDropdownWithFilter? _factionDropdown;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_JudgeCanAtoneCrimeConditionTask>();
            if (_castedTask == null) return;

            SubDatabase_Item? itemDatabase = ProviderAccess.ItemDatabase;
            Item[] items = itemDatabase != null ? itemDatabase.GetItems().ToArray() : [];
            Faction[] factions = Resources.FindObjectsOfTypeAll<Faction>();
            _items = items;
            _factions = factions;

            int selectedItemIndex = -1;
            Item payment = _castedTask.Payment;
            if (payment != null)
            {
                selectedItemIndex = Array.FindIndex(items, (item) => item.Guid == payment.Guid);
            }

            int selectedFactionIndex = -1;
            Faction faction = _castedTask.Faction;
            if (faction != null)
            {
                selectedFactionIndex = Array.FindIndex(factions, (candidate) => candidate.GUID == faction.GUID);
            }

            _itemDropdown = new GUIDropdownWithFilter(items.Select((item) => item.ReadableId).ToArray(), selectedItemIndex, 20);
            _factionDropdown = new GUIDropdownWithFilter(factions.Select((candidate) => candidate.name).ToArray(), selectedFactionIndex, 20);
        }

        /// <inheritdoc />
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null || _items == null || _factions == null || _itemDropdown == null || _factionDropdown == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float totalHeight = 85f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_JudgeCanAtoneCrimeConditionTask", "Check for all judgable crimes of player"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "Payment:");
            if (_itemDropdown.Draw(new Rect(x + 110, y, width - 120, rowH)) && _itemDropdown.SelectedIndex >= 0)
            {
                _castedTask.Payment = _items[_itemDropdown.SelectedIndex];
            }

            y += 25;
            GUI.Label(new Rect(x, y, 100, rowH), "Faction:");
            if (_factionDropdown.Draw(new Rect(x + 110, y, width - 120, rowH)) && _factionDropdown.SelectedIndex >= 0)
            {
                _castedTask.Faction = _factions[_factionDropdown.SelectedIndex];
            }

            return drawRect;
        }
    }
}