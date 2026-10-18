using HarmonyLib;
using Il2CppDrova.GUI;
using Il2CppDrova.GUI.Arena;
using Il2CppDrova.JournalSystem;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Interception points for GUI content windows (letters/notes and the rune-drawing window),
    /// so mods can inspect or swap what the player is shown without re-discovering the correct
    /// overloads and chokepoints.
    /// </summary>
    public static class WindowContentAccess
    {
        /// <summary>
        /// A letter/note window finished building its content. Fires for world letters: the hook
        /// is the two-argument <c>GUI_Window_Letter.ShowLetterContent(GameObject, Action)</c>
        /// overload, which the one-argument overload delegates to. The journal's Letters tab
        /// does NOT pass through this window - it builds the same content prefab in its own pane
        /// (<see cref="OnJournalLetterShown"/>) - so content rewrites must subscribe to both to
        /// stay consistent across a re-read. Mutate the window's children (e.g. swap
        /// <c>Image.sprite</c>s) in the handler.
        /// </summary>
        public static event Action<GUI_Window_Letter>? OnLetterShown
        {
            add
            {
                EnsureLetterHooked();
                _onLetterShown += value;
            }
            remove
            {
                _onLetterShown -= value;
            }
        }

        /// <summary>
        /// The journal's Letters tab finished building a letter's content. The hook is a postfix
        /// on <c>GUI_Journal_EntryController.Init(Journal_RuntimeTopic)</c>, which instantiates
        /// the letter content prefab into the journal pane; it fires only when the shown topic
        /// is a letter. Mutate the controller's children (e.g. swap <c>Image.sprite</c>s) in the
        /// handler, exactly like an <see cref="OnLetterShown"/> handler does for the window.
        /// </summary>
        public static event Action<GUI_Journal_EntryController>? OnJournalLetterShown
        {
            add
            {
                EnsureJournalHooked();
                _onJournalLetterShown += value;
            }
            remove
            {
                _onJournalLetterShown -= value;
            }
        }

        private static Action<GUI_Window_Letter>? _onLetterShown;
        private static Action<GUI_Journal_EntryController>? _onJournalLetterShown;
        private static bool _letterHooked;
        private static bool _journalHooked;
        private static bool _drawingHooked;

        private static readonly List<Func<Texture2D, Texture2D?>> DrawingTransformers = [];

        /// <summary>
        /// Register a transformer for the rune-drawing window's required pattern. Runs inside a
        /// prefix on <c>GUI_Window_Drawing.SetTargetTexture</c>, before the window compares
        /// anything - the drawn result is checked per-pixel against the (possibly replaced)
        /// texture (alpha, plus RGB for multi-color windows), so replacement textures must be
        /// pixel-exact 8x8 grids. Return null (or the input) to leave the pattern alone; the
        /// first transformer that returns a different texture wins.
        /// </summary>
        /// <param name="transformer">Maps the vanilla target texture to a replacement, or null.</param>
        public static void RegisterDrawingTargetTransformer(Func<Texture2D, Texture2D?> transformer)
        {
            EnsureDrawingHooked();
            DrawingTransformers.Add(transformer);
        }

        /// <summary>
        /// Remove a transformer registered with <see cref="RegisterDrawingTargetTransformer"/>.
        /// </summary>
        /// <param name="transformer">The transformer to remove.</param>
        public static void RemoveDrawingTargetTransformer(Func<Texture2D, Texture2D?> transformer)
        {
            DrawingTransformers.Remove(transformer);
        }

        /// <summary>
        /// Lazy: the letter hook is only applied once someone subscribes, and the overload must
        /// be resolved explicitly - <c>ShowLetterContent</c> is overloaded, so a name-only
        /// <c>AccessTools.Method</c> lookup is ambiguous.
        /// </summary>
        private static void EnsureLetterHooked()
        {
            if (_letterHooked)
            {
                return;
            }
            _letterHooked = true;
            System.Reflection.MethodBase? showLetter = AccessTools.Method(typeof(GUI_Window_Letter),
                nameof(GUI_Window_Letter.ShowLetterContent),
                [typeof(GameObject), typeof(Il2CppSystem.Action)]);
            Hooking.TryPostfix(Core.SharedHarmony, showLetter, typeof(WindowContentAccess),
                nameof(ShowLetterContentPostfix), "GUI_Window_Letter.ShowLetterContent(prefab, callback)");
        }

        private static void EnsureJournalHooked()
        {
            if (_journalHooked)
            {
                return;
            }
            _journalHooked = true;
            Hooking.TryPostfix(Core.SharedHarmony, typeof(GUI_Journal_EntryController),
                nameof(GUI_Journal_EntryController.Init), typeof(WindowContentAccess),
                nameof(JournalInitPostfix));
        }

        private static void EnsureDrawingHooked()
        {
            if (_drawingHooked)
            {
                return;
            }
            _drawingHooked = true;
            Hooking.TryPrefix(Core.SharedHarmony, typeof(GUI_Window_Drawing), nameof(GUI_Window_Drawing.SetTargetTexture),
                typeof(WindowContentAccess), nameof(SetTargetTexturePrefix));
        }

        private static void ShowLetterContentPostfix(GUI_Window_Letter __instance)
        {
            try
            {
                if (__instance != null)
                {
                    _onLetterShown?.Invoke(__instance);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[WindowContentAccess] letter handler failed: " + e);
            }
        }

        private static void JournalInitPostfix(GUI_Journal_EntryController __instance, Journal_RuntimeTopic topic)
        {
            try
            {
                // Init also builds quest and NPC topics; only letter topics carry a content
                // prefab whose children handlers may want to rewrite.
                if (__instance != null && topic != null
                    && topic.Topic?.TryCast<JournalLetterTopic>() != null)
                {
                    _onJournalLetterShown?.Invoke(__instance);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[WindowContentAccess] journal letter handler failed: " + e);
            }
        }

        private static void SetTargetTexturePrefix(ref Texture2D targetTexture)
        {
            try
            {
                if (targetTexture == null)
                {
                    return;
                }
                foreach (Func<Texture2D, Texture2D?> transformer in DrawingTransformers)
                {
                    Texture2D? replacement = transformer(targetTexture);
                    if (replacement != null && replacement != targetTexture)
                    {
                        targetTexture = replacement;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("[WindowContentAccess] drawing transformer failed: " + e);
            }
        }
    }
}
