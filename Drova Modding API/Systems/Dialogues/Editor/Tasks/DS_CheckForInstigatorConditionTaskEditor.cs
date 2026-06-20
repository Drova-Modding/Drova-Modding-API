using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppDrova.DialogueNew;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_CheckForInstigatorConditionTask"/>
    /// </summary>
    public class DS_CheckForInstigatorConditionTaskEditor : DrawTaskEditor
    {
        private DS_CheckForInstigatorConditionTask? _castedTask;
        private EntityInfo[]? _entityInfos;
        private string[]? _entityInfoNames;
        private GUIDropdownWithFilter? _entityInfoDropdown;

        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_CheckForInstigatorConditionTask>();
            if (_castedTask == null) return;

            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>();
            List<string> list = new();
            foreach (EntityInfo e in _entityInfos)
            {
                list.Add(e.name);
            }
            _entityInfoNames = [.. list];

            int selectedIndex = -1;
            if (_castedTask.entityInfo != null)
            {
                selectedIndex = Array.FindIndex(_entityInfos, (e) => e.GUID == _castedTask.entityInfo.GUID);
            }

            _entityInfoDropdown = new GUIDropdownWithFilter(_entityInfoNames, selectedIndex, 20);
        }

        /// <inheritdoc/>
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
            GUI.Box(drawRect, new GUIContent("DS_CheckForInstigator", "Checks if EntityInfo is INSTIGATOR"), EditorBoxStyles.Task);
            GUI.color = previousColor;

            float x = position.x + 5;
            float y = position.y + 25;

            GUI.Label(new Rect(x, y, 100, rowH), "EntityInfo:");

            if (_entityInfoDropdown.Draw(new Rect(x + 110, y, width - 120, rowH)))
            {
                _castedTask.entityInfo = _entityInfos[_entityInfoDropdown.SelectedIndex];
            }

            return drawRect;
        }
    }
}