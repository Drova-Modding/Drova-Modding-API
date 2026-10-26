using System.Globalization;
using UnityEngine;

namespace Drova_Modding_API.Systems.Testing
{
    /// <summary>
    /// Synthetic IMGUI input for scenarios: <c>gui click|rclick|down|up|drag x y</c>, <c>gui scroll x y dy</c>,
    /// <c>gui key KeyCode</c>, <c>gui type text</c>. Positions are GUI coordinates (top left origin). One event is replayed
    /// per frame through the API's own OnGUI drawing, so the windows handle it like real input without the desktop cursor.
    /// </summary>
    internal static class TestGuiInput
    {
        private static readonly Queue<Event> Pending = new();
        private static Vector2 _mouse;

        /// <summary>Events still waiting to be replayed.</summary>
        internal static int Count => Pending.Count;

        internal static string Command(string[] parts)
        {
            string verb = parts.Length > 1 ? parts[1] : string.Empty;
            switch (verb)
            {
                case "click":
                    Mouse(parts, EventType.MouseDown, 0);
                    Mouse(parts, EventType.MouseUp, 0);
                    break;
                case "rclick":
                    Mouse(parts, EventType.MouseDown, 1);
                    Mouse(parts, EventType.MouseUp, 1);
                    break;
                case "down":
                    Mouse(parts, EventType.MouseDown, 0);
                    break;
                case "up":
                    Mouse(parts, EventType.MouseUp, 0);
                    break;
                case "drag":
                    Mouse(parts, EventType.MouseDrag, 0);
                    break;
                case "move":
                    Mouse(parts, EventType.MouseMove, 0);
                    break;
                case "scroll":
                    Mouse(parts, EventType.ScrollWheel, 0);
                    Pending.Last().delta = new Vector2(0f, float.Parse(parts[4], CultureInfo.InvariantCulture));
                    break;
                case "key":
                    KeyCode key = Enum.Parse<KeyCode>(parts[2], true);
                    Pending.Enqueue(new Event { type = EventType.KeyDown, keyCode = key, mousePosition = _mouse });
                    Pending.Enqueue(new Event { type = EventType.KeyUp, keyCode = key, mousePosition = _mouse });
                    break;
                case "type":
                    foreach (char c in string.Join(" ", parts.Skip(2)))
                    {
                        Pending.Enqueue(new Event { type = EventType.KeyDown, character = c, mousePosition = _mouse });
                    }

                    break;
                default:
                    throw new InvalidOperationException("gui click|rclick|down|up|drag|move x y, gui scroll x y dy, gui key KeyCode, gui type text");
            }

            return "queued " + Pending.Count;
        }

        /// <summary>
        /// Replays the next event in place of this frame's layout pass. Drawing the same controls in the same order keeps
        /// their control ids equal to a normal pass, which button clicks and text fields depend on.
        /// </summary>
        internal static bool TryReplay(Action draw)
        {
            if (Pending.Count == 0 || Event.current == null || Event.current.type != EventType.Layout)
            {
                return false;
            }

            Event saved = Event.current;
            Event.current = Pending.Dequeue();
            try
            {
                draw();
            }
            finally
            {
                Event.current = saved;
            }

            return true;
        }

        private static void Mouse(string[] parts, EventType type, int button)
        {
            _mouse = new Vector2(float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture));
            Pending.Enqueue(new Event { type = type, mousePosition = _mouse, button = button });
        }
    }
}
