using Il2CppCommandTerminal;
using Il2CppDrova;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Access to the Cheat Menu
    /// </summary>
    public static class CheatMenuAccess
    {
        /// <summary>
        /// Toggle the Cheat Menu
        /// </summary>
        /// <param name="toggle">True to enable, false to disable</param>
        public static void ToggleCheatMenu(bool toggle)
        {
            if (CheatGameHandler.TryGet(out CheatGameHandler handler))
            {
                handler.EnableCheatMode(toggle);
            }
        }

        /// <summary>
        /// Register a cheat command
        /// </summary>
        /// <param name="name">Name of the cheat</param>
        /// <param name="action">Action to execute</param>
        /// <param name="minArgs">How many args are ok</param>
        /// <param name="maxArgs">What is the max count for args</param>
        /// <param name="help">Help display</param>
        /// <param name="hint">Hint display</param>
        public static bool RegisterCheat(string name, Action<Il2CppReferenceArray<CommandArg>> action, int minArgs, int maxArgs, string help = "", string hint = "")
        {
            if (CheatGameHandler.TryGet(out CheatGameHandler handler) && handler.IsCheatModeEnabled)
            {
                if (!Terminal._isInitialized)
                    Terminal.Init();
                var shell = Terminal.Shell;
                var autoComplete = Terminal.Autocomplete;
                shell.AddCommand(name, new CommandInfo()
                {
                    proc = action,
                    help = help,
                    hint = hint,
                    min_arg_count = minArgs,
                    max_arg_count = maxArgs,
                    CamelCaseName = name
                });
                autoComplete.Register(name);
                return true;
            }
            return false;
        }
    }
}
