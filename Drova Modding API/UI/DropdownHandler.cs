using Il2Cpp;
using Il2CppCustomFramework.Localization;
using Il2CppDrova;
using Il2CppInterop.Runtime.Attributes;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.UI
{
    [RegisterTypeInIl2Cpp]

    public class DropdownHandler : MonoBehaviour
    {
        protected Dictionary<int, LocalizedString> Options;
        protected TMP_Dropdown Dropdown;
        protected ConfigGameHandler ConfigHandler;
        protected string OptionKey;

        [HideFromIl2Cpp]
        internal void Init(List<int> keys, List<LocalizedString> values, ConfigGameHandler configHandler, string optionKey, int value)
        {
            Options = [];
            for (int i = 0; i < keys.Count; i++)
            {
                Options.Add(keys[i], values[i]);
            }
            Dropdown = GetComponent<TMP_Dropdown>();
            ConfigHandler = configHandler;
            OptionKey = optionKey;
            Dropdown.ClearOptions();
            Options.Values.ToList().ForEach(label => Dropdown.options.Add(new TMP_Dropdown.OptionData(label.GetLocalizedString(null))));
            SetOption(value);
            Dropdown.onValueChanged.AddListener(new Action<int>(SetConfig));
            LocalizationDB.Instance.LanguageLoadedEvent.AddEventListener(new Action<LocalizationDB>(OnLanguageChange));
        }

        private void OnDestroy()
        {
            Dropdown.onValueChanged.RemoveAllListeners();
            LocalizationDB.Instance.LanguageLoadedEvent.RemoveEventListener(new Action<LocalizationDB>(OnLanguageChange));
        }

        private void OnLanguageChange(LocalizationDB _db)
        {
            int index = Dropdown.value;
            Dropdown.ClearOptions();
            Options.Values.ToList().ForEach(label => Dropdown.options.Add(new TMP_Dropdown.OptionData(label.GetLocalizedString(null))));
            SetOption(index);
        }

        internal void SetConfig(int index)
        {
            ConfigHandler.GameplayConfig.ConfigFile.SetValue(OptionKey, index.ToString());
        }

        internal void SetOption(int index)
        {
            Dropdown.SetValue(index);
            SetConfig(index);
        }
    }
}
