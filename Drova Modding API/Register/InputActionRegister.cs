using Il2CppInterop.Runtime.InteropTypes.Fields;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Drova_Modding_API.Register
{
    /// <summary>
    /// This class is used to register the input actions for mods.
    /// Gameplay actions are enabled ingame and disabled in the main menu and optionsmenu.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    public class InputActionRegister : MonoBehaviour
    {
        /// <summary>
        /// The name of the gameplay action map
        /// </summary>
        public const string GAMEPLAY_ACTION_MAP_NAME = "gameplay";
        private static InputActionRegister _inputActionRegister;
        internal static bool isMappingCurrently = false;

        /// <summary>
        /// The instance of the InputActionRegister.
        /// </summary>
        public static InputActionRegister Instance
        {
            get
            {
                return _inputActionRegister;
            }
        }

        private readonly Il2CppReferenceField<InputActionAsset> _inputActionAsset;
        private InputActionMap gameplayMap = new(GAMEPLAY_ACTION_MAP_NAME);

        /**
         * Event that is called when the input actions are loaded.
         */
        public static event Action OnInputActionsLoaded;

        /**
         * Constructor for the InputActionRegister.
         */
        public InputActionRegister(IntPtr ptr) : base(ptr) { }

        internal void Start()
        {
            _inputActionAsset.Value = ScriptableObject.CreateInstance<InputActionAsset>();
            _inputActionAsset.Value.AddActionMap(gameplayMap);
            _inputActionRegister = this;
            LoadActions();
            OnInputActionsLoaded?.Invoke();
        }

        internal void SaveActions()
        {
            var json = _inputActionAsset.Value.ToJson();
            var rebindingsJson = _inputActionAsset.Value.Cast<IInputActionCollection2>().SaveBindingOverridesAsJson();
            try
            {
                var path = Utils.SavePath;
                Directory.CreateDirectory(path);
                var file = Path.Combine(path, "actions.json");
                var rebindingsFile = Path.Combine(path, "rebindings.json");
                File.WriteAllText(file, json);
                File.WriteAllText(rebindingsFile, rebindingsJson);
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error saving actions: " + e);
            }
        }

        internal void LoadActions()
        {
            try
            {
                var path = Utils.SavePath;
                var file = Path.Combine(path, "actions.json");
                if (!File.Exists(file))
                {
                    return;
                }
                string json = File.ReadAllText(file);
                _inputActionAsset.Value.LoadFromJson(json);
                gameplayMap = _inputActionAsset.Value.FindActionMap(GAMEPLAY_ACTION_MAP_NAME);

                var rebindingFile = Path.Combine(path, "rebindings.json");
                if (File.Exists(rebindingFile))
                {
                    var rebindingsJson = File.ReadAllText(rebindingFile);
                    _inputActionAsset.Value.Cast<IInputActionCollection2>().LoadBindingOverridesFromJson(rebindingsJson);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error loading actions: " + e);
            }
        }

        /// <summary>
        /// Add a new action map to the input action asset
        /// </summary>
        /// <param name="map"></param>
        public void RegisterInputActionMap(InputActionMap map)
        {
            if (map == null)
            {
                MelonLogger.Error("InputActionMap is null");
                return;
            }
            for (int i = 0; i < _inputActionAsset.Value.actionMaps.Count; i++)
            {
                if (_inputActionAsset.Value.actionMaps[i].name == map.name)
                {
                    MelonLogger.Msg("ActionMap already exists and will not be added");
                    return;
                }
            }
            _inputActionAsset.Value.AddActionMap(map);
        }

        /// <summary>
        /// Add a new action to the gameplay action map
        /// </summary>
        /// <param name="name">The unique name of your action</param>
        /// <returns>A <see cref="InputAction"/> or <c>null</c> when action name is null or the action already exists</returns>
        public InputAction? AddActionToGameplayMap(string name)
        {
            if (name == null)
            {
                MelonLogger.Error("Action name is null");
                return null;
            }
            for (int i = 0; i < gameplayMap.actions.Count; i++)
            {
                if (gameplayMap.actions[i].name == name)
                {
                    MelonLogger.Msg("Action already exists and will not be added");
                    return null;
                }
            }
            return gameplayMap.AddAction(name);
        }

        /// <summary>
        /// Get an action by its name
        /// </summary>
        /// <param name="actionName">The unique name of the action</param>
        /// <returns></returns>
        public InputAction this[string actionName]
        {
            get
            {
                return gameplayMap[actionName];
            }
        }

        internal void EnableGameplayActions()
        {
            gameplayMap.Enable();
        }

        internal void DisableGameplayActions()
        {
            gameplayMap.Disable();
        }
    }
}
