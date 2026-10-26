using HarmonyLib;
using Il2CppDrova.GUI;
using Il2CppDrova.GUI.Arena;
using Il2CppDrova.JournalSystem;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Interception points for GUI content windows (letters and the rune-drawing window), so mods can inspect or
    /// swap what the player is shown.
    /// </summary>
    public static class WindowContentAccess
    {
        /// <summary>
        /// A world letter window finished building its content. Mutate the window's children in the handler.
        /// The journal's Letters tab builds the same content without this window, so content rewrites also
        /// subscribe to <see cref="OnJournalLetterShown"/>.
        /// </summary>
        public static event Action<GUI_Window_Letter>? OnLetterShown
        {
            add
            {
                HookLetterOnce();
                _onLetterShown += value;
            }
            remove
            {
                _onLetterShown -= value;
            }
        }

        /// <summary>
        /// The journal's Letters tab finished building a letter's content. Mutate the controller's children in the
        /// handler, as an <see cref="OnLetterShown"/> handler does for the window.
        /// </summary>
        public static event Action<GUI_Journal_EntryController>? OnJournalLetterShown
        {
            add
            {
                HookJournalOnce();
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
        /// Register a transformer for the rune-drawing window's required pattern. The drawing is checked per pixel
        /// against it, so a replacement must be a pixel-exact 8x8 grid. Return null or the input to leave the
        /// pattern alone. The first transformer that returns a different texture wins.
        /// </summary>
        /// <param name="transformer">Maps the vanilla target texture to a replacement, or null.</param>
        public static void RegisterDrawingTargetTransformer(Func<Texture2D, Texture2D?> transformer)
        {
            HookDrawingOnce();
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
        /// Hooks the two-argument <c>ShowLetterContent</c>, which the one-argument overload delegates to, on first subscribe.
        /// </summary>
        private static void HookLetterOnce()
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

        private static void HookJournalOnce()
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

        private static void HookDrawingOnce()
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
                // Init also builds quest and NPC topics, which carry no letter content.
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
