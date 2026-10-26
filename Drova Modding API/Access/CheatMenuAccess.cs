using System.Diagnostics.CodeAnalysis;
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

        private static readonly List<PendingCheat> PendingCheats = [];
        private static readonly HashSet<string> RegisteredNames = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Cached CommandShell for using cheats without opening the console.
        /// </summary>
        private static CommandShell? _commandShell;

        // What the private shell FireCommand runs already holds, so a cheat is added to it once.
        private static readonly HashSet<string> FiredShellNames = new(StringComparer.OrdinalIgnoreCase);

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
            if (TryGetCheatHandler(out CheatGameHandler? handler))
            {
                handler!.EnableCheatMode(toggle);
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
        /// <remarks>
        /// Never throws. Calling it before the terminal exists, even from <c>OnInitializeMelon</c>, is supported:
        /// the command is queued and registered on the next cheat mode change or scene load.
        /// </remarks>
        public static bool RegisterCheat(string name, Action<Il2CppReferenceArray<CommandArg>> action, int minArgs, int maxArgs, string help = "", string hint = "")
        {
            if (RegisteredNames.Contains(name)) return true;

            PendingCheat cheat = new(name, action, minArgs, maxArgs, help, hint);
            if (TryRegisterCheat(cheat)) return true;

            PendingCheats.RemoveAll(pending => string.Equals(pending.Name, name, StringComparison.OrdinalIgnoreCase));
            PendingCheats.Add(cheat);
            return false;
        }

        internal static void NotifyCheatModeEnabled()
        {
            OnCheatModeEnabled?.Invoke();
        }

        /// <summary>
        /// A cheat queued while cheat mode is already on waits for the terminal's scene, and nothing raises
        /// <see cref="OnCheatModeEnabled"/> then, so queued registrations are retried on every scene load.
        /// </summary>
        internal static void NotifySceneLoaded()
        {
            FlushPendingCheats();
        }

        private static void FlushPendingCheats()
        {
            if (PendingCheats.Count == 0) return;

            for (int i = PendingCheats.Count - 1; i >= 0; i--)
            {
                if (TryRegisterCheat(PendingCheats[i]))
                    PendingCheats.RemoveAt(i);
            }
        }

        /// <summary>
        /// Registers a single cheat if the game is ready for it, swallowing the failures that come
        /// from asking too early. A failure leaves the cheat queued for the next attempt.
        /// </summary>
        private static bool TryRegisterCheat(PendingCheat cheat)
        {
            if (!TryGetCheatHandler(out CheatGameHandler? handler)) return false;

            try
            {
                if (!handler!.IsCheatModeEnabled) return false;

                RegisterCheatInternal(cheat.Name, cheat.Action, cheat.MinArgs, cheat.MaxArgs, cheat.Help, cheat.Hint);
                RegisteredNames.Add(cheat.Name);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// The handler lookup itself throws while the game managers are still coming up, which is
        /// the normal state during the scenes that load before gameplay.
        /// </summary>
        private static bool TryGetCheatHandler([NotNullWhen(true)] out CheatGameHandler? handler)
        {
            try
            {
                CheatGameHandler.TryGet(out CheatGameHandler found);
                handler = found;
                return handler != null;
            }
            catch (Exception)
            {
                handler = null;
                return false;
            }
        }

        private static void RegisterCheatInternal(string name, Action<Il2CppReferenceArray<CommandArg>> action, int minArgs, int maxArgs, string help, string hint)
        {
            if (!Terminal._isInitialized)
                Terminal.Init();
            CommandShell shell = Terminal.Shell;
            shell.AddCommand(name, InfoOf(action, minArgs, maxArgs, help, hint, name));
            if (_commandShell == null)
            {
                _commandShell = new CommandShell();
                _commandShell.RegisterCommands();
            }
            if (FiredShellNames.Add(name)) _commandShell.AddCommand(name, InfoOf(action, minArgs, maxArgs, help, hint, name));
        }

        /// <summary>
        /// Fires a cheat command as if it was user input
        /// </summary>
        public static void FireCommand(string command)
        {
            if (_commandShell == null)
            {
                _commandShell = new CommandShell();
                _commandShell.RegisterCommands();
            }

            // Queued cheats reach the game's terminal only once cheat mode is on, but this private shell runs before that.
            foreach (PendingCheat cheat in PendingCheats)
            {
                if (FiredShellNames.Add(cheat.Name)) _commandShell.AddCommand(cheat.Name, InfoOf(cheat.Action, cheat.MinArgs, cheat.MaxArgs, cheat.Help, cheat.Hint, cheat.Name));
            }

            _commandShell.RunCommand(command);
        }

        private static CommandInfo InfoOf(Action<Il2CppReferenceArray<CommandArg>> action, int minArgs, int maxArgs, string help, string hint, string name)
        {
            return new CommandInfo
            {
                proc = action,
                help = help,
                hint = hint,
                min_arg_count = minArgs,
                max_arg_count = maxArgs,
                CamelCaseName = name
            };
        }
    }
}