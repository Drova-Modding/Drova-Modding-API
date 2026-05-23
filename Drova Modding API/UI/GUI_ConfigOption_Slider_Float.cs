using Il2CppDrova.ConfigOptions;
using Il2CppDrova;
using Il2CppInterop.Runtime.Attributes;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace Drova_Modding_API.UI
{
    /// <summary>
    /// Class for a float slider in the option menu.
    /// </summary>
    /// <remarks>
    /// Constructor for the float slider.
    /// </remarks>
    [RegisterTypeInIl2Cpp]
    public class GUI_ConfigOption_Slider_Float : MonoBehaviour
    {
        /// <summary>
        /// Slider UI Element
        /// </summary>
        public Slider UiElement;
        /// <summary>
        /// The config game handler
        /// </summary>
        public ConfigGameHandler ConfigHandler;
        /// <summary>
        /// The key for the ConfigGameHandler
        /// </summary>
        public ConfigOptionKey Key;

        private TextMeshProUGUI _amountText;
        private bool _showPercentSign;

        /// <summary>
        /// Do not use
        /// </summary>
        /// <param name="ptr"></param>
        // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
        public GUI_ConfigOption_Slider_Float(IntPtr ptr) : base(ptr)
        {
        }

        /// <summary>
        /// Initialize the float slider.
        /// </summary>
        [HideFromIl2Cpp]
        public void Init()
        {
            _amountText = gameObject.transform.parent.GetComponentInChildren<TextMeshProUGUI>();
            if (_amountText == null)
            {
                MelonLogger.Error("Amount text not found");
            }
            Slider slider = GetComponent<Slider>();
            if (slider == null)
            {
                MelonLogger.Error("Slider not found"); return;
            }
            slider.onValueChanged.AddListener(new Action<float>(OnValueChangedListener));
            _showPercentSign = false;
        }

        /// <summary>
        /// Set the value of the slider and text.
        /// </summary>
        [HideFromIl2Cpp]
        public void SetUIValueCustom(float value)
        {
            if (UiElement == null)
            {
                MelonLogger.Error("UI slider element is not initialized");
                return;
            }
            UiElement.value = value;
            OnValueChangedListener(value);
        }

        private void OnDestroy()
        {
            GetComponent<Slider>()?.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Value Change listener for the slider.
        /// </summary>
        /// <param name="value"></param>
        [HideFromIl2Cpp]
        protected void OnValueChangedListener(float value)
        {
            if (_amountText == null)
            {
                return;
            }

            if (_showPercentSign)
            {
                _amountText.text = value + "%";
            }
            else
            {
                _amountText.text = value.ToString("0.00");
            }
            ConfigHandler?.GameplayConfig?.ConfigFile?.SetValue(Key?._key, value.ToString("0.00"));
        }
    }
}
