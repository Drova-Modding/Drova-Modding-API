using UnityEngine;
using MelonLoader;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Visualizes routine points in the game world during wizard editing.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    internal class RoutinePointVisualizer(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private static readonly List<Vector2> EmptyPoints = [];
        private List<Vector2> _points = EmptyPoints;
        private bool _visible;
        private int _lastUpdateFrame = -1;
        private Camera? _cachedMainCamera;

        public void SetPoints(List<Vector2> points, bool visible)
        {
            _points = points ?? EmptyPoints;
            _visible = visible;
            _lastUpdateFrame = Time.frameCount;
        }

        internal void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Draw only while the routine module is actively rendering this frame.
            if (_lastUpdateFrame != Time.frameCount)
            {
                _visible = false;
                _points = EmptyPoints;
                return;
            }

            if (!_visible || _points.Count == 0)
                return;

            Camera? mainCamera = ResolveMainCamera();
            if (mainCamera == null)
                return;

            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 point = _points[i];
                // Convert world position to screen position
                Vector3 worldPos = new(point.x, point.y, 0);
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

                // Check if point is in front of camera
                if (screenPos.z < 0)
                    continue;

                // Draw a circle at the location
                DrawCircleAtScreenPosition(screenPos, 8f, Color.cyan);
            }
        }

        private static void DrawCircleAtScreenPosition(Vector3 screenPos, float radius, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.Box(new Rect(screenPos.x - radius, Screen.height - screenPos.y - radius, radius * 2, radius * 2), "");
            GUI.color = previousColor;
        }

        private Camera? ResolveMainCamera()
        {
            if (_cachedMainCamera != null)
                return _cachedMainCamera;

            _cachedMainCamera = Camera.main;
            return _cachedMainCamera;
        }
    }
}


