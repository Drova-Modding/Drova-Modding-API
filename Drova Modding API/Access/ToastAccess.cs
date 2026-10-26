using Drova_Modding_API.Systems.ModdingUI;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Short notices stacked in the top right corner, shared by all mods. Configurable in the Modding UI options.
    /// </summary>
    public static class ToastAccess
    {
        private const int MaxToasts = 4;
        private const float Width = 440f;
        private const float Height = 26f;
        private const float Margin = 16f;

        private static readonly Color Panel = new(0.102f, 0.078f, 0.063f, 0.95f);
        private static readonly Color Frame = new(0.549f, 0.353f, 0.227f, 1f);
        private static readonly Color Bone = new(0.910f, 0.875f, 0.800f, 1f);

        private static readonly List<(string Text, long ShownAt)> Toasts = [];

        private static volatile bool _enabled = true;
        private static long _lifetimeMs = ModdingUI.ToastSecondsDefault * 1000L;

        private static Texture2D? _panelTexture;
        private static Texture2D? _frameTexture;
        private static GUIStyle? _label;

        /// <summary>Shows a line in the top right corner for a few seconds. Safe from any thread.</summary>
        public static void Show(string text)
        {
            if (!_enabled || string.IsNullOrEmpty(text)) return;

            lock (Toasts)
            {
                Toasts.Add((text, Environment.TickCount64));
                if (Toasts.Count > MaxToasts) Toasts.RemoveAt(0);
            }
        }

        /// <summary>Draws the stack. Called from the API melon's <c>OnGUI</c>.</summary>
        internal static void Draw()
        {
            lock (Toasts)
            {
                if (Toasts.Count == 0) return;

                long expired = Environment.TickCount64 - _lifetimeMs;
                for (int i = Toasts.Count - 1; i >= 0; i--)
                {
                    if (!_enabled || Toasts[i].ShownAt <= expired) Toasts.RemoveAt(i);
                }
                if (Toasts.Count == 0) return;

                CreateStyles();
                float y = Margin;
                foreach ((string text, _) in Toasts)
                {
                    Rect box = new(Screen.width - Width - Margin, y, Width, Height);
                    GUI.DrawTexture(box, _frameTexture);
                    GUI.DrawTexture(new Rect(box.x + 1f, box.y + 1f, box.width - 2f, box.height - 2f), _panelTexture);
                    GUI.Label(new Rect(box.x + 10f, box.y, box.width - 20f, box.height), text, _label);
                    y += Height + 4f;
                }
            }
        }

        /// <summary>Takes the player's toast settings from the game config. Missing keys keep the defaults.</summary>
        internal static void ReadSettings()
        {
            _enabled = !ConfigAccessor.TryGetConfigValue(ModdingUI.ToastsEnabledOptionKey, out bool enabled) || enabled;
            int seconds = ConfigAccessor.TryGetConfigValue(ModdingUI.ToastSecondsOptionKey, out int configured)
                ? configured
                : ModdingUI.ToastSecondsDefault;
            Interlocked.Exchange(ref _lifetimeMs, seconds * 1000L);
        }

        // Kept from unloading, so a scene change does not leave the style pointing at destroyed textures.
        private static Texture2D Solid(Color color)
        {
            Texture2D texture = new(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void CreateStyles()
        {
            if (_label != null && _panelTexture != null) return;

            _panelTexture = Solid(Panel);
            _frameTexture = Solid(Frame);
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            _label.normal.textColor = Bone;
        }
    }
}
