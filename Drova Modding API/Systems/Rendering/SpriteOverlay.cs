using Drova_Modding_API.Access;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Rendering
{
    /// <summary>
    /// Draws mod content over existing world art by parenting a child SpriteRenderer onto the original.
    /// The overlay copies the parent's Just2D material so it takes scene lighting and tints, and joins the art's
    /// <see cref="Just2D_SpriteRenderer"/> so the interact highlight flashes it too. Overlays die with their streamed
    /// scene; remove one by hand only through <see cref="Destroy"/>, or the highlight wrapper warns on every later highlight.
    /// </summary>
    public static class SpriteOverlay
    {
        private static readonly List<(GameObject Object, SpriteRenderer Renderer, Just2D_SpriteRenderer? Wrapper)> Registered = [];
        private static bool _pruneHooked;

        /// <summary>
        /// Create an overlay over <paramref name="parent"/> showing <paramref name="sprite"/>.
        /// Sorting is parent's layer at order+1; material, lighting and interact-highlight
        /// behavior follow the parent.
        /// </summary>
        /// <param name="parent">The renderer to draw over.</param>
        /// <param name="sprite">The sprite to display, see <see cref="CreateRuntimeSprite"/>.</param>
        /// <param name="localOffset">Offset from the parent's transform in world units. To cover region (x,y,w,h) of the
        /// parent sprite use <c>((x + w/2 - pivot.x) / ppu, (y + h/2 - pivot.y) / ppu)</c>, plus the trim offset when
        /// the region was measured on the packed image.</param>
        /// <param name="name">GameObject name. A distinctive prefix lets your own sweeps skip overlays you already created.</param>
        /// <returns>The overlay GameObject, or null when creation failed.</returns>
        public static GameObject? Create(SpriteRenderer parent, Sprite sprite, Vector2 localOffset, string name)
        {
            try
            {
                HookPruneOnce();
                GameObject overlayObject = new(name);
                overlayObject.transform.SetParent(parent.transform, false);
                overlayObject.transform.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);

                SpriteRenderer overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
                overlayRenderer.sprite = sprite;
                overlayRenderer.sortingLayerID = parent.sortingLayerID;
                overlayRenderer.sortingOrder = parent.sortingOrder + 1;
                overlayRenderer.sharedMaterial = parent.sharedMaterial;

                Just2D_SpriteRenderer? wrapper = parent.GetComponent<Just2D_SpriteRenderer>();
                if (wrapper == null)
                {
                    wrapper = parent.GetComponentInParent<Just2D_SpriteRenderer>();
                }
                if (wrapper != null)
                {
                    wrapper.AddSpriteRenderer(overlayRenderer);
                }

                Registered.Add((overlayObject, overlayRenderer, wrapper));
                return overlayObject;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[SpriteOverlay] Create failed: " + e);
                return null;
            }
        }

        /// <summary>
        /// Destroy an overlay created by <see cref="Create"/>, deregistering it from the highlight wrapper first.
        /// </summary>
        /// <param name="overlay">The overlay GameObject (null is tolerated).</param>
        public static void Destroy(GameObject? overlay)
        {
            if (overlay == null)
            {
                return;
            }
            for (int i = Registered.Count - 1; i >= 0; i--)
            {
                (GameObject registeredObject, SpriteRenderer renderer, Just2D_SpriteRenderer? wrapper) = Registered[i];
                if (registeredObject != overlay)
                {
                    continue;
                }
                if (wrapper != null && renderer != null)
                {
                    wrapper.RemoveSpriteRenderer(renderer);
                }
                Registered.RemoveAt(i);
            }
            UnityEngine.Object.Destroy(overlay);
        }

        /// <summary>
        /// Drops registrations whose overlay died with its streamed scene, so the list cannot grow over a long session.
        /// </summary>
        private static void HookPruneOnce()
        {
            if (_pruneHooked)
            {
                return;
            }
            _pruneHooked = true;
            SceneStreamAccess.OnSceneUnloaded += _ => PruneDead();
        }

        private static void PruneDead()
        {
            for (int i = Registered.Count - 1; i >= 0; i--)
            {
                (GameObject registeredObject, SpriteRenderer renderer, Just2D_SpriteRenderer? wrapper) = Registered[i];
                if (registeredObject != null)
                {
                    continue;
                }
                if (wrapper != null && renderer != null)
                {
                    wrapper.RemoveSpriteRenderer(renderer);
                }
                Registered.RemoveAt(i);
            }
        }

        /// <summary>
        /// Build a pixel-art sprite from raw pixels the way world art expects: point filtering, clamped wrap, centered pivot,
        /// and <see cref="HideFlags.HideAndDontSave"/> so scene unloads never try to serialize it.
        /// </summary>
        /// <param name="pixels">Row-major, bottom-up pixel rows (SetPixels32 order).</param>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="pixelsPerUnit">Use the parent sprite's ppu so sizes line up.</param>
        /// <param name="name">Asset name. A recognizable prefix lets you skip already swapped assets.</param>
        /// <returns>The sprite, or null when the arguments are invalid.</returns>
        public static Sprite? CreateRuntimeSprite(Color32[] pixels, int width, int height, float pixelsPerUnit, string name)
        {
            if (width <= 0 || height <= 0)
            {
                MelonLogger.Error($"[SpriteOverlay] CreateRuntimeSprite: width/height must be positive (got {width}x{height}).");
                return null;
            }
            if (pixels == null || pixels.Length != width * height)
            {
                MelonLogger.Error($"[SpriteOverlay] CreateRuntimeSprite: expected {width * height} pixels for {width}x{height}, got {pixels?.Length ?? 0}.");
                return null;
            }
            try
            {
                Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
                {
                    name = name,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                texture.SetPixels32(pixels);
                texture.Apply(false, false);

                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                sprite.name = name;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[SpriteOverlay] CreateRuntimeSprite failed: " + e);
                return null;
            }
        }

        /// <summary>
        /// Where the trimmed content sits inside the sprite's runtime rect. Drova's atlases trim sprites and the pivot is
        /// rect-relative, so a position measured on the packed image must add this offset. Zero for unpacked sprites.
        /// </summary>
        /// <param name="sprite">The sprite.</param>
        /// <param name="offset">The content offset in rect pixels.</param>
        /// <returns>True when the offset could be read (tight-packed sprites can refuse).</returns>
        public static bool TryGetPackedContentOffset(Sprite sprite, out Vector2 offset)
        {
            try
            {
                offset = sprite.textureRectOffset;
                return true;
            }
            catch (Exception)
            {
                offset = Vector2.zero;
                return false;
            }
        }
    }
}
