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
        /// Config key for regional event cooldown in minutes.
        /// </summary>
        public const string RegionalEventCooldownOptionKey = "RegionalEventCooldownMinutes";
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
        /// <summary>
        /// Config key for the master toggle that controls whether dialogue audio is loaded at all.
        /// Defaults to <c>false</c>, so no dialogue audio is loaded until the user opts in.
        /// </summary>
        public const string EnableDialogueAudioOptionKey = "EnableDialogueAudio";

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
                new LocalizationAccess.LocalizationEntry("RegionalEventCooldown", "Regionaler Event Cooldown", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("RegionalEventCooldown", "Regional Event Cooldown", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("RegionalEventCooldown", "Temps de recharge des événements régionaux", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioHeader", "Dialogue Audio", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioHeader", "Dialogue Audio", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioHeader", "Audio de dialogue", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioDisclaimer", "Diese Option hat ohne ein Audio-Mod keine Wirkung und kann ohne dieses die Performance verschlechtern.", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioDisclaimer", "This option has no effect without an audio mod and may reduce performance when enabled without it.", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioDisclaimer", "Cette option n'a aucun effet sans un mod audio et peut réduire les performances si elle est activée sans celui-ci.", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMinDistance", "Minimale Distanz", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMinDistance", "Min Distance", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMinDistance", "Distance minimale", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMaxDistance", "Maximale Distanz", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMaxDistance", "Max Distance", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioMaxDistance", "Distance maximale", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("DialogueAudioVolumeLerpSpeed", "Lautstaerke-Uebergang", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("DialogueAudioVolumeLerpSpeed", "Volume Lerp Speed", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("DialogueAudioVolumeLerpSpeed", "Vitesse de fondu du volume", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudio", "Dialogaudio aktivieren", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudio", "Enable dialogue audio", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudio", "Activer l'audio des dialogues", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudioOn", "An", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudioOn", "On", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudioOn", "Activé", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudioOff", "Aus", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudioOff", "Off", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.en),
                new LocalizationAccess.LocalizationEntry("EnableDialogueAudioOff", "Désactivé", Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.fr),
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
                .CreateSlider(LocalizationAccess.GetLocalizedString("ModdingUI", "RegionalEventCooldown"), RegionalEventCooldownOptionKey, 0, 60, 1)
                .CreateHeader(LocalizationAccess.GetLocalizedString("ModdingUI", "DialogueAudioHeader"))
                .CreateDisclaimer(LocalizationAccess.GetLocalizedString("ModdingUI", "DialogueAudioDisclaimer"))
                 .CreateSwitch(
                    LocalizationAccess.GetLocalizedString("ModdingUI", "EnableDialogueAudio"),
                    LocalizationAccess.GetLocalizedString("ModdingUI", "EnableDialogueAudioOn"),
                    LocalizationAccess.GetLocalizedString("ModdingUI", "EnableDialogueAudioOff"),
                    EnableDialogueAudioOptionKey,
                    defaultValue: false)
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
                    var instance = WorldEvents.WorldEventSystemManager.Instance;
                    instance?.RefreshCooldown();
                }));
                maxSlider.onValueChanged.AddListener(new Action<float>((value) =>
                {
                    if (value <= minSlider.value)
                    {
                        minSliderOptions.OnValueChangedListener(value - 1);
                        minSliderOptions.UpdateOptionValue((int)value - 1);
                    }
                    var instance = WorldEvents.WorldEventSystemManager.Instance;
                    instance?.RefreshCooldown();
                }));

                if (builded.Count >= 11)
                 {
                    Slider minDistanceSlider = builded[9].GetComponentInChildren<Slider>();
                    GUI_ConfigOption_Slider minDistanceOptions = builded[9].GetComponentInChildren<GUI_ConfigOption_Slider>();
                    Slider maxDistanceSlider = builded[10].GetComponentInChildren<Slider>();
                    GUI_ConfigOption_Slider maxDistanceOptions = builded[10].GetComponentInChildren<GUI_ConfigOption_Slider>();
                     if (minDistanceSlider == null || minDistanceOptions == null || maxDistanceSlider == null || maxDistanceOptions == null)
                     {
                         MelonLogger.Warning("Skipping dialogue distance slider link setup because one or more slider components are missing.");
                         return;
                     }

                     minDistanceSlider.onValueChanged.AddListener(new Action<float>((value) =>
                     {
                         if (value >= maxDistanceSlider.value)
                         {
                             maxDistanceOptions.OnValueChangedListener(value + 1f);
                             maxDistanceOptions.UpdateOptionValue((int)value + 1);
                         }
                     }));

                     maxDistanceSlider.onValueChanged.AddListener(new Action<float>((value) =>
                     {
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
