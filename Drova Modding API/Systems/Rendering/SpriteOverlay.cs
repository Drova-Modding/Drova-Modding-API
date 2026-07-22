using Drova_Modding_API.Access;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Rendering
{
    /// <summary>
    /// Draws mod content over existing world art by parenting a child SpriteRenderer onto the
    /// original. Encapsulates four field-learned traps that make naive overlays look wrong:
    ///
    /// 1. MATERIAL - Unity's default sprite material is unlit and lacks Drova's shader
    ///    properties. The overlay must copy the parent's shared material (the Just2D shader) or
    ///    it renders flat, ignores scene lighting/water, and can never be tinted by the game.
    /// 2. HIGHLIGHT - the interact outline/flash (<c>Interact_Bhvr_FeedbackOutline</c>) works
    ///    through the art's <see cref="Just2D_SpriteRenderer"/> wrapper, which property-blocks
    ///    <c>_OverrideColor</c>/<c>_OutlineColor</c> onto every renderer in its list. The overlay
    ///    registers itself there so it flashes in lockstep with the art it covers.
    /// 3. ATLAS TRIM - packed sprites are trimmed: the runtime rect (which the pivot is relative
    ///    to) is larger than the packed content, offset by <c>textureRectOffset</c>. Positions
    ///    measured in packed-image pixels must add that offset; see
    ///    <see cref="TryGetPackedContentOffset"/>.
    /// 4. LIFETIME - overlays are children of streamed scene objects and die with them. Use
    ///    <see cref="Destroy"/> for manual removal so the highlight wrapper is deregistered
    ///    first, or it warns about an unassigned renderer on every later highlight.
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
        /// <param name="sprite">The sprite to display (see <see cref="CreateRuntimeSprite"/>).</param>
        /// <param name="localOffset">Offset from the parent's transform, in world units (for a
        /// ppu-1 sprite: rect pixels). For "cover region (x,y,w,h) of the parent sprite" compute
        /// <c>((x + w/2 - pivot.x) / ppu, (y + h/2 - pivot.y) / ppu)</c> and add the trim offset
        /// when the region was measured on the packed image.</param>
        /// <param name="name">GameObject name; pick a distinctive prefix so your own sweeps can
        /// recognize (and skip) overlays you already created.</param>
        /// <returns>The overlay GameObject, or null when creation failed.</returns>
        public static GameObject? Create(SpriteRenderer parent, Sprite sprite, Vector2 localOffset, string name)
        {
            try
            {
                EnsurePruneHook();
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
        /// Destroy an overlay created by <see cref="Create"/>, deregistering it from the
        /// highlight wrapper first so it no longer warns on later highlights.
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
        /// Drop registrations whose overlay was destroyed with its streamed scene, so the list
        /// cannot grow without bound over a long session. Wired to scene unload on first use.
        /// </summary>
        private static void EnsurePruneHook()
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
        /// Build a pixel-art sprite from raw pixels, configured the way world art expects:
        /// point filtering (no bilinear smear), clamped wrap, centered pivot, and
        /// <see cref="HideFlags.HideAndDontSave"/> so scene unloads never try to serialize it.
        /// </summary>
        /// <param name="pixels">Row-major, BOTTOM-UP pixel rows (Unity's SetPixels32 order).</param>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="pixelsPerUnit">Use the parent sprite's ppu so sizes line up.</param>
        /// <param name="name">Asset name. Give your mod's assets a recognizable prefix and check
        /// for it before transforming sprites, so a swapped asset is never re-swapped.</param>
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
        /// Where the packed (trimmed) content sits inside the sprite's runtime rect. Drova's
        /// atlases trim sprites (e.g. a 72x72 plate packs 67x67 content at ~(2.08, 3.08)), and
        /// <c>Sprite.pivot</c> is RECT-relative - so any position measured on the packed image
        /// (exported art, offline calibration) must add this offset before being used against
        /// the pivot. Zero for unpacked sprites.
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
