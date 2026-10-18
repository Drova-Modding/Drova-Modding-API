using System.Runtime.InteropServices;
using System.Text;

namespace Drova.TestRunner;

/// <summary>
/// Moves a launched game's window and its MelonLoader console onto the secondary monitor, so a test run does not cover
/// the window the user is working in: the games side by side across the top, each console below its game. Both belong
/// to the game process (MelonLoader allocates a classic console), so they are found by owner. Without a second monitor
/// they go side by side on the primary one.
/// </summary>
internal static class ConsolePlacer
{
    private const int ConsoleHeight = 300;

    /// <summary>Waits (in the background) for the process's windows and moves them; slot picks the column.</summary>
    internal static void Place(int processId, int slot)
    {
        if (!OperatingSystem.IsWindows() || !TryGetWorkArea(out Rect area)) return;

        // Two columns, A left and B right; a third instance shares the first column.
        int column = (area.Right - area.Left) / 2;
        int x = area.Left + slot % 2 * column;

        int width = column;
        int height = width * 9 / 16;
        int room = area.Bottom - area.Top - ConsoleHeight;
        if (height > room)
        {
            height = room;
            width = height * 16 / 9;
        }

        Task.Run(async () =>
        {
            // The console comes up first and stays put once moved. The game window comes up later and the game resizes
            // it once more when it applies its own settings, so it is held in place for three minutes.
            bool consoleDone = false;
            for (int attempt = 0; attempt < 360; attempt++)
            {
                if (!consoleDone)
                {
                    IntPtr console = FindWindow(processId, "ConsoleWindowClass");
                    if (console != IntPtr.Zero)
                    {
                        SetWindowPos(console, IntPtr.Zero, x, area.Top + height, column, ConsoleHeight, NoZOrder | NoActivate);
                        consoleDone = true;
                    }
                }

                IntPtr game = FindWindow(processId, "UnityWndClass");
                if (game != IntPtr.Zero && GetWindowRect(game, out Rect now)
                    && (now.Left != x || now.Top != area.Top || now.Right - now.Left != width || now.Bottom - now.Top != height))
                {
                    SetWindowPos(game, IntPtr.Zero, x, area.Top, width, height, NoZOrder | NoActivate);
                }

                await Task.Delay(500);
            }
        });
    }

    private static IntPtr FindWindow(int processId, string className)
    {
        IntPtr found = IntPtr.Zero;
        StringBuilder name = new(64);

        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint owner);
            if (owner != processId) return true;

            name.Clear();
            GetClassName(window, name, name.Capacity);
            if (name.ToString() != className) return true;

            found = window;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    /// <summary>The secondary monitor's work area, else the primary's.</summary>
    private static bool TryGetWorkArea(out Rect area)
    {
        Rect result = default;
        bool found = false;

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            MonitorInfo info = new() { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info)) return true;

            result = info.Work;
            found = true;
            return (info.Flags & PrimaryFlag) != 0;
        }, IntPtr.Zero);

        area = result;
        return found;
    }

    private const uint PrimaryFlag = 1;
    private const uint NoZOrder = 0x0004;
    private const uint NoActivate = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    private delegate bool WindowProc(IntPtr window, IntPtr parameter);

    private delegate bool MonitorProc(IntPtr monitor, IntPtr dc, IntPtr rect, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(WindowProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorProc callback, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
