using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CheckLootInventoryEmptyConditionTask"/>
    /// </summary>
    public class DS_CheckLootInventoryEmptyConditionTaskEditor : DrawTaskEditor
    {
        private DS_CheckLootInventoryEmptyConditionTask? _castedTask;
        private EntityInfo[]? _entityInfos;
        private string[]? _entityInfoNames;
        private GUIDropdownWithFilter? _entityInfoDropdown;

        /// <inheritdoc />
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CheckLootInventoryEmptyConditionTask>();
            if (_castedTask == null) return;

            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>();
            List<string> list = new();
            foreach (EntityInfo entityInfo in _entityInfos)
            {
                list.Add(entityInfo.name);
            }
            _entityInfoNames = [.. list];

            int selectedIndex = -1;
            if (_castedTask._targetEntityInfo != null)
            {
                selectedIndex = Array.FindIndex(_entityInfos, (entityInfo) => entityInfo.GUID == _castedTask._targetEntityInfo.GUID);
            }

            _entityInfoDropdown = new GUIDropdownWithFilter(_entityInfoNames, selectedIndex, 20);
        }

        /// <inheritdoc />
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null || _entityInfoDropdown == null || _entityInfos == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float totalHeight = 60f;

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            Rect drawRect = new(position.x, position.y, width, totalHeight);
            GUI.Box(drawRect, new GUIContent("DS_CheckLootInventoryEmpty", "Check before OpenLootWindow if inventory is not empty. If no EntityInfo is defined, the Owner of this graph is checked."), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "EntityInfo:");

            if (_entityInfoDropdown.Draw(new Rect(x + 110, y, width - 120, rowH)) && _entityInfoDropdown.SelectedIndex >= 0)
            {
                _castedTask._targetEntityInfo = _entityInfos[_entityInfoDropdown.SelectedIndex];
            }

            return drawRect;
        }
    }
}