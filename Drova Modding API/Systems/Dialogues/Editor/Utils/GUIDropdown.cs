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
        protected bool _showDropdown;
        /// <summary>
        /// The index of the selected option.
        /// </summary>
        protected int _selectedIndex;
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
        public const string NoOptionsMessage = "No options available";

        /// <summary>
        /// The message to show when no option is selected.
        /// </summary>
        public const string EmptyOptionMessage = "No option selected";

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

        // GUI.depth only orders separate OnGUI callbacks. Within one OnGUI the last drawn
        // is on top, so open lists are deferred until every node has been drawn.
        private static readonly List<(GUIDropdown dd, Rect rect)> PendingOverlays = [];

        private bool _changedThisFlush;

        /// <summary>
        /// Draws all deferred dropdown lists. Call once per frame after everything else is drawn.
        /// </summary>
        public static void FlushOverlays(Rect visibleRect)
        {
            // Snapshot and clear before drawing, since an item click can re-enter Draw() and add entries.
            (GUIDropdown dd, Rect rect)[] toFlush = [.. PendingOverlays];
            var orderedFlush = toFlush.OrderBy(t => -t.rect.y).ThenBy(t => -t.rect.x);
            PendingOverlays.Clear();
            foreach ((GUIDropdown dd, Rect rect) in orderedFlush)
            {
                if(!visibleRect.Overlaps(rect)) continue;
                if (dd.DrawOptions(rect))
                    dd._changedThisFlush = true;
            }
        }

        /// <summary>
        /// Creates a dropdown. A <paramref name="selectedIndex"/> of -1 means no selection.
        /// </summary>
        public GUIDropdown(string[] options, int selectedIndex)
        {
            _options = options;
            _selectedIndex = selectedIndex;

            Texture2D blackTexture = CreateTexture(2, 2, Color.black);
            Texture2D hoverTexture = CreateTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 1.0f));
            Texture2D highlightTexture = CreateTexture(2, 2, new Color(0.2f, 0.5f, 0.8f, 1.0f));

            _defaultStyle = new GUIStyle(GUI.skin.button);
            _defaultStyle.normal.textColor = Color.white;
            _defaultStyle.normal.background = blackTexture;
            _defaultStyle.hover.background = hoverTexture;
            _defaultStyle.active.background = hoverTexture;
            _defaultStyle.focused.background = blackTexture;
            _defaultStyle.onNormal.background = blackTexture;
            _defaultStyle.onHover.background = hoverTexture;
            _defaultStyle.onActive.background = hoverTexture;
            _defaultStyle.onFocused.background = blackTexture;

            _highlightStyle = new GUIStyle(GUI.skin.button);
            _highlightStyle.normal.textColor = Color.yellow;
            _highlightStyle.normal.background = highlightTexture;
            _highlightStyle.hover.background = highlightTexture;
            _highlightStyle.active.background = highlightTexture;
            _highlightStyle.focused.background = highlightTexture;
            _highlightStyle.onNormal.background = highlightTexture;
            _highlightStyle.onHover.background = highlightTexture;
            _highlightStyle.onActive.background = highlightTexture;
            _highlightStyle.onFocused.background = highlightTexture;
        }

        /// <summary>
        /// Sets the selected index of the dropdown.
        /// </summary>
        public virtual void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _options.Length)
                _selectedIndex = index;
        }

        /// <summary>
        /// Draws the button. The open list is drawn later by <see cref="FlushOverlays"/> so it is on top.
        /// </summary>
        /// <returns>True if the selection changed, one frame late because the list is drawn after this call.</returns>
        public virtual bool Draw(Rect dropdownRect)
        {
            bool changed = _changedThisFlush;
            _changedThisFlush = false;

            RenderSelectedOption(dropdownRect);

            if (_showDropdown)
                PendingOverlays.Add((this, dropdownRect));

            return changed;
        }

        /// <summary>
        /// Renders the button showing the selected option.
        /// </summary>
        public virtual void RenderSelectedOption(Rect dropdownRect)
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
        /// Renders the dropdown options if the dropdown is open.
        /// </summary>
        public virtual bool DrawOptions(Rect dropdownRect)
        {
            if (_showDropdown)
            {
                return RenderDropdownOptions(dropdownRect, _options);
            }
            return false;
        }

        /// <summary>
        /// Renders the dropdown options.
        /// </summary>
        public virtual bool RenderDropdownOptions(Rect dropdownRect, string[] options)
        {
            Rect backgroundRect = new Rect(dropdownRect.x, dropdownRect.y + dropdownRect.height, dropdownRect.width, dropdownRect.height * options.Length);

            if (_showDropdown && Event.current.type == EventType.MouseDown && !backgroundRect.Contains(Event.current.mousePosition) && !dropdownRect.Contains(Event.current.mousePosition))
            {
                _showDropdown = false;
            }

            if (!_showDropdown)
            {
                return false;
            }
            GUI.Box(backgroundRect, "", _defaultStyle);

            bool selectionChanged = false;
            for (int i = 0; i < options.Length; i++)
            {
                Rect buttonRect = new Rect(dropdownRect.x, dropdownRect.y + (dropdownRect.height * (i + 1)), dropdownRect.width, dropdownRect.height);
                GUIStyle style = (i == _selectedIndex) ? _highlightStyle : _defaultStyle;
                if (GUI.Button(buttonRect, options[i], style))
                {
                    if (i != _selectedIndex)
                    {
                        SetSelectedIndex(i);
                        selectionChanged = true;
                    }
                    _showDropdown = false;
                    Event.current.Use();
                }
            }
            return selectionChanged;
        }

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
