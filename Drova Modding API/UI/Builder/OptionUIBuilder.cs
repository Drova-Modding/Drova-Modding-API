using Il2Cpp;
using Il2CppDrova.ConfigOptions;
using Il2CppDrova.GUI;
using Il2CppDrova.GUI.Options;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;


namespace Drova_Modding_API.UI.Builder
{
    public class OptionUIBuilder
    {
        private readonly LinkedList<GameObject> toPut = new();
        private readonly Transform _parent;
        internal OptionUIBuilder(Transform parent)
        {
            _parent = parent;
        }

        private static GameObject GetTitleObject()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Panel_Interface/SlotScrollRect(VIEW)/LayoutGroup_Interface/GUI_OptionRow_Title_Interface");
        }

        private static GameObject GetDisclaimerObject()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Panel_Interface/SlotScrollRect(VIEW)/LayoutGroup_Interface/GUI_OptionRow_Show_DisclaimerText");
        }

        private static GameObject GetHeaderObject()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Panel_Interface/SlotScrollRect(VIEW)/LayoutGroup_Interface/GUI_OptionRow_Parent_BasicHudElements");
        }

        private static GameObject GetSwitchObject()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Panel_Interface/SlotScrollRect(VIEW)/LayoutGroup_Interface/GUI_OptionRow_Show_PlayerHealthbar");
        }

        private static GameObject GetSliderObject()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Panel_Video/SlotScrollRect(VIEW)/LayoutGroup_Video/GUI_OptionRow_Slider_Screenshake");
        }

        public OptionUIBuilder CreateTitle(LocalizedString localizedString)
        {
            var title = UnityEngine.Object.Instantiate(GetTitleObject(), _parent);
            title.SetActive(false);
            SetLocalizedText(title, localizedString);
            toPut.AddLast(title);
            return this;
        }

        public OptionUIBuilder CreateDisclaimer(LocalizedString localizedString)
        {
            var disclaimer = UnityEngine.Object.Instantiate(GetDisclaimerObject(), _parent);
            disclaimer.SetActive(false);
            SetLocalizedText(disclaimer, localizedString);
            toPut.AddLast(disclaimer);
            return this;
        }

        public OptionUIBuilder CreateHeader(LocalizedString localizedString)
        {
            var header = UnityEngine.Object.Instantiate(GetHeaderObject(), _parent);
            header.SetActive(false);
            SetLocalizedText(header, localizedString);
            toPut.AddLast(header);
            return this;
        }

        public OptionUIBuilder CreateSlider(LocalizedString title, string optionKey, int min = 0, int max = 100, int defaultValue = 0)
        {
            var slider = UnityEngine.Object.Instantiate(GetSliderObject(), _parent);
            SetLocalizedText(slider.transform.FindChild("Left").gameObject, title);
            var rightOptionConfig = slider.transform.FindChild("Right/GUI_Slider_OptionConfig");
            var unitySlider = rightOptionConfig.GetComponent<Slider>();
            unitySlider.minValue = min;
            unitySlider.maxValue = max;
            unitySlider.value = defaultValue;

            var sliderOption = rightOptionConfig.GetComponent<GUI_ConfigOption_Slider>();
            sliderOption._key = ScriptableObject.CreateInstance<ConfigOptionKey>();
            if (!sliderOption._configHandler.GameplayConfig._keyToOptions.ContainsKey(optionKey))
            {
                sliderOption._configHandler.GameplayConfig._configFile.SetValue(optionKey, defaultValue.ToString());
                var configOptionInt = new ConfigOption_Int(sliderOption._configHandler.GameplayConfig, optionKey, defaultValue);
                configOptionInt.ValueChangedEvent.AddEventListener(new Action<AConfigOption<int>>(t =>
                {
                    bool value = t.TryGetValue(out int rightValue);
                    MelonLogger.Msg("Value changed " + rightValue.ToString());
                }));
                sliderOption._configHandler.GameplayConfig._keyToOptions.Add(optionKey, configOptionInt);
                sliderOption.UpdateOptionValue(defaultValue);
            }
            sliderOption._key._key = optionKey;
            slider.SetActive(false);
            toPut.AddLast(slider);
            return this;
        }

        public OptionUIBuilder CreateSlider(LocalizedString title, string optionKey, float min = 0, float max = 100, float defaultValue = 0, bool wholeNumbers = false)
        {
            var slider = UnityEngine.Object.Instantiate(GetSliderObject(), _parent);
            SetLocalizedText(slider.transform.FindChild("Left").gameObject, title);
            var rightOptionConfig = slider.transform.FindChild("Right/GUI_Slider_OptionConfig");
            var unitySlider = rightOptionConfig.GetComponent<Slider>();
            unitySlider.minValue = min;
            unitySlider.maxValue = max;
            unitySlider.value = defaultValue;
            unitySlider.wholeNumbers = wholeNumbers;

            var toDestory = rightOptionConfig.GetComponent<GUI_ConfigOption_Slider>();
            var sliderOption = rightOptionConfig.gameObject.AddComponent<GUI_ConfigOption_Slider_Float>();
            sliderOption._uiElement = rightOptionConfig.GetComponent<Slider>();
            sliderOption._configHandler = toDestory._configHandler;
            sliderOption._key = ScriptableObject.CreateInstance<ConfigOptionKey>();


            UnityEngine.Object.Destroy(toDestory);

            sliderOption.Init();

            if (!sliderOption._configHandler.GameplayConfig._keyToOptions.ContainsKey(optionKey))
            {
                sliderOption._configHandler.GameplayConfig._configFile.SetValue(optionKey, defaultValue.ToString());

                var configOptionFloat = new ConfigOption_Float(sliderOption._configHandler.GameplayConfig, optionKey, defaultValue);

                sliderOption._configHandler.GameplayConfig._keyToOptions.Add(optionKey, configOptionFloat);
            }

            sliderOption._key._key = optionKey;

            slider.SetActive(false);
            toPut.AddLast(slider);
            return this;
        }

        public OptionUIBuilder CreateSwitch(LocalizedString title, LocalizedString onValue, LocalizedString offValue, string optionKey, bool defaultValue = false)
        {
            var @switch = UnityEngine.Object.Instantiate(GetSwitchObject(), _parent);
            @switch.SetActive(true);
            @switch.transform.FindChild("Left").GetComponentInChildren<LocalizedTextMeshPro>()._localizedString = title;

            var configOption = @switch.transform.FindChild("Right/GUI_Switch_ConfigOption");
            configOption.GetComponent<GUI_SwitchBhvr>()._toggle.isOn = defaultValue;
            configOption.FindChild("TextOn").GetComponent<LocalizedTextMeshPro>()._localizedString = onValue;
            configOption.FindChild("TextOff").GetComponent<LocalizedTextMeshPro>()._localizedString = offValue;

            var toggle = configOption.GetComponent<GUI_ConfigOption_Toggle>();

            if (!toggle._configHandler.GameplayConfig._keyToOptions.ContainsKey(optionKey))
            {
                toggle._configHandler.GameplayConfig._configFile.SetValue(optionKey, defaultValue.ToString());
                var configOptionBool = new ConfigOption_Bool(toggle._configHandler.GameplayConfig, optionKey, defaultValue);
                //configOptionBool.ValueChangedEvent.AddEventListener(new Action<AConfigOption<bool>>(t =>
                //{
                //    bool value = t.TryGetValue(out bool rightValue);
                //    MelonLogger.Msg("Value changed " + rightValue.ToString());
                //}));
                toggle._configHandler.GameplayConfig._keyToOptions.Add(optionKey, configOptionBool);
                toggle.UpdateOptionValue(defaultValue);
            }
            else
            {
                var assignedValue = toggle._configHandler.GameplayConfig.GetOption<ConfigOption_Bool>(optionKey);
                assignedValue.TryGetValue(out ConfigOption_Bool value);
                if (value != null)
                {
                    toggle.UpdateOptionValue(value._cachedValue);
                }
            }
            toggle._key = ScriptableObject.CreateInstance<ConfigOptionKey>();
            toggle._key._key = optionKey;
            @switch.SetActive(false);
            toPut.AddLast(@switch);
            return this;
        }

        public void Build()
        {
            foreach (var gameObject in toPut)
            {
                gameObject.SetActive(true);
            }
        }

        private static void SetLocalizedText(GameObject gameObject, LocalizedString localizedString)
        {
            var localizedText = gameObject.GetComponentInChildren<LocalizedTextMeshPro>();
            localizedText._localizedString = localizedString;
            localizedText.UpdateLocalizedText();
        }


    }
}
