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
        private record PendingCheat(
            string Name,
            Action<Il2CppReferenceArray<CommandArg>> Action,
            int MinArgs,
            int MaxArgs,
            string Help,
            string Hint);

        private static readonly List<PendingCheat> _pendingCheats = [];

        /// <summary>
        /// Cached CommandShell for using cheats without opening the console.
        /// </summary>
        private static CommandShell? _commandShell;

        /// <summary>
        /// Raised when cheat mode is enabled. Internal use only – the patch calls this.
        /// </summary>
        internal static event Action? OnCheatModeEnabled;

        static CheatMenuAccess()
        {
            OnCheatModeEnabled += FlushPendingCheats;
        }

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
        /// Register a cheat command. If cheat mode is not yet enabled, the command is queued
        /// and will be registered automatically once cheat mode becomes active.
        /// </summary>
        /// <param name="name">Name of the cheat</param>
        /// <param name="action">Action to execute</param>
        /// <param name="minArgs">Minimum number of arguments</param>
        /// <param name="maxArgs">Maximum number of arguments</param>
        /// <param name="help">Help display</param>
        /// <param name="hint">Hint display</param>
        /// <returns>
        /// <c>true</c> if the command was registered immediately;
        /// <c>false</c> if it was queued for later registration.
        /// </returns>
        public static bool RegisterCheat(string name, Action<Il2CppReferenceArray<CommandArg>> action, int minArgs, int maxArgs, string help = "", string hint = "")
        {
            if (CheatGameHandler.TryGet(out CheatGameHandler handler) && handler.IsCheatModeEnabled)
            {
                RegisterCheatInternal(name, action, minArgs, maxArgs, help, hint);
                return true;
            }

            // Queue for when cheat mode is enabled later
            _pendingCheats.Add(new PendingCheat(name, action, minArgs, maxArgs, help, hint));
            return false;
        }

        /// <summary>
        /// Called by the <see cref="Patches.CheatGameHandlerPatch"/> when cheat mode is turned on.
        /// </summary>
        internal static void NotifyCheatModeEnabled()
        {
            OnCheatModeEnabled?.Invoke();
        }

        private static void FlushPendingCheats()
        {
            if (_pendingCheats.Count == 0) return;

            foreach (var cheat in _pendingCheats)
            {
                RegisterCheatInternal(cheat.Name, cheat.Action, cheat.MinArgs, cheat.MaxArgs, cheat.Help, cheat.Hint);
            }
            _pendingCheats.Clear();
        }

        private static void RegisterCheatInternal(string name, Action<Il2CppReferenceArray<CommandArg>> action, int minArgs, int maxArgs, string help, string hint)
        {
            if (!Terminal._isInitialized)
                Terminal.Init();
            CommandShell shell = Terminal.Shell;
            shell.AddCommand(name,
                new CommandInfo()
                {
                    proc = action,
                    help = help,
                    hint = hint,
                    min_arg_count = minArgs,
                    max_arg_count = maxArgs,
                    CamelCaseName = name
                });
            if (_commandShell == null)
            {
                _commandShell = new CommandShell();
                _commandShell.RegisterCommands();
            }
            _commandShell.AddCommand(name,
                new CommandInfo()
                {
                    proc = action,
                    help = help,
                    hint = hint,
                    min_arg_count = minArgs,
                    max_arg_count = maxArgs,
                    CamelCaseName = name
                });
        }

        /// <summary>
        /// Fires a cheat command as if it was user input
        /// </summary>
        /// <param name="command"></param>
        public static void FireCommand(string command)
        {
            if (_commandShell == null)
            {
                _commandShell = new CommandShell();
                _commandShell.RegisterCommands();
            }
            _commandShell.RunCommand(command);
        }
    }
}