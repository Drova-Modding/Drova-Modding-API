using Il2Cpp;
using Il2CppDrova.ConfigOptions;
using Il2CppDrova.GUI;
using Il2CppDrova.GUI.Options;
using Il2CppSystem.Runtime.Remoting;
using MelonLoader;
using UnityEngine;


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
