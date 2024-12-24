using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    internal class GUIDropdown
    {
        private bool showDropdown = false;
        private int selectedIndex = 0;
        private readonly string[] options;
        private readonly GUIStyle defaultStyle;
        private readonly GUIStyle highlightStyle;

        public int SelectedIndex => selectedIndex;
        public string SelectedOption => options[selectedIndex];

        /// <summary>
        /// Initializes a new instance of the GUIDropdown class.
        /// </summary>
        /// <param name="options">The list of options for the dropdown.</param>
        /// <param name="selectedIndex">The index of the selected option.</param>
        public GUIDropdown(string[] options, int selectedIndex)
        {
            this.options = options;
            this.selectedIndex = selectedIndex;

            // Initialize default styles
            defaultStyle = new GUIStyle(GUI.skin.button);

            // Highlight style with a different background color
            highlightStyle = new GUIStyle(GUI.skin.button);
            highlightStyle.normal.textColor = Color.yellow;
            highlightStyle.normal.background = CreateTexture(2, 2, new Color(0.2f, 0.5f, 0.8f, 0.8f));
        }

        /// <summary>
        /// Draws the dropdown UI and handles interactions.
        /// </summary>
        /// <param name="dropdownRect">The position and size of the dropdown.</param>
        /// <returns>True if the selected index changed; otherwise, false.</returns>
        public bool Draw(Rect dropdownRect)
        {
            bool selectionChanged = false;

            if (GUI.Button(new Rect(dropdownRect.x, dropdownRect.y, dropdownRect.width, dropdownRect.height), options[selectedIndex]))
            {
                showDropdown = !showDropdown;
            }

            if (showDropdown)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    GUIStyle style = (i == selectedIndex) ? highlightStyle : defaultStyle;
                    if (GUI.Button(new Rect(dropdownRect.x, dropdownRect.y + dropdownRect.height * (i + 1), dropdownRect.width, dropdownRect.height), options[i], style))
                    {
                        if (i != selectedIndex)
                        {
                            selectedIndex = i;
                            selectionChanged = true;
                        }
                        showDropdown = false;
                    }
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

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
