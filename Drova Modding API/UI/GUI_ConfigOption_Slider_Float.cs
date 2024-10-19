using Il2CppDrova.GUI.Options;
using MelonLoader;
using UnityEngine.UI;
using Il2CppInterop.Runtime;
using Il2CppTMPro;


namespace Drova_Modding_API.UI
{
    [RegisterTypeInIl2Cpp]
    public class GUI_ConfigOption_Slider_Float : GUI_AConfigOption<float, Slider>
    {
        private TextMeshProUGUI _amountText;
        private bool _showPercentSign;

        public GUI_ConfigOption_Slider_Float(IntPtr ptr) : base(ptr) {}

        public void Init()
        {
            _amountText = gameObject.transform.parent.GetComponentInChildren<TextMeshProUGUI>();
            GetComponent<Slider>().onValueChanged.AddListener(new Action<float>(f =>
            {
                this.OnValueChangedListener(f);
            }));
            _showPercentSign = false;
        }

        private void OnDestroy()
        {
            GetComponent<Slider>()?.onValueChanged.RemoveAllListeners();
        }

        
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

        }
    }
}
