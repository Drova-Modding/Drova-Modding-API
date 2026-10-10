using Il2CppFileHandling;
using Il2CppFileHandling.PathSystems;
using MelonLoader;

namespace Drova_Modding_API.Systems.Testing
{
    /// <summary>
    /// Lets several game instances on one machine keep separate savegames.
    ///
    /// Drova writes every save under one fixed folder (<c>LocalLow\Just2D\Drova\Sprint_24</c>), so two
    /// instances started side by side overwrite each other's slots. Started with <c>--profile B</c>, an
    /// instance uses <c>Sprint_24_B</c> and <c>config_B.j2d</c> instead. Without the argument nothing
    /// changes. Mods that keep per-save files under UserData should name their folder through
    /// <see cref="UserDataFolder"/> so those stay apart too.
    ///
    /// Patched at <c>DefaultPathSystem.GetPath</c>, the one place the game turns a path kind into a
    /// folder; reached through its path-system interface, so it cannot have been inlined.
    /// </summary>
    public static class LaunchProfile
    {
        /// <summary>The profile this instance was started with, or null for the ordinary one.</summary>
        public static string? Name { get; } = ReadArgument("--profile");

        /// <summary>Whether this instance really writes its own savegames. The test runner refuses to go on without it.</summary>
        public static bool Separate { get; private set; }

        /// <summary>
        /// The value following <paramref name="flag"/> on this process's command line, or null. Only
        /// letters, digits, '-' and '_' are taken, because callers build folder names from it.
        /// </summary>
        public static string? ReadArgument(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int index = 0; index < args.Length - 1; index++)
            {
                if (!string.Equals(args[index], flag, StringComparison.OrdinalIgnoreCase)) continue;

                string value = args[index + 1].Trim();
                return value.Length > 0 && value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_') ? value : null;
            }

            return null;
        }

        /// <summary>
        /// A folder name under UserData for this profile: the name itself for the ordinary instance, the
        /// name with the profile appended otherwise.
        /// </summary>
        public static string UserDataFolder(string folderName)
        {
            return Name == null ? folderName : folderName + "_" + Name;
        }

        internal static void Initialize(HarmonyLib.Harmony harmony)
        {
            if (Name == null) return;

            Separate = Hooking.TryPostfix(harmony, typeof(DefaultPathSystem), nameof(DefaultPathSystem.GetPath),
                typeof(LaunchProfile), nameof(GetPathPostfix));

            if (Separate)
            {
                MelonLogger.Msg($"[Profile] this instance is profile '{Name}': its savegames and settings are kept " +
                    "apart from any other instance on this machine.");
            }
            else
            {
                MelonLogger.Warning($"[Profile] profile '{Name}' was asked for but the save path could not be " +
                    "patched. This instance shares savegames with every other one.");
            }
        }

        // A plain string __result: this Harmony refuses the IntPtr form for a string return ("Cannot assign
        // method return type System.String to __result type System.IntPtr"), and the string one works here.
        private static void GetPathPostfix(SpecialPathType pathType, ref string __result)
        {
            if (pathType != SpecialPathType.Savegames && pathType != SpecialPathType.Config_Settings) return;

            try
            {
                string path = __result;
                if (string.IsNullOrEmpty(path)) return;

                __result = pathType == SpecialPathType.Savegames
                    ? path.TrimEnd('/', '\\') + "_" + Name + Path.DirectorySeparatorChar
                    : Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                        Path.GetFileNameWithoutExtension(path) + "_" + Name + Path.GetExtension(path));
            }
            catch
            {
                // The ordinary path stands: sharing saves is the pre-existing behaviour, not a new fault.
            }
        }
    }
}
