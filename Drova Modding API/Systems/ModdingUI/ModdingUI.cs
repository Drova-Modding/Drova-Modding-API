using Drova_Modding_API.Access;
using Il2CppDrova.GUI.Options;
using UnityEngine.UI;

namespace Drova_Modding_API.Systems.ModdingUI
{
    /**
     * The Modding UI system for its own Options.
     */
    public static class ModdingUI
    {
        /**
         * The key for the min option.
         */
        public const string ModdingUIMinOptionKey = "ModdingUIMin";
        /**
         * The key for the max option.
         */
        public const string ModdingUIMaxOptionKey = "ModdingUIMax";

        internal static void RegisterModdingUI()
        {
            OptionMenuAccess.Instance.OnOptionMenuOpen += Instance_OnOptionMenuOpen;
        }

        internal static void RegisterLocalization()
        {
            LocalizationAccess.CreateLocalizationEntries([
                new LocalizationAccess.LocalizationEntry("ModdingUITitle", "Modding UI", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("ModdingUITitle", "Modding UI", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("ModdingUITitle", "Modding UI", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("ModdingSliderDisclaimer2", "Diese Einstellungen haben keine Auswirkung ohne einen Event Mod", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("ModdingSliderDisclaimer2", "These settings have no effect without an event mod", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("ModdingSliderDisclaimer2", "Ces paramètres n'ont aucun effet sans un Event Mod", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("ModdingSliderDisclaimer", "Angabe in Minuten", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("ModdingSliderDisclaimer", "Input is in Minutes", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("ModdingSliderDisclaimer", "L'entrée est en minutes", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("ModdingSliderMinRandomness", "Min. Zeit zwischen Events", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("ModdingSliderMinRandomness", "Min Time between Events", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("ModdingSliderMinRandomness", "Temps minimal entre les événements", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("ModdingSliderMaxRandomness", "Max. Zeit zwischen Events", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("ModdingSliderMaxRandomness", "Max. time between Events", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("ModdingSliderMaxRandomness", "Temps maximum entre les événements", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                ], "ModdingUI");
        }

        private static void Instance_OnOptionMenuOpen()
        {
            var builder = OptionMenuAccess.Instance.GetBuilder("ModdingUI");
            if (builder == null) return;
            var builded = builder.CreateTitle(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingUITitle"))
                .CreateDisclaimer(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderDisclaimer2"))
                .CreateDisclaimer(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderDisclaimer"))
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderMinRandomness"), ModdingUIMinOptionKey, 1, 120, 30)
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderMaxRandomness"), ModdingUIMaxOptionKey, 2, 360, 60)
                .Build();
            if (builded != null && builded.Count == 4)
            {
                var minSlider = builded[2].GetComponentInChildren<Slider>();
                var minSliderOptions = builded[2].GetComponentInChildren<GUI_ConfigOption_Slider>();
                var maxSlider = builded[3].GetComponentInChildren<Slider>();
                var maxSliderOptions = builded[3].GetComponentInChildren<GUI_ConfigOption_Slider>();
                minSlider.onValueChanged.AddListener(new Action<float>((value) =>
                {
                    if (value >= maxSlider.value)
                    {
                        maxSliderOptions.OnValueChangedListener(value + 1);
                        maxSliderOptions.UpdateOptionValue((int)value + 1);
                    }
                }));
                maxSlider.onValueChanged.AddListener(new Action<float>((value) =>
                {
                    if (value <= minSlider.value)
                    {
                        minSliderOptions.OnValueChangedListener(value - 1);
                        minSliderOptions.UpdateOptionValue((int)value - 1);
                    }
                }));
            }
        }
    }
}
