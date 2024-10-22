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
    [RegisterTypeInIl2Cpp]
    public class GUI_ConfigOption_Slider_Float : GUI_AConfigOption<float, Slider>
    {
        private TextMeshProUGUI _amountText;
        private bool _showPercentSign;

        /// <summary>
        /// Constructor for the float slider.
        /// </summary>
        public GUI_ConfigOption_Slider_Float(IntPtr ptr) : base(ptr) {}

        /// <summary>
        /// Initialize the float slider.
        /// </summary>
        public void Init()
        {
            _amountText = gameObject.transform.parent.GetComponentInChildren<TextMeshProUGUI>();
            GetComponent<Slider>().onValueChanged.AddListener(new Action<float>(f =>
            {
                this.OnValueChangedListener(f);
            }));
            _showPercentSign = false;
        }

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
                _amountText.text = value.ToString();
            }
            this.UpdateOptionValue(value);
            _configHandler.GameplayConfig.ConfigFile.SetValue(_key._key, value.ToString());
        }
    }
}
