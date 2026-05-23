using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    /// <summary>
    /// Opaque, category-tinted <see cref="GUIStyle"/> boxes for the dialogue graph editor.
    /// Default <c>GUI.skin.box</c> has a translucent border texture that disappears against
    /// the in-game backdrop, so each category gets its own solid background here.
    /// Lazily initialized — must be touched first from inside <c>OnGUI</c>.
    /// </summary>
    public static class EditorBoxStyles
    {
        private static GUIStyle _hubNode;
        private static GUIStyle _multipleChoice;
        private static GUIStyle _statementNode;
        private static GUIStyle _genericNode;
        private static GUIStyle _choice;
        private static GUIStyle _task;

        /// <summary>Outer wrapper for hub-style nodes.</summary>
        public static GUIStyle HubNode => _hubNode ??= Build(new Color(0.10f, 0.28f, 0.14f, 0.94f), new Color(0.55f, 1f, 0.55f));

        /// <summary>Outer wrapper for multiple-choice nodes.</summary>
        public static GUIStyle MultipleChoice => _multipleChoice ??= Build(new Color(0.12f, 0.20f, 0.32f, 0.94f), new Color(0.55f, 0.85f, 1f));

        /// <summary>Outer wrapper for statement nodes.</summary>
        public static GUIStyle StatementNode => _statementNode ??= Build(new Color(0.18f, 0.22f, 0.16f, 0.94f), new Color(0.75f, 1f, 0.55f));

        /// <summary>Fallback wrapper for any other node type.</summary>
        public static GUIStyle GenericNode => _genericNode ??= Build(new Color(0.16f, 0.16f, 0.20f, 0.94f), new Color(0.95f, 0.95f, 0.95f));

        /// <summary>Inner box used for an individual choice inside a multiple-choice node.</summary>
        public static GUIStyle Choice => _choice ??= Build(new Color(0.22f, 0.26f, 0.34f, 0.92f), Color.white);

        /// <summary>Outer wrapper for a condition / task block.</summary>
        public static GUIStyle Task => _task ??= Build(new Color(0.30f, 0.18f, 0.34f, 0.94f), new Color(1f, 0.85f, 1f));

        private static GUIStyle Build(Color background, Color headerColor)
        {
            Texture2D tex = new(1, 1);
            tex.SetPixel(0, 0, background);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;

            GUIStyle style = new()
            {
                alignment = TextAnchor.UpperCenter,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(2, 2, 2, 2),
            };
            style.normal.background = tex;
            style.normal.textColor = headerColor;
            style.hover.background = tex;
            style.hover.textColor = headerColor;
            return style;
        }
    }
}
