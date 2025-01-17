using Drova_Modding_API.Register;
using Il2CppDrova.GUI;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace Drova_Modding_API.UI
{
    /**
     * This class is used to create a custom keybinding element for the option menu.
     */
    [RegisterTypeInIl2Cpp]
    public class GUI_Options_Controls_KeyFieldElement_Custom(IntPtr ptr) : MonoBehaviour(ptr)
    {
        Button _keybindingButton;
        Button _controllerButton;
        TextMeshProUGUI _keybindingText;
        TextMeshProUGUI _controllerKeybindingText;
        string _actionName;
        GUI_OptionPanel_Controls _controls;
        GameObject conflictMarker;
        GameObject _controller;
        GameObject _keyboard;
        GameObject _exitButton;

        private const string GamePadGroup = "Gamepad";
        private const string KeyboardGroup = "Keyboard;Mouse";


        /// <summary>
        /// OnDestory method.
        /// </summary>
        protected void OnDestroy()
        {
            InputSystem.remove_onDeviceChange(new Action<InputDevice, InputDeviceChange>(this.OnDeviceChange));
        }

        /**
         * Initialize the keybinding element.
         */
        public void Init(string actionName, GameObject exitButton = null)
        {
            _exitButton = exitButton;
            _actionName = actionName;
            _keyboard = transform.FindChild("Keyboard").gameObject;
            _controller = transform.FindChild("Controller").gameObject;
            _controls = FindObjectOfType<GUI_OptionPanel_Controls>(true);
            if (_keyboard == null)
            {
                MelonLogger.Error("Keyboard not found."); return;
            }
            if (_controls == null)
            {
                MelonLogger.Error("Controls not found."); return;
            }
            if (_controller == null)
            {
                MelonLogger.Error("Controller not found."); return;
            }
            _controllerButton = _controller.GetComponentInChildren<Button>();
            _controllerKeybindingText = _controller.GetComponentInChildren<TextMeshProUGUI>();
            if (_controllerButton == null || _controllerKeybindingText == null)
            {
                MelonLogger.Error("Controller button or text not found."); return;
            }
            _keybindingButton = _keyboard.GetComponentInChildren<Button>();
            _keybindingText = _keyboard.GetComponentInChildren<TextMeshProUGUI>();
            if (_keybindingButton == null || _keybindingText == null)
            {
                MelonLogger.Error("Keybinding button or text not found."); return;
            }

            Transform conflictMarkerChild = _keyboard.transform.FindChild("ConflictMarker");
            if (conflictMarkerChild == null)
            {
                MelonLogger.Error("ConflictMarker not found."); return;
            }
            conflictMarker = conflictMarkerChild.gameObject;

            Init();
            InputSystem.add_onDeviceChange(new Action<InputDevice, InputDeviceChange>(this.OnDeviceChange));
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            bool isGamePad = device.TryCast<Gamepad>() != null;
            if ((change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Enabled) && isGamePad)
            {
                InitGamepad();
            }
            else if ((change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Disabled) && isGamePad)
            {
                RemoveGamepad();
            }
        }

        private void InitGamepad()
        {
            _controllerButton.gameObject.SetActive(true);
            InputAction action = InputActionRegister.Instance[_actionName];
            _controllerButton.onClick.AddListener(new Action(RegisterListenController));
            int bindingIndex = action.GetBindingIndex(InputBinding.MaskByGroup(GamePadGroup));
            if (bindingIndex == -1)
            {
                _controllerButton.interactable = false;
                MelonLogger.Warning($"Binding index not found for action {_actionName} and group {GamePadGroup}. This could be intenional if you don't want this action to be rebindable");
                return;
            }
            _controllerKeybindingText.text = action.GetBindingDisplayString((InputBinding.DisplayStringOptions)0, GamePadGroup);
        }

        private void RemoveGamepad()
        {
            _controllerButton.onClick.RemoveAllListeners();
            _controllerButton.gameObject.SetActive(false);

        }

        private void InitKeyboard()
        {
            _keyboard.SetActive(true);
            InputAction action = InputActionRegister.Instance[_actionName];
            _keybindingButton.onClick.AddListener(new Action(RegisterListen));
            _keybindingText.text = action.GetBindingDisplayString((InputBinding.DisplayStringOptions)0, KeyboardGroup);
        }



        private void Init()
        {

            if (Gamepad.current != null)
            {
                InitGamepad();
            }
            else
            {
                RemoveGamepad();
            }
            InitKeyboard();

        }

        private void RegisterListen()
        {
            _keybindingText.text = "...";
            ListenForRebinding(_keybindingText, KeyboardGroup, "<Gamepad>");
        }

        private void RegisterListenController()
        {
            _controllerKeybindingText.text = "...";
            ListenForRebinding(_controllerKeybindingText, GamePadGroup, "<Keyboard>", "<Pointer>");
        }

        private void ListenForRebinding(TextMeshProUGUI textMesh, string group, string excludeGroup, string secondaryExcludeGroup = null)
        {
            InputAction action = InputActionRegister.Instance[_actionName];
            int bindingIndex = action.GetBindingIndex(InputBinding.MaskByGroup(group));
            if (bindingIndex == -1)
            {
                MelonLogger.Error($"Binding index not found for action {_actionName} and group {group}.");
                return;
            }
            InputActionRegister.isMappingCurrently = true;
            RebindingOperation rebindOperation = action.PerformInteractiveRebinding(bindingIndex).WithControlsExcluding(excludeGroup).WithBindingMask(new Il2CppSystem.Nullable<InputBinding>(InputBinding.MaskByGroup(group))).WithTimeout(30);
            if (secondaryExcludeGroup != null)
            {
                rebindOperation.WithControlsExcluding(secondaryExcludeGroup);
            }
            rebindOperation.OnComplete(new Action<RebindingOperation>(operation =>
            {
                textMesh.text = operation.action.bindings[bindingIndex].ToDisplayString();
                InputActionRegister.isMappingCurrently = false;
                _exitButton.SetActive(true);
                operation.Dispose();
            }));
            rebindOperation.Start();
        }
    }
}
