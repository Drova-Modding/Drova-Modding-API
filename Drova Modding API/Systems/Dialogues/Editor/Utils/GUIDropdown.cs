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

        // ── Deferred overlay system ──────────────────────────────────────────
        // GUI.depth only affects ordering between separate OnGUI callbacks.
        // Within one OnGUI, last-drawn = on top.  We therefore defer all
        // dropdown overlays to after all nodes have been drawn.

        private static readonly List<(GUIDropdown dd, Rect rect)> _pendingOverlays = [];

        // Set by FlushOverlays; consumed (and cleared) on the next Draw() call
        private bool _changedThisFlush;

        /// <summary>
        /// Draws all deferred dropdown overlays.
        /// Call this once per frame, AFTER all nodes / controls have been drawn,
        /// so that open dropdowns are always painted on top of everything else.
        /// </summary>
        public static void FlushOverlays(Rect visibleRect)
        {
            // Snapshot + clear BEFORE drawing in case an item click triggers
            // re-entrant Draw() calls that add new entries.
            (GUIDropdown dd, Rect rect)[] toFlush = [.. _pendingOverlays];
            var orderedFlush = toFlush.OrderBy(t => -t.rect.y).ThenBy(t => -t.rect.x);
            _pendingOverlays.Clear();
            foreach ((GUIDropdown dd, Rect rect) in orderedFlush)
            {
                if(!visibleRect.Overlaps(rect)) continue;
                if (dd.DrawOptions(rect))
                    dd._changedThisFlush = true;
            }
        }
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes a new instance of the GUIDropdown class.
        /// </summary>
        /// <param name="options">The list of options for the dropdown.</param>
        /// <param name="selectedIndex">The index of the selected option. Set -1 for no selection</param>
        public GUIDropdown(string[] options, int selectedIndex)
        {
            _options = options;
            _selectedIndex = selectedIndex;

            Texture2D blackTexture = CreateTexture(2, 2, Color.black);
            Texture2D hoverTexture = CreateTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 1.0f));
            Texture2D highlightTexture = CreateTexture(2, 2, new Color(0.2f, 0.5f, 0.8f, 1.0f));

            // Initialize default styles
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

            // Highlight style with a different background color
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
        /// <param name="index"></param>
        public virtual void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _options.Length)
                _selectedIndex = index;
        }

        /// <summary>
        /// Draws the dropdown UI and handles interactions.
        /// The dropdown overlay (options list) is NOT drawn immediately; it is
        /// registered for deferred rendering via <see cref="FlushOverlays"/> so
        /// it always appears on top of all other controls.
        /// </summary>
        /// <param name="dropdownRect">The position and size of the dropdown.</param>
        /// <returns>
        /// True if the selected index changed since the last call;
        /// the result reflects the previous frame's selection (one-frame lag).
        /// </returns>
        public virtual bool Draw(Rect dropdownRect)
        {
            // Consume and reset the flag so it only returns true for one frame
            bool changed = _changedThisFlush;
            _changedThisFlush = false;

            RenderSelectedOption(dropdownRect);

            // Register for deferred overlay drawing if the list is open
            if (_showDropdown)
                _pendingOverlays.Add((this, dropdownRect));

            return changed;
        }

        /// <summary>
        /// Renders the selected option.
        /// If no options are available, a message is shown.
        /// If no option is selected, then nothing is shown.
        /// </summary>
        /// <param name="dropdownRect"></param>
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
        /// <param name="dropdownRect"></param>
        /// <returns></returns>
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
        /// <param name="dropdownRect"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual bool RenderDropdownOptions(Rect dropdownRect, string[] options)
        {
            Rect backgroundRect = new Rect(dropdownRect.x, dropdownRect.y + dropdownRect.height, dropdownRect.width, dropdownRect.height * options.Length);

            // Close dropdown if clicked outside the dropdown area (main button or options)
            if (_showDropdown && Event.current.type == EventType.MouseDown && !backgroundRect.Contains(Event.current.mousePosition) && !dropdownRect.Contains(Event.current.mousePosition))
            {
                _showDropdown = false;
            }

            if (!_showDropdown)
            {
                return false;
            }
            // Draw a background box for the entire dropdown area to ensure full opacity
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
