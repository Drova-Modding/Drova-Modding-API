using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    /// <summary>
    /// A dropdown with a filter.
    /// </summary>
    public class GUIDropdownWithFilter(string[] options, int selectedIndex, int maxItems, string filter = "") : GUIDropdown(options, selectedIndex)
    {
        private string _filter = filter;

        private string[] _filteredOptions = options.Take(maxItems).ToArray();

        private void OnFilter()
        {
            _filteredOptions = _options.Where(option => option.Contains(_filter, StringComparison.OrdinalIgnoreCase)).Take(maxItems).ToArray();
        }

        /// <inheritdoc/>
        public override void SetSelectedIndex(int index)
        {
            if (string.IsNullOrWhiteSpace(_filter))
            {
                base.SetSelectedIndex(index);
            }
            else
            {
                int originalIndex = Array.IndexOf(_options, _filteredOptions[index]);
                base.SetSelectedIndex(originalIndex);
            }
        }

        /// <inheritdoc/>
        public override bool DrawOptions(Rect dropdownRect)
        {
            if (!_showDropdown)
            {
                return false;
            }

            Rect totalRect = new(dropdownRect.x, dropdownRect.y + dropdownRect.height, dropdownRect.width, dropdownRect.height * (1 + _filteredOptions.Length));

            if (Event.current.type == EventType.MouseDown && !totalRect.Contains(Event.current.mousePosition) && !dropdownRect.Contains(Event.current.mousePosition))
            {
                _showDropdown = false;
                return false;
            }

            int previousDepth = GUI.depth;
            GUI.depth = -2000;

            GUI.Box(totalRect, "", _defaultStyle);

            RenderFilter(dropdownRect);

            bool changed = RenderDropdownOptions(new Rect(dropdownRect.x, dropdownRect.y + dropdownRect.height, dropdownRect.width, dropdownRect.height), _filteredOptions);
            GUI.depth = previousDepth;
            return changed;
        }

        private void RenderFilter(Rect dropdownRect)
        {
            Rect filterRect = new(dropdownRect.x, dropdownRect.y + dropdownRect.height, dropdownRect.width, dropdownRect.height);
            string previousFilter = _filter;
            GUI.Label(filterRect, "Filter:");
            filterRect.x += 40;
            filterRect.width -= 40;
            _filter = GUI.TextField(filterRect, _filter);
            if (_filter != previousFilter)
            {
                OnFilter();
            }
        }
    }
}
