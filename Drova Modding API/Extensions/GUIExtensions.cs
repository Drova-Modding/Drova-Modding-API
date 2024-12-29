using UnityEngine;

namespace Drova_Modding_API.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="GUI"/> class.
    /// </summary>
    public static class GUIExtensions
    {
        private static Texture2D _lineTexture;

        /// <summary>
        /// Draws a line between two points in the GUI.
        /// </summary>
        /// <param name="start">The starting point of the line.</param>
        /// <param name="end">The ending point of the line.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="width">The width of the line.</param>
        public static void DrawLine(Vector2 start, Vector2 end, Color color, float width = 1.0f)
        {
            if (_lineTexture == null)
            {
                _lineTexture = new Texture2D(1, 1);
                _lineTexture.SetPixel(0, 0, Color.white);
                _lineTexture.Apply();
            }

            Color previousColor = GUI.color;
            GUI.color = color;

            Vector2 delta = end - start;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            Matrix4x4 originalMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y, length, width), _lineTexture);
            GUI.matrix = originalMatrix;

            GUI.color = previousColor;
        }
    }
}
