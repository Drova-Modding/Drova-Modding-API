using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    /// <summary>
    /// A simple dropdown UI element for Unity Editor GUI.
    /// </summary>
    public class GUIDropdown
    {
        /// <summary>
        /// Whether the dropdown is currently shown.
        /// </summary>
        protected bool _showDropdown = false;
        /// <summary>
        /// The index of the selected option.
        /// </summary>
        protected int _selectedIndex = 0;
        /// <summary>
        /// The list of options for the dropdown.
        /// </summary>
        protected readonly string[] _options;
        /// <summary>
        /// The default styles for the dropdown.
        /// </summary>
        protected readonly GUIStyle _defaultStyle;
        /// <summary>
        /// The highlight style for the selected option.
        /// </summary>
        protected readonly GUIStyle _highlightStyle;

        /// <summary>
        /// The message to show when no options are available.
        /// </summary>
        public string NoOptionsMessage = "No options available";

        /// <summary>
        /// The message to show when no option is selected.
        /// </summary>
        public string EmptyOptionMessage = "No option selected";

        /// <summary>
        /// The index of the selected option.
        /// </summary>
        public int SelectedIndex => _selectedIndex;
        /// <summary>
        /// The selected option.
        /// </summary>
        public string SelectedOption => _options[_selectedIndex];

        /// <summary>
        /// Whether the dropdown is currently shown.
        /// </summary>
        public bool IsDropdownShown => _showDropdown;

        /// <summary>
        /// The number of options in the dropdown.
        /// </summary>
        public int OptionsCount => _options.Length;

        /// <summary>
        /// Initializes a new instance of the GUIDropdown class.
        /// </summary>
        /// <param name="options">The list of options for the dropdown.</param>
        /// <param name="selectedIndex">The index of the selected option. Set -1 for no selection</param>
        public GUIDropdown(string[] options, int selectedIndex)
        {
            _options = options;
            _selectedIndex = selectedIndex;

            // Initialize default styles
            _defaultStyle = new GUIStyle(GUI.skin.button);
            _defaultStyle.normal.textColor = Color.white;
            _defaultStyle.normal.background = CreateTexture(2, 2, Color.black);

            // Highlight style with a different background color
            _highlightStyle = new GUIStyle(GUI.skin.button);
            _highlightStyle.normal.textColor = Color.yellow;
            _highlightStyle.normal.background = CreateTexture(2, 2, new Color(0.2f, 0.5f, 0.8f, 0.8f));
        }

        /// <summary>
        /// Draws the dropdown UI and handles interactions.
        /// </summary>
        /// <param name="dropdownRect">The position and size of the dropdown.</param>
        /// <returns>True if the selected index changed; otherwise, false.</returns>
        public virtual bool Draw(Rect dropdownRect)
        {
            RenderSelectedOption(dropdownRect);

            if (_showDropdown)
            {                
                return RenderDropdownOptions(dropdownRect, _options);
            }

            return false;
        }

        /// <summary>
        /// Renders the selected option.
        /// If no options are available, a message is shown.
        /// If no option is selected, then nothing is shown.
        /// </summary>
        /// <param name="dropdownRect"></param>
        protected void RenderSelectedOption(Rect dropdownRect)
        {

            if (_options.Length == 0)
            {
                GUI.Label(new Rect(dropdownRect.x, dropdownRect.y, dropdownRect.width, dropdownRect.height), NoOptionsMessage);
                return;
            }

            if (GUI.Button(new Rect(dropdownRect.x, dropdownRect.y, dropdownRect.width, dropdownRect.height), _selectedIndex == -1 ? EmptyOptionMessage : SelectedOption))
            {
                _showDropdown = !_showDropdown;
            }
        }

        /// <summary>
        /// Renders the dropdown options.
        /// </summary>
        /// <param name="dropdownRect"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        protected bool RenderDropdownOptions(Rect dropdownRect, string[] options)
        {
            int previousDepth = GUI.depth;
            
            GUI.depth = -1;
            bool selectionChanged = false;
            for (int i = 0; i < options.Length; i++)
            {
                GUIStyle style = (i == _selectedIndex) ? _highlightStyle : _defaultStyle;
                if (GUI.Button(new Rect(dropdownRect.x, dropdownRect.y + dropdownRect.height * (i + 1), dropdownRect.width, dropdownRect.height), options[i], style))
                {
                    if (i != _selectedIndex)
                    {
                        _selectedIndex = i;
                        selectionChanged = true;
                    }
                    _showDropdown = false;
                }
            }
            GUI.depth = previousDepth;
            return selectionChanged;
        }

        /// <summary>
        /// Creates a solid color texture for GUI backgrounds.
        /// </summary>
        /// <param name="width">Width of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="color">Color of the texture.</param>
        /// <returns>A Texture2D with the specified color.</returns>
        private static Texture2D CreateTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
