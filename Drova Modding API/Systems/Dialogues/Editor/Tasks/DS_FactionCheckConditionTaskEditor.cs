using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Factions;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_FactionCheckConditionTask"/>
    /// </summary>
    public class DS_FactionCheckConditionTaskEditor : DrawTaskEditor
    {
        private DS_FactionCheckConditionTask? _castedTask;
        private Faction[]? _factions;
        private string[]? _factionNames;
        private GUIDropdownWithFilter? _factionDropdown;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_FactionCheckConditionTask>();
            if (_castedTask == null) return;

            _factions = Resources.FindObjectsOfTypeAll<Faction>();
            List<string> list = new();
            foreach (Faction faction in _factions)
            {
                list.Add(faction.name);
            }
            _factionNames = [.. list];

            int selectedIndex = -1;
            if (_castedTask._faction != null)
            {
                selectedIndex = Array.FindIndex(_factions, (faction) => faction.GUID == _castedTask._faction.GUID);
            }

            _factionDropdown = new GUIDropdownWithFilter(_factionNames, selectedIndex, 20);
        }

        /// <inheritdoc />
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null || _factionDropdown == null || _factions == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float totalHeight = 60f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_FactionCheckConditionTask", "Check if owner is following Faction"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "Faction:");

            if (_factionDropdown.Draw(new Rect(x + 110, y, width - 120, rowH)) && _factionDropdown.SelectedIndex >= 0)
            {
                _castedTask._faction = _factions[_factionDropdown.SelectedIndex];
            }

            return drawRect;
        }

    }
}