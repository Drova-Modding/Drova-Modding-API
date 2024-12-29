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
        public override bool Draw(Rect dropdownRect)
        {
            RenderSelectedOption(dropdownRect);

            if (_showDropdown)
            {
                RenderFilter(dropdownRect);

                return RenderDropdownOptions(new Rect(dropdownRect.x, dropdownRect.y + 20, dropdownRect.width, dropdownRect.height), _filteredOptions);
            }
            return false;
        }

        private void RenderFilter(Rect dropdownRect)
        {
            var filterRect = new Rect(dropdownRect.x, dropdownRect.y + 20, dropdownRect.width, dropdownRect.height);
            var previous_filter = _filter;
            GUI.Label(filterRect, "Filter:");
            filterRect.x += 35;
            _filter = GUI.TextField(filterRect, _filter);
            if (_filter != previous_filter)
            {
                OnFilter();
            }
        }
    }
}
