using MelonLoader;
using UnityEngine;

using System.Text.Json.Serialization;
using System.Text.Json;

namespace Drova_Modding_API.Register
{
    /**
     * This class is used to register the action keys for mods.
     */
    public class ActionKeyRegister
    {
        private readonly Dictionary<string, KeyCode> _actionKeys = [];
        private bool _isInitialized = false;
        private ActionKeyRegister() { }
        private static readonly ActionKeyRegister _instance = new();
        /**
         * The instance of the ActionKeyRegister.
         */
        public static ActionKeyRegister Instance { get; } = _instance;

        /**
         * Get the KeyCode of a action.
         */
        public KeyCode this[string actionName]
        {
            get => _actionKeys.ContainsKey(actionName) ? _actionKeys[actionName] : KeyCode.None;
            internal set => _actionKeys[actionName] = value;
        }

        /**
         * Saves the current keyCodes.
         */
        internal void SaveKeyCodes()
        {
            // Save the keycodes to a file.

            var path = Path.Combine(Core.assemblyLocation, "..", "Modding_API");
            try
            {
                Directory.CreateDirectory(path);
                var file = Path.Combine(path, "keybinds.json");
                var json = JsonSerializer.Serialize(_actionKeys, new JsonSerializerOptions
                {
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                });
                // Write json into file
                File.WriteAllText(file, json);
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to save the keybinds: " + e.Message);
            }
        }

        internal void LoadKeyCodes()
        {
            if (_isInitialized) return;
            var path = Path.Combine(Core.assemblyLocation, "..", "Modding_API");
            var file = Path.Combine(path, "keybinds.json");
            if (!File.Exists(file)) return;
            try
            {
                var json = File.ReadAllText(file);
                var saveData = JsonSerializer.Deserialize<Dictionary<string, KeyCode>>(json, new JsonSerializerOptions
                {
                    Converters = {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                });
                foreach (var key in saveData)
                {
                    _actionKeys[key.Key] = key.Value;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to load the keybinds: " + e.Message);
            }
            _isInitialized = true;
        }

        /**
         * Get the KeyCode of a action.
         */
        public KeyCode GetKeyCode(string actionName) => this[actionName];

        /**
         * Add a action to the register.
         */
        internal void AddAction(string actionName, KeyCode code)
        {
            if (_actionKeys.ContainsKey(actionName)) this[actionName] = code;
            else _actionKeys.Add(actionName, code);
            SaveKeyCodes();
        }
    }
}
