﻿using Drova_Modding_API.Register;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Il2CppDrova.GUI;

namespace Drova_Modding_API.UI
{
    /**
     * This class is used to create a custom keybinding element for the option menu.
     */
    [RegisterTypeInIl2Cpp]
    public class GUI_Options_Controls_KeyFieldElement_Custom(IntPtr ptr) : MonoBehaviour(ptr)
    {
        Button _keybindingButton;
        TextMeshProUGUI _keybindingText;
        string _actionName;
        GUI_OptionPanel_Controls _controls;
        GameObject conflictMarker;

        /**
         * Initialize the keybinding element.
         */
        public void Init(string actionName)
        {
            _actionName = actionName;
            var keyboard = transform.FindChild("Keyboard");
            _controls = FindObjectOfType<GUI_OptionPanel_Controls>(true);
            _keybindingButton = keyboard.GetComponentInChildren<Button>();
            _keybindingText = keyboard.GetComponentInChildren<TextMeshProUGUI>();
            _keybindingButton.onClick.AddListener(new Action(RegisterListenKey));
            conflictMarker = keyboard.transform.FindChild("ConflictMarker").gameObject;
        }

        private bool KeyAlreadyBound(KeyCode code)
        {
            foreach (var key in ActionKeyRegister.Instance.KeyCodes)
                if (key == code) return true;
            if (_controls)
            {
                foreach (var key in _controls.RemapService._defaultMappingKeyboard.Values)
                    if (Enum.TryParse(key.KeyboardValue, out KeyCode result))
                    {
                        if (result == code) return true;
                    }
            }
            return false;
        }

        /**
         * Register the listener for the keybinding.
         */
        public void RegisterListenKey()
        {
            _keybindingText.text = "...";
            Core.OnMonoUpdate += ListenForKey;
        }

        private void ListenForKey()
        {

            foreach (var key in Keyboard.current.allKeys)
                if (key.wasPressedThisFrame && char.IsLetterOrDigit(key.displayName[0]))
                {
                    // If escape is pressed, cancel the keybinding action.
                    if (key.keyCode == Key.Escape)
                    {
                        _keybindingText.text = ActionKeyRegister.Instance[_actionName].ToString();
                        Core.OnMonoUpdate -= ListenForKey; return;
                    }
                    KeyCode code = Utils.KeyToKeyCode(key.keyCode);
                    if (code == KeyCode.None) return;
                    if (KeyAlreadyBound(code))
                    {
                        Core.OnMonoUpdate -= ListenForKey;
                        _keybindingText.text = code.ToString();
                        ActionKeyRegister.Instance.AddAction(_actionName, code);
                        conflictMarker.GetComponent<Image>().enabled = true;
                        return;
                    }
                    conflictMarker.GetComponent<Image>().enabled = false;
                    ActionKeyRegister.Instance.AddAction(_actionName, code);
                    _keybindingText.text = code.ToString();
                    Core.OnMonoUpdate -= ListenForKey;
                }
        }
    }
}
