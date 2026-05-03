using Drova_Modding_API.Access;
using Drova_Modding_API.UI;
using Il2CppDrova.GUI.Options;
using MelonLoader;
using UnityEngine;
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
        /// <summary>
        /// Config key for dialogue audio minimum full-volume distance.
        /// </summary>
        public const string DialogueAudioMinDistanceOptionKey = "DialogueAudioMinDistance";
        /// <summary>
        /// Config key for dialogue audio maximum audible distance.
        /// </summary>
        public const string DialogueAudioMaxDistanceOptionKey = "DialogueAudioMaxDistance";
        /// <summary>
        /// Config key for dialogue audio volume interpolation speed.
        /// </summary>
        public const string DialogueAudioVolumeLerpSpeedOptionKey = "DialogueAudioVolumeLerpSpeed";

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
                new LocalizationAccess.LocalizationEntry("DialogueAudioHeader", "Dialogue Audio", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioHeader", "Dialogue Audio", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioHeader", "Audio de dialogue", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMinDistance", "Minimale Distanz", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMinDistance", "Min Distance", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMinDistance", "Distance minimale", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMaxDistance", "Maximale Distanz", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMaxDistance", "Max Distance", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMaxDistance", "Distance maximale", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioVolumeLerpSpeed", "Lautstaerke-Uebergang", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioVolumeLerpSpeed", "Volume Lerp Speed", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioVolumeLerpSpeed", "Vitesse de fondu du volume", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                ], "ModdingUI");
        }

        private static void Instance_OnOptionMenuOpen()
        {
            UI.Builder.OptionUIBuilder builder = OptionMenuAccess.Instance.GetBuilder("ModdingUI");
            if (builder == null) return;
            List<GameObject> builded = builder.CreateTitle(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingUITitle"))
                .CreateDisclaimer(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderDisclaimer2"))
                .CreateDisclaimer(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderDisclaimer"))
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderMinRandomness"), ModdingUIMinOptionKey, 1, 120, 30)
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "ModdingSliderMaxRandomness"), ModdingUIMaxOptionKey, 2, 360, 60)
                .CreateHeader(LocalizationAccess.GetLocalizedString("ModdingUI", "DialogueAudioHeader"))
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "DialogueAudioMinDistance"), DialogueAudioMinDistanceOptionKey, 50, 800, 100)
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "DialogueAudioMaxDistance"), DialogueAudioMaxDistanceOptionKey, 200, 1000, 425)
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "DialogueAudioVolumeLerpSpeed"), DialogueAudioVolumeLerpSpeedOptionKey, 1, 20, 5)
                .Build();
            if (builded.Count >= 5)
            {
                // Layout currently is: 0 title, 1 disclaimer, 2 disclaimer, 3 min slider, 4 max slider, ...
                Slider minSlider = builded[3].GetComponentInChildren<Slider>();
                GUI_ConfigOption_Slider minSliderOptions = builded[3].GetComponentInChildren<GUI_ConfigOption_Slider>();
                Slider maxSlider = builded[4].GetComponentInChildren<Slider>();
                GUI_ConfigOption_Slider maxSliderOptions = builded[4].GetComponentInChildren<GUI_ConfigOption_Slider>();
                if (minSlider == null || minSliderOptions == null || maxSlider == null || maxSliderOptions == null)
                {
                    MelonLogger.Warning("Skipping min/max slider link setup because one or more slider components are missing.");
                    return;
                }
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

                if (builded.Count >= 8)
                {
                    Slider minDistanceSlider = builded[6].GetComponentInChildren<Slider>();
                    GUI_ConfigOption_Slider minDistanceOptions = builded[6].GetComponentInChildren<GUI_ConfigOption_Slider>();
                    Slider maxDistanceSlider = builded[7].GetComponentInChildren<Slider>();
                    GUI_ConfigOption_Slider maxDistanceOptions = builded[7].GetComponentInChildren<GUI_ConfigOption_Slider>();
                    if (minDistanceSlider == null || minDistanceOptions == null || maxDistanceSlider == null || maxDistanceOptions == null)
                    {
                        MelonLogger.Warning("Skipping dialogue distance slider link setup because one or more slider components are missing.");
                        return;
                    }

                    minDistanceSlider.onValueChanged.AddListener(new Action<float>((value) =>
                    {
                        MelonLogger.Msg($"Min Distance changed to {value}");
                        if (value >= maxDistanceSlider.value)
                        {
                            maxDistanceOptions.OnValueChangedListener(value + 1f);
                            maxDistanceOptions.UpdateOptionValue((int)value + 1);
                        }
                    }));

                    maxDistanceSlider.onValueChanged.AddListener(new Action<float>((value) =>
                    {
                        MelonLogger.Msg($"Max Distance changed to {value}");
                        if (value <= minDistanceSlider.value)
                        {
                            minDistanceOptions.OnValueChangedListener(value - 1f);
                            minDistanceOptions.UpdateOptionValue((int)value - 1);
                        }
                    }));
                }
            }
        }
    }
}
