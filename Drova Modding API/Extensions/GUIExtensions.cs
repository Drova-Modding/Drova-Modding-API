using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// <param name="_">The GUI object.</param>
        /// <param name="start">The starting point of the line.</param>
        /// <param name="end">The ending point of the line.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="width">The width of the line.</param>
        public static void DrawLine(Vector2 start, Vector2 end, Color color, float width = 1.0f)
        {
            // Create a 1x1 texture if it doesn't exist
            if (_lineTexture == null)
            {
                _lineTexture = new Texture2D(1, 1);
                _lineTexture.SetPixel(0, 0, Color.white);
                _lineTexture.Apply();
            }
            // Save the previous GUI color
            Color previousColor = GUI.color;
            GUI.color = color;

            // Calculate the direction and length of the line
            Vector2 delta = end - start;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            // Set the GUI matrix to rotate and scale the line
            Matrix4x4 originalMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y, length, width), _lineTexture);
            GUI.matrix = originalMatrix;

            // Restore the previous GUI color
            GUI.color = previousColor;
        }
    }
}
