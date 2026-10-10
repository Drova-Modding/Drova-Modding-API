using System.Text.RegularExpressions;

namespace Drova.TestRunner;

/// <summary>Where Drova is installed and where it keeps its saves, found the way Steam lays them out.</summary>
internal static class GamePaths
{
    private const string InstallDir = "Drova - Forsaken Kin";

    /// <summary>The save root: <c>Application.persistentDataPath</c> for company Just2D, product Drova.</summary>
    internal static string SaveRoot => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Just2D", "Drova")
        // ApplicationData is $XDG_CONFIG_HOME or ~/.config, where Unity puts unity3d/<company>/<product> on Linux.
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "unity3d", "Just2D", "Drova");

    /// <summary>The game folder in whichever Steam library holds it, or null.</summary>
    internal static string? FindGameDir()
    {
        foreach (string steam in SteamRoots())
        {
            string folders = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            IEnumerable<string> libraries = File.Exists(folders)
                ? Regex.Matches(File.ReadAllText(folders), "\"path\"\\s+\"(?<p>[^\"]+)\"").Select(match => match.Groups["p"].Value.Replace(@"\\", @"\"))
                : [steam];

            foreach (string library in libraries)
            {
                string dir = Path.Combine(library, "steamapps", "common", InstallDir);
                if (Executable(dir) != null) return dir;
            }
        }

        return null;
    }

    /// <summary>The game's executable in <paramref name="dir"/>: Drova.exe on Windows, the native Linux player elsewhere.</summary>
    internal static string? Executable(string dir)
    {
        if (!Directory.Exists(dir)) return null;
        if (OperatingSystem.IsWindows()) return File.Exists(Path.Combine(dir, "Drova.exe")) ? Path.Combine(dir, "Drova.exe") : null;

        return Directory.EnumerateFiles(dir, "*.x86_64").FirstOrDefault();
    }

    private static IEnumerable<string> SteamRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) is string path) yield return path;
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            yield break;
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(home, ".steam", "steam");
        yield return Path.Combine(home, ".local", "share", "Steam");
    }
}
