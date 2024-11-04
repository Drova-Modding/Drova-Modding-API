using Drova_Modding_API.Access;
using Drova_Modding_API.Register;
using Il2Cpp;
using Il2CppDrova.ConfigOptions;
using Il2CppDrova.GUI;
using Il2CppDrova.GUI.Options;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


namespace Drova_Modding_API.UI.Builder
{
    /// <summary>
    ///  This class is used to build the option UI. <see cref="OptionMenuAccess"/>
    /// </summary>
    public class OptionUIBuilder
    {
        private readonly Transform _parent;
        private readonly List<GameObject> gameObjects = [];
        internal OptionUIBuilder(Transform parent)
        {
            _parent = parent;
        }

        private static GameObject GetTitleObject()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Title).WaitForCompletion();
        }

        private static GameObject GetDisclaimerObject()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Description).WaitForCompletion();
        }

        private static GameObject GetHeaderObject()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_TextCenter).WaitForCompletion();
        }

        private static GameObject GetSwitchObject()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Toggle).WaitForCompletion();
        }

        private static GameObject GetSwitchWithMainTextObject()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Toggle).WaitForCompletion();
        }

        private static GameObject GetSliderObject()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Slider).WaitForCompletion();
        }

        private static GameObject GetDropdownObject()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Dropdown).WaitForCompletion();
        }

        private static GameObject GetKeyBinding()
        {
            return Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Control).WaitForCompletion();
        }

        /**
         * Create a title.
         */
        public OptionUIBuilder CreateTitle(LocalizedString localizedString)
        {
            return CreateTitle(localizedString, _parent);
        }

        /**
         * Create a title.
         */
        public OptionUIBuilder CreateTitle(LocalizedString localizedString, Transform parent)
        {
            var title = UnityEngine.Object.Instantiate(GetTitleObject(), parent);
            SetLocalizedText(title, localizedString);
            gameObjects.Add(title);
            return this;
        }

        /**
         * Create a disclaimer.
         */
        public OptionUIBuilder CreateDisclaimer(LocalizedString localizedString)
        {
            var disclaimer = UnityEngine.Object.Instantiate(GetDisclaimerObject(), _parent);
            SetLocalizedText(disclaimer, localizedString);
            gameObjects.Add(disclaimer);
            return this;
        }

        /**
         * Create a header.
         */
        public OptionUIBuilder CreateHeader(LocalizedString localizedString)
        {
            var header = UnityEngine.Object.Instantiate(GetHeaderObject(), _parent);
            SetLocalizedText(header, localizedString);
            gameObjects.Add(header);
            return this;
        }

        /**
         * Create a slider.
         * @param title The title of the slider.
         * @param optionKey The key of the option.
         * @param min The minimum value of the slider.
         * @param max The maximum value of the slider.
         * @param defaultValue The default value of the slider.
         */
        public OptionUIBuilder CreateSlider(LocalizedString title, string optionKey, int min = 0, int max = 100, int defaultValue = 0)
        {
            var slider = UnityEngine.Object.Instantiate(GetSliderObject(), _parent);
            var left = slider.transform.FindChild("Left");
            if (!left)
            {
                MelonLogger.Error("Left not found in slider prefab");
                return this;
            }
            SetLocalizedText(left.gameObject, title);
            var rightOptionConfig = slider.transform.FindChild("Right/GUI_Slider_OptionConfig");
            if(!rightOptionConfig)
            {
                MelonLogger.Error("Right option config not found in slider prefab");
                return this;
            }
            var unitySlider = rightOptionConfig.GetComponent<Slider>();
            if (!unitySlider)
            {
                MelonLogger.Error("Slider not found in slider prefab");
                return this;
            }
            unitySlider.minValue = min;
            unitySlider.maxValue = max;
            if (min > defaultValue)
            {
                MelonLogger.Warning("Default value is smaller than min value. Setting default value to min value.");
                defaultValue = min;
            }
            else if (max < defaultValue)
            {
                MelonLogger.Warning("Default value is higher than max value. Setting default value to max value.");
                defaultValue = max;
            }
            unitySlider.value = defaultValue;

            var sliderOption = rightOptionConfig.GetComponent<GUI_ConfigOption_Slider>();
            if (!sliderOption)
            {
                MelonLogger.Error("SliderOption not found in slider prefab");
                return this;
            }
            sliderOption._uiElement.onValueChanged.AddListener(new Action<float>(f =>
            {
                int i = (int)f;
                sliderOption.OnValueChangedListener(f);
                sliderOption.UpdateOptionValue(i);
                sliderOption._configHandler.GameplayConfig.ConfigFile.SetValue(optionKey, i.ToString());
            }));
            sliderOption._configHandler = ProviderAccess.GetConfigGameHandler();
            if (!sliderOption._configHandler)
            {
                MelonLogger.Error("ConfigHandler not found in slider prefab");
                return this;
            }
            sliderOption._key = ScriptableObject.CreateInstance<ConfigOptionKey>();
            sliderOption._key._key = optionKey;
            if (!sliderOption._configHandler.GameplayConfig.ConfigFile.TryGetInt(optionKey, out int value))
            {

                sliderOption._configHandler.GameplayConfig._configFile.SetValue(optionKey, defaultValue.ToString());
                var configOptionInt = new ConfigOption_Int(sliderOption._configHandler.GameplayConfig, optionKey, defaultValue);
                sliderOption._configHandler.GameplayConfig._keyToOptions.TryAdd(optionKey, configOptionInt);
                sliderOption.OnValueChangedListener(defaultValue);
                sliderOption.UpdateOptionValue(defaultValue);

            }
            else
            {
                var configOptionInt = new ConfigOption_Int(sliderOption._configHandler.GameplayConfig, optionKey, value);
                sliderOption._configHandler.GameplayConfig._keyToOptions.TryAdd(optionKey, configOptionInt);
                sliderOption.OnValueChangedListener(value);
                sliderOption.UpdateOptionValue(value);
            }
            gameObjects.Add(slider);
            return this;
        }

        /**
         * Create a float slider.
         * @param title The title of the slider.
         * @param optionKey The key of the option.
         * @param min The minimum value of the slider.
         * @param max The maximum value of the slider.
         * @param defaultValue The default value of the slider.
         * @param wholeNumbers If the slider should only have whole numbers.
         */
        public OptionUIBuilder CreateSlider(LocalizedString title, string optionKey, float min = 0, float max = 100, float defaultValue = 0, bool wholeNumbers = false)
        {
            var slider = UnityEngine.Object.Instantiate(GetSliderObject(), _parent);
            SetLocalizedText(slider.transform.FindChild("Left").gameObject, title);
            var rightOptionConfig = slider.transform.FindChild("Right/GUI_Slider_OptionConfig");
            if (!rightOptionConfig)
            {
                MelonLogger.Error("Right option config not found in slider prefab");
                return this;
            }
            var unitySlider = rightOptionConfig.GetComponent<Slider>();
            if (!unitySlider)
            {
                MelonLogger.Error("Slider not found in slider prefab");
                return this;
            }
            unitySlider.minValue = min;
            unitySlider.maxValue = max;
            if (min > defaultValue)
            {
                MelonLogger.Warning("Default value is smaller than min value. Setting default value to min value.");
                defaultValue = min;
            }
            else if (max < defaultValue)
            {
                MelonLogger.Warning("Default value is higher than max value. Setting default value to max value.");
                defaultValue = max;
            }
            unitySlider.value = defaultValue;
            unitySlider.wholeNumbers = wholeNumbers;

            var sliderOption = rightOptionConfig.gameObject.AddComponent<GUI_ConfigOption_Slider_Float>();
            sliderOption._uiElement = unitySlider;
            sliderOption._configHandler = ProviderAccess.GetConfigGameHandler();
            sliderOption._key = ScriptableObject.CreateInstance<ConfigOptionKey>();
            sliderOption._key._key = optionKey;
            var toDestroy = rightOptionConfig.GetComponent<GUI_ConfigOption_Slider>();
            if (toDestroy)
            {
                UnityEngine.Object.Destroy(toDestroy);
            }
            else
            {
                MelonLogger.Error("Failed to destroy old slider component");
            }


            sliderOption.Init();
            if (!sliderOption._configHandler)
            {
                MelonLogger.Error("ConfigHandler not found in slider prefab");
                return this;
            }

            if (!sliderOption._configHandler.GameplayConfig._configFile.TryGetString(optionKey, out string value))
            {
                sliderOption._configHandler.GameplayConfig._configFile.SetValue(optionKey, defaultValue.ToString());

                var configOptionFloat = new ConfigOption_Float(sliderOption._configHandler.GameplayConfig, optionKey, defaultValue);
                sliderOption._configHandler.GameplayConfig._keyToOptions.TryAdd(optionKey, configOptionFloat);
                sliderOption.SetUIValueCustom(defaultValue);
            }
            else
            {
                var configOptionFloat = new ConfigOption_Float(sliderOption._configHandler.GameplayConfig, optionKey, float.Parse(value));

                sliderOption._configHandler.GameplayConfig._keyToOptions.TryAdd(optionKey, configOptionFloat);
                sliderOption.SetUIValueCustom(float.Parse(value));
            }
            gameObjects.Add(slider);
            return this;
        }


        /**
         * Create a switch.
         * @param title The title of the switch.
         * @param onValue The value when the switch is on.
         * @param offValue The value when the switch is off.
         * @param optionKey The key of the option.
         * @param defaultValue The default value of the switch.
         * @param useGreyText If the text should be grey.
         */
        public OptionUIBuilder CreateSwitch(LocalizedString title, LocalizedString onValue, LocalizedString offValue, string optionKey, bool defaultValue = false, bool useGreyText = true)
        {
            var objectToInstantiate = GetSwitchObject();
            var @switch = UnityEngine.Object.Instantiate(objectToInstantiate, _parent);
            if (useGreyText)
            {
                var row = @switch.GetComponent<GUI_OptionsRow>();
                if (!row)
                {
                    MelonLogger.Error("Row not found in switch prefab");
                    return this;
                }
                row.InitRow(true);
                row._rowType = GUI_OptionsRow.RowType.OptionChild;
                row.SetRowType(GUI_OptionsRow.RowType.OptionChild);
            }
            var leftChild = @switch.transform.FindChild("Left");
            if (!leftChild)
            {
                MelonLogger.Error("Left child not found in switch prefab");
                return this;
            }
            var leftChildLocalized = leftChild.GetComponentInChildren<LocalizedTextMeshPro>();
            if (!leftChildLocalized)
            {
                MelonLogger.Error("LocalizedTextMeshPro not found in switch prefab");
                return this;
            }
            leftChildLocalized._localizedString = title;
            var configOption = @switch.transform.FindChild("Right/GUI_Switch_ConfigOption");
            if (!configOption)
            {
                MelonLogger.Error("ConfigOption not found in switch prefab");
                return this;
            }
            configOption.GetComponent<GUI_SwitchBhvr>()._toggle.isOn = defaultValue;
            var onChild = configOption.FindChild("TextOn");
            var offChild = configOption.FindChild("TextOff");
            if (!onChild || !offChild)
            {
                MelonLogger.Error("TextOn or TextOff not found in switch prefab");
                return this;
            }
            onChild.GetComponent<LocalizedTextMeshPro>()._localizedString = onValue;
            offChild.GetComponent<LocalizedTextMeshPro>()._localizedString = offValue;

            var toggle = configOption.GetComponent<GUI_ConfigOption_Toggle>();
            if (!toggle)
            {
                MelonLogger.Error("Toggle not found in switch prefab");
                return this;
            }
            toggle._uiElement.onValueChanged.AddListener(new Action<bool>(b =>
            {
                toggle.OnValueChangedListener(b);
                toggle.SetUIValue(b);
                toggle.UpdateOptionValue(b);
                toggle._configHandler.GameplayConfig.ConfigFile.SetValue(optionKey, b.ToString());
            }));
            toggle._configHandler = ProviderAccess.GetConfigGameHandler();
            if (!toggle._configHandler)
            {
                MelonLogger.Error("ConfigHandler not found in switch prefab");
                return this;
            }
            toggle._key = ScriptableObject.CreateInstance<ConfigOptionKey>();
            toggle._key._key = optionKey;

            if (!toggle._configHandler.GameplayConfig._configFile.TryGetBool(optionKey, out bool value))
            {
                toggle._configHandler.GameplayConfig._configFile.SetValue(optionKey, defaultValue.ToString());
                var configOptionBool = new ConfigOption_Bool(toggle._configHandler.GameplayConfig, optionKey, defaultValue);
                toggle._configHandler.GameplayConfig._keyToOptions.TryAdd(optionKey, configOptionBool);
                toggle.UpdateOptionValue(defaultValue);
                toggle.UpdateUIValue();
            }
            else
            {
                var configOptionBool = new ConfigOption_Bool(toggle._configHandler.GameplayConfig, optionKey, value);

                toggle._configHandler.GameplayConfig._keyToOptions.TryAdd(optionKey, configOptionBool);
            }
            gameObjects.Add(@switch);
            return this;
        }

        /**
         * Create a keybinding
         * @param titleOfSection The title of the section.
         * @param keybindings The keybindings to create.
         */
        public OptionUIBuilder CreateKeyBindingSection(List<Keybinding> keybindings, LocalizedString titleOfSection = null)
        {
            var keybindingPrefab = GetKeyBinding();
            if (titleOfSection != null)
            {
                CreateTitle(titleOfSection);
            };
            return BuildKeyBinding(keybindings, keybindingPrefab);
        }

        private OptionUIBuilder BuildKeyBinding(List<Keybinding> keybindings, GameObject keybindingPrefab)
        {
            foreach (var keybinding in keybindings)
            {
                var keyBinding = UnityEngine.Object.Instantiate(keybindingPrefab, _parent);
                var toDestroy = keyBinding.GetComponent<GUI_Option_Controls_KeyFieldElement>();
                if (toDestroy)
                {
                    UnityEngine.Object.Destroy(toDestroy);

                }
                var components = keyBinding.GetComponentsInChildren<HorizontalLayoutGroup>();
                foreach (var component in components)
                {
                    component.enabled = true;
                }

                var loadedKeycode = ActionKeyRegister.Instance.GetKeyCode(keybinding.ActionName);
                if (loadedKeycode == KeyCode.None)
                {
                    ActionKeyRegister.Instance[keybinding.ActionName] = keybinding.DefaultActionKey;
                }

                var customKeyFieldElement = keyBinding.AddComponent<GUI_Options_Controls_KeyFieldElement_Custom>();
                if (customKeyFieldElement != null)
                {
                    customKeyFieldElement.Init(keybinding.ActionName);
                }
                else
                {
                    MelonLogger.Error("Failed to create custom keybinding element");
                }

                var leftChild = keyBinding.transform.FindChild("Left");
                if (!leftChild)
                {
                    MelonLogger.Error("Left child not found in keybinding prefab");
                    return this;
                }
                var textMeshPro = leftChild.GetComponentInChildren<TextMeshProUGUI>();
                if (!textMeshPro)
                {
                    MelonLogger.Error("TextMeshPro not found in keybinding prefab");
                    return this;
                }

                var textMeshProObject = textMeshPro.gameObject;
                var localized = textMeshProObject.AddComponent<LocalizedTextMeshPro>();

                localized._text = textMeshPro;
                localized._localizedString = keybinding.Title;
                localized.UpdateLocalizedText();

                // We can't support at the moment controller keybindings. Fix me with MelonLoader 1.6.6
                var controller = keyBinding.transform.FindChild("Controller/ChangeButton");
                if (!controller)
                {
                    MelonLogger.Error("controller not found in keybinding prefab");
                    return this;
                }
                controller.gameObject.SetActive(false);

                var keyboard = keyBinding.transform.FindChild("Keyboard");
                if (!keyboard)
                {
                    MelonLogger.Error("keyboard not found in keybinding prefab");
                    return this;
                }
                var keyboardTitle = keyboard.GetComponentInChildren<TextMeshProUGUI>();
                if (!keyboardTitle)
                {
                    MelonLogger.Error("keyboardTitle not found in keybinding prefab");
                    return this;
                }
                keyboardTitle.text = loadedKeycode != KeyCode.None ? Enum.GetName(loadedKeycode) : Enum.GetName(keybinding.DefaultActionKey);
                gameObjects.Add(keyBinding);
            }
            return this;
        }

        /**
         * Create a keybinding section in the Controls Panel without a title.
         * At the moment it is not working as intended, because the keybinding area recreates itself on the second time, which moves our elements from last to first!
         * @param keybindings The keybindings to create.
         */
        public OptionUIBuilder CreateKeyBindingSection(List<Keybinding> keybindings)
        {

            var keybindingPrefab = GetKeyBinding();
            return BuildKeyBinding(keybindings, keybindingPrefab);
        }

        /// <summary>
        /// Create a dropdown with a Enum List.
        /// </summary>
        /// <typeparam name="E">Your Enum</typeparam>
        /// <param name="title">Title of the dropdown</param>
        /// <param name="optionKey">Key for the option</param>
        /// <param name="dropdownOptions">Value pairs</param>
        /// <param name="defaulValue">Value when option does not exist</param>
        public OptionUIBuilder CreateDropdown<E>(LocalizedString title, string optionKey, Dictionary<E, LocalizedString> dropdownOptions, E defaulValue) where E : Enum
        {
            var dropdown = UnityEngine.Object.Instantiate(GetDropdownObject(), _parent);
            var left = dropdown.transform.FindChild("Left");
            if (!left)
            {
                MelonLogger.Error("Left not found in dropdown prefab");
                return this;
            }
            SetLocalizedText(left.gameObject, title);

            var rightOptionConfig = dropdown.transform.FindChild("Right/GUI_Dropdown_OptionConfig");

            if (!rightOptionConfig)
            {
                MelonLogger.Error("Right option config not found in dropdown prefab");
                return this;
            }
            var toDestroy = rightOptionConfig.GetComponent<GUI_ConfigOption_Dropdown>();
            if (toDestroy)
            {
                UnityEngine.Object.Destroy(toDestroy);
            }
            else
            {
                MelonLogger.Error("Failed to destroy old dropdown component");
            }

            var dropdownHandler = rightOptionConfig.gameObject.AddComponent<DropdownHandler>();

            var configHandler = ProviderAccess.GetConfigGameHandler();

            if (!configHandler)
            {
                MelonLogger.Error("ConfigHandler not found in dropdown prefab");
                return this;
            }

            List<int> keys = [];
            foreach (var key in dropdownOptions.Keys)
            {
                keys.Add(Utils.GetIndexFromEnum(key));
            }
            keys.Sort();
            if (ConfigAccessor.TryGetConfigValue(optionKey, out E value))
            {
                dropdownHandler.Init(keys, [.. dropdownOptions.Values], configHandler, optionKey, Utils.GetIndexFromEnum(value));
            }
            else
            {
                dropdownHandler.Init(keys, [.. dropdownOptions.Values], configHandler, optionKey, Utils.GetIndexFromEnum(defaulValue));
            }
            gameObjects.Add(dropdown);
            return this;
        }

        /**
         * Add a button to the UI.
         * @param title The title of the button Action.
         * @param buttonName The name of the button.
         * @param onClick The action to perform when the button is clicked.
         */
        public OptionUIBuilder CreateButton(LocalizedString title, LocalizedString buttonName, Action onClick)
        {
            var buttonPrefab = Addressables.LoadAssetAsync<GameObject>(AddressableAccess.GUIOptions.GUI_OptionRow_Button_ResetDefault).WaitForCompletion();
            var button = UnityEngine.Object.Instantiate(buttonPrefab, _parent);
            var left = button.transform.FindChild("Left");
            var right = button.transform.FindChild("Right");
            if (!left || !right)
            {
                MelonLogger.Error("Left or right not found in button prefab");
                return this;
            }
            SetLocalizedText(left.gameObject, title);
            SetLocalizedText(right.gameObject, buttonName);
            var buttonJ2D = button.GetComponentInChildren<ButtonJ2D>();
            if (!buttonJ2D)
            {
                MelonLogger.Error("ButtonJ2D not found in button prefab");
                return this;
            }
            buttonJ2D.onClick.AddListener(new Action(onClick));
            gameObjects.Add(button);
            return this;
        }

        /**
         * Builds the UI and returns all your elements sorted after your call order.
         */
        public List<GameObject> Build()
        {
            ProviderAccess.GetConfigGameHandler().GameplayConfig.ConfigFile.SaveChangesToFile(false);
            return gameObjects;
        }

        private static void SetLocalizedText(GameObject gameObject, LocalizedString localizedString)
        {
            if (gameObject == null)
            {
                MelonLogger.Error("GameObject is null in SetLocalizedText");
                return;
            }
            var localizedText = gameObject.GetComponentInChildren<LocalizedTextMeshPro>();
            if (localizedText == null)
            {
                MelonLogger.Error("LocalizedTextMeshPro not found in SetLocalizedText");
                return;
            }
            localizedText._localizedString = localizedString;
            localizedText.UpdateLocalizedText();
        }

        /**
         * A keybinding.
         * @param title The title of the keybinding.
         */
        public record Keybinding(LocalizedString Title, string ActionName, KeyCode DefaultActionKey);
    }
}
