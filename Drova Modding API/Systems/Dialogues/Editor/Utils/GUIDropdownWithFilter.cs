using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    /// <summary>
    /// A dropdown with a filter.
    /// </summary>
    /// <remarks>
    /// Create a new dropdown with a filter.
    /// </remarks>
    /// <param name="options">Options to shown</param>
    /// <param name="selectedIndex">Index of the option</param>
    /// <param name="maxItems">Max items to display</param>
    /// <param name="filter">starting filter</param>
    public class GUIDropdownWithFilter(string[] options, int selectedIndex, int maxItems, string filter = "") : GUIDropdown(options, selectedIndex)
    {
        private string _filter = filter;

        private readonly int _maxItems = maxItems;

        private string[] _filteredOptions = options.Take(maxItems).ToArray();

        private void OnFilter()
        {
            _filteredOptions = _options.Where(option => option.Contains(_filter)).Take(_maxItems).ToArray();
        }

        /// <inheritdoc/>
        public override bool DrawOptions(Rect dropdownRect)
        {
            if (!_showDropdown)
            {
                return false;
            }

            // Draw background for filter and options area
            // Area includes filter (dropdownRect.height) + options (dropdownRect.height * count)
            Rect totalRect = new Rect(dropdownRect.x, dropdownRect.y + dropdownRect.height, dropdownRect.width, dropdownRect.height * (1 + _filteredOptions.Length));
            
            // Close dropdown if clicked outside the dropdown area (main button or filter/options area)
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
            string previous_filter = _filter;
            GUI.Label(filterRect, "Filter:");
            filterRect.x += 40;
            filterRect.width -= 40;
            _filter = GUI.TextField(filterRect, _filter);
            if (_filter != previous_filter)
            {
                OnFilter();
            }
        }
    }
}
