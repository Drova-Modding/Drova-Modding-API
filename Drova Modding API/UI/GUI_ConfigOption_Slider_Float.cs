using Il2CppDrova.GUI.Options;
using MelonLoader;
using UnityEngine.UI;
using Il2CppInterop.Runtime;
using Il2CppTMPro;


namespace Drova_Modding_API.UI
{
    /// <summary>
    /// Class for a float slider in the option menu.
    /// </summary>
    /// <remarks>
    /// Constructor for the float slider.
    /// </remarks>
    [RegisterTypeInIl2Cpp]
    public class GUI_ConfigOption_Slider_Float(IntPtr ptr) : GUI_AConfigOption<float, Slider>(ptr)
    {
        private TextMeshProUGUI _amountText;
        private bool _showPercentSign;

        /// <summary>
        /// Initialize the float slider.
        /// </summary>
        public void Init()
        {
            _amountText = gameObject.transform.parent.GetComponentInChildren<TextMeshProUGUI>();
            GetComponent<Slider>().onValueChanged.AddListener(new Action<float>(OnValueChangedListener));
            _showPercentSign = false;
        }


        /// <summary>
        /// Set the value of the slider and text.
        /// Doesn't work at the moment
        /// </summary>
        /// <param name="value"></param>
        public override void SetUIValue(float value)
        {
            SetUIValueCustom(value);
        }

        /// <summary>
        /// Set the value of the slider and text.
        /// </summary>
        public void SetUIValueCustom(float value)
        {
            _uiElement.value = value;
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
        protected void OnValueChangedListener(float value)
        {
            if (_showPercentSign)
            {
                _amountText.text = value + "%";
            }
            else
            {
                _amountText.text = value.ToString("0.00");
            }
            _configHandler.GameplayConfig.ConfigFile.SetValue(_key._key, value.ToString("0.00"));
        }
    }
}
