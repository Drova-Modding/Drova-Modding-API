using Drova_Modding_API.Register;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

        /**
         * Initialize the keybinding element.
         */
        public void Init(string actionName)
        {
            _actionName = actionName;
            var keyboard = transform.FindChild("Keyboard");
            _keybindingButton = keyboard.GetComponentInChildren<Button>();
            _keybindingText = keyboard.GetComponentInChildren<TextMeshProUGUI>();
            _keybindingButton.onClick.AddListener(new System.Action(RegisterListenKey));
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
                    ActionKeyRegister.Instance.AddAction(_actionName, code);
                    _keybindingText.text = code.ToString();
                    Core.OnMonoUpdate -= ListenForKey;
                }
        }
    }
}
