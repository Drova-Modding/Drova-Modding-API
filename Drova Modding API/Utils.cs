using UnityEngine;
using UnityEngine.InputSystem;

namespace Drova_Modding_API
{
    /**
     * A class with utility functions.
     */
    public static class Utils
    {
        /// <summary>
        /// The folder name of the Modding API.
        /// </summary>
        public const string ModdingAPI_FolderName = "Modding_API";

        internal static string SavePath = Path.Combine(Core.AssemblyLocation, "..", ModdingAPI_FolderName);
        /**
         * Converts an enum to its index.
         */
        public static int GetIndexFromEnum<T>(T enumValue) where T : Enum
        {
            try
            {
                int index = Convert.ToInt32(enumValue);
                return index;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error("Error converting enum to index: " + e);
                return default;
            }
        }


        /**
         * Converts an index to an enum.
         */
        public static T GetEnumFromIndex<T>(int index) where T : Enum
        {
            try
            {
                T enumValue = (T)Enum.ToObject(typeof(T), index);
                return enumValue;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error("Error converting index to enum: " + e);
                return default;
            }
        }


        /**
         * Converts a Key to a KeyCode.
         */
        public static KeyCode KeyToKeyCode(Key key, KeyCode unknownKey = KeyCode.None)
        {
            return key switch
            {
                Key.None => KeyCode.None,
                Key.Space => KeyCode.Space,
                Key.Enter => KeyCode.Return,
                Key.Tab => KeyCode.Tab,
                Key.Backquote => KeyCode.BackQuote,
                Key.Quote => KeyCode.Quote,
                Key.Semicolon => KeyCode.Semicolon,
                Key.Comma => KeyCode.Comma,
                Key.Period => KeyCode.Period,
                Key.Slash => KeyCode.Slash,
                Key.Backslash => KeyCode.Backslash,
                Key.LeftBracket => KeyCode.LeftBracket,
                Key.RightBracket => KeyCode.RightBracket,
                Key.Minus => KeyCode.Minus,
                Key.Equals => KeyCode.Equals,
                Key.A => KeyCode.A,
                Key.B => KeyCode.B,
                Key.C => KeyCode.C,
                Key.D => KeyCode.D,
                Key.E => KeyCode.E,
                Key.F => KeyCode.F,
                Key.G => KeyCode.G,
                Key.H => KeyCode.H,
                Key.I => KeyCode.I,
                Key.J => KeyCode.J,
                Key.K => KeyCode.K,
                Key.L => KeyCode.L,
                Key.M => KeyCode.M,
                Key.N => KeyCode.N,
                Key.O => KeyCode.O,
                Key.P => KeyCode.P,
                Key.Q => KeyCode.Q,
                Key.R => KeyCode.R,
                Key.S => KeyCode.S,
                Key.T => KeyCode.T,
                Key.U => KeyCode.U,
                Key.V => KeyCode.V,
                Key.W => KeyCode.W,
                Key.X => KeyCode.X,
                Key.Y => KeyCode.Y,
                Key.Z => KeyCode.Z,
                Key.Digit1 => KeyCode.Alpha1,
                Key.Digit2 => KeyCode.Alpha2,
                Key.Digit3 => KeyCode.Alpha3,
                Key.Digit4 => KeyCode.Alpha4,
                Key.Digit5 => KeyCode.Alpha5,
                Key.Digit6 => KeyCode.Alpha6,
                Key.Digit7 => KeyCode.Alpha7,
                Key.Digit8 => KeyCode.Alpha8,
                Key.Digit9 => KeyCode.Alpha9,
                Key.Digit0 => KeyCode.Alpha0,
                Key.LeftShift => KeyCode.LeftShift,
                Key.RightShift => KeyCode.RightShift,
                Key.LeftAlt => KeyCode.LeftAlt,
                Key.RightAlt => KeyCode.RightAlt,
                Key.LeftCtrl => KeyCode.LeftControl,
                Key.RightCtrl => KeyCode.RightControl,
                Key.LeftCommand => KeyCode.LeftCommand,
                Key.RightCommand => KeyCode.RightCommand,
                Key.ContextMenu => unknownKey,
                Key.Escape => KeyCode.Escape,
                Key.LeftArrow => KeyCode.LeftArrow,
                Key.RightArrow => KeyCode.RightArrow,
                Key.UpArrow => KeyCode.UpArrow,
                Key.DownArrow => KeyCode.DownArrow,
                Key.Backspace => KeyCode.Backspace,
                Key.PageDown => KeyCode.PageDown,
                Key.PageUp => KeyCode.PageUp,
                Key.Home => KeyCode.Home,
                Key.End => KeyCode.End,
                Key.Insert => KeyCode.Insert,
                Key.Delete => KeyCode.Delete,
                Key.CapsLock => KeyCode.CapsLock,
                Key.NumLock => KeyCode.Numlock,
                Key.PrintScreen => KeyCode.Print,
                Key.ScrollLock => KeyCode.ScrollLock,
                Key.Pause => KeyCode.Pause,
                Key.NumpadEnter => KeyCode.KeypadEnter,
                Key.NumpadDivide => KeyCode.KeypadDivide,
                Key.NumpadMultiply => KeyCode.KeypadMultiply,
                Key.NumpadPlus => KeyCode.KeypadPlus,
                Key.NumpadMinus => KeyCode.KeypadMinus,
                Key.NumpadPeriod => KeyCode.KeypadPeriod,
                Key.NumpadEquals => KeyCode.KeypadEquals,
                Key.Numpad0 => KeyCode.Keypad0,
                Key.Numpad1 => KeyCode.Keypad1,
                Key.Numpad2 => KeyCode.Keypad2,
                Key.Numpad3 => KeyCode.Keypad3,
                Key.Numpad4 => KeyCode.Keypad4,
                Key.Numpad5 => KeyCode.Keypad5,
                Key.Numpad6 => KeyCode.Keypad6,
                Key.Numpad7 => KeyCode.Keypad7,
                Key.Numpad8 => KeyCode.Keypad8,
                Key.Numpad9 => KeyCode.Keypad9,
                Key.F1 => KeyCode.F1,
                Key.F2 => KeyCode.F2,
                Key.F3 => KeyCode.F3,
                Key.F4 => KeyCode.F4,
                Key.F5 => KeyCode.F5,
                Key.F6 => KeyCode.F6,
                Key.F7 => KeyCode.F7,
                Key.F8 => KeyCode.F8,
                Key.F9 => KeyCode.F9,
                Key.F10 => KeyCode.F10,
                Key.F11 => KeyCode.F11,
                Key.F12 => KeyCode.F12,
                Key.OEM1 => unknownKey,
                Key.OEM2 => unknownKey,
                Key.OEM3 => unknownKey,
                Key.OEM4 => unknownKey,
                Key.OEM5 => unknownKey,
                Key.IMESelected => unknownKey,
                _ => unknownKey,
            };
        }
    }
}
