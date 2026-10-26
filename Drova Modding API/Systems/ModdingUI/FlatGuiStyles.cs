using UnityEngine;

namespace Drova_Modding_API.Systems.ModdingUI
{
    /// <summary>
    /// Flat, square IMGUI styles on solid textures for the API's own windows. The default skin's textures are rounded.
    /// Built on first use, which must be inside <c>OnGUI</c>.
    /// </summary>
    internal static class FlatGuiStyles
    {
        private static GUIStyle? _panel;
        private static GUIStyle? _button;
        private static GUIStyle? _selectedButton;
        private static GUIStyle? _disabledButton;
        private static GUIStyle? _field;
        private static GUIStyle? _title;
        private static GUIStyle? _label;
        private static GUIStyle? _hint;
        private static GUIStyle? _outline;

        internal static GUIStyle Panel => _panel ??= new GUIStyle { normal = { background = Solid(new Color(0.09f, 0.09f, 0.11f, 0.96f)) } };

        internal static GUIStyle Button => _button ??= MakeButton(new Color(0.20f, 0.24f, 0.30f), new Color(0.28f, 0.34f, 0.42f), Color.white);

        internal static GUIStyle SelectedButton => _selectedButton ??= MakeButton(new Color(0.36f, 0.44f, 0.20f), new Color(0.42f, 0.52f, 0.24f), Color.white);

        internal static GUIStyle DisabledButton => _disabledButton ??= MakeButton(new Color(0.15f, 0.15f, 0.17f), new Color(0.15f, 0.15f, 0.17f), new Color(0.5f, 0.5f, 0.5f));

        internal static GUIStyle Field => _field ??= new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 6, 3, 3),
            normal = { background = Solid(new Color(0.16f, 0.16f, 0.19f)), textColor = Color.white },
            focused = { background = Solid(new Color(0.20f, 0.20f, 0.25f)), textColor = Color.white }
        };

        internal static GUIStyle Title => _title ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };

        internal static GUIStyle Label => _label ??= new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white } };

        internal static GUIStyle Hint => _hint ??= new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.75f, 0.75f, 0.75f) } };

        /// <summary>
        /// A frame only: a solid border around a transparent middle, for marking something in the world.
        /// </summary>
        internal static GUIStyle Outline => _outline ??= new GUIStyle
        {
            border = new RectOffset(2, 2, 2, 2),
            normal = { background = Frame(new Color(0.55f, 1f, 0.55f)) }
        };

        private static GUIStyle MakeButton(Color background, Color hover, Color text)
        {
            GUIStyle style = new()
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                fontStyle = FontStyle.Bold
            };
            style.normal.background = Solid(background);
            style.normal.textColor = text;
            style.hover.background = Solid(hover);
            style.hover.textColor = text;
            style.active.background = Solid(hover);
            style.active.textColor = text;
            return style;
        }

        private static Texture2D Solid(Color color)
        {
            Texture2D texture = new(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D Frame(Color color)
        {
            Texture2D texture = new(5, 5) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    bool edge = x < 2 || y < 2 || x > 2 || y > 2;
                    texture.SetPixel(x, y, edge ? color : Color.clear);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
