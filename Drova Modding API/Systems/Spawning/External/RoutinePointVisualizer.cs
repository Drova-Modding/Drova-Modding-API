using UnityEngine;
using MelonLoader;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Visualizes routine points in the game world during wizard editing.
    /// Renders a solid colored marker with the point index at each world position.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    internal class RoutinePointVisualizer(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private static readonly List<Vector2> EmptyPoints = [];
        private List<Vector2> _points = EmptyPoints;
        private bool _visible;
        private int _lastUpdateFrame = -1;
        private Camera? _cachedMainCamera;

        private const float MarkerSize = 28f;
        private const float MarkerHalf = MarkerSize / 2f;

        private static Texture2D? _markerTexture;
        private static GUIStyle? _markerStyle;
        private static GUIStyle? _labelStyle;
        private static GUIStyle? _shadowLabelStyle;

        // Texture2D is safe to create outside OnGUI; GUIStyles require GUI.skin
        // and must be initialized lazily inside an OnGUI callback.
        internal void Awake()
        {
            _markerTexture = new Texture2D(1, 1);
            _markerTexture.SetPixel(0, 0, new Color(0f, 0.85f, 1f, 0.92f));
            _markerTexture.Apply();
        }

        public void SetPoints(List<Vector2>? points, bool visible)
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

            EnsureStyles();

            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 point = _points[i];
                Vector3 worldPos = new(point.x, point.y, 0f);
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

                if (screenPos.z < 0f)
                    continue;

                // Convert from Unity screen space (bottom-left) to IMGUI space (top-left).
                float guiX = screenPos.x - MarkerHalf;
                float guiY = Screen.height - screenPos.y - MarkerHalf;

                Rect markerRect = new(guiX, guiY, MarkerSize, MarkerSize);
                string label = i.ToString();

                GUI.Box(markerRect, GUIContent.none, _markerStyle!);

                // Shadow pass for readability.
                Rect shadowRect = new(guiX + 1f, guiY + 1f, MarkerSize, MarkerSize);
                GUI.Label(shadowRect, label, _shadowLabelStyle!);

                // Main label.
                GUI.Label(markerRect, label, _labelStyle!);
            }
        }

        private static void EnsureStyles()
        {
            if (_markerStyle == null)
            {
                _markerStyle = new GUIStyle(GUIStyle.none);
                _markerStyle.normal.background = _markerTexture;
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 13
                };
                _labelStyle.normal.textColor = Color.black;
            }

            if (_shadowLabelStyle == null)
            {
                _shadowLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 13
                };
                _shadowLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            }
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


