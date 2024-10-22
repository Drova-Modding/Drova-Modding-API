using Il2Cpp;
using Il2CppCustomFramework.Localization;
using MelonLoader;
using System.Text;
using static Il2CppCustomFramework.Localization.LocalizationDB;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// This class is used to access the localization system.
    /// </summary>
    public class LocalizationAccess
    {
        private LocalizationAccess() { }

        /// <summary>
        /// Changes the language of the game.
        /// </summary>
        /// <param name="language"></param>
        public static void SetLanguage(ELanguage language)
        {
            LocalizationDB.Instance.LoadLanguage(language);
        }


        /// <summary>
        /// Gets a localized string for a mod.
        /// <example>
        /// <code>
        /// GameObject.GetComponent{LocalizedTextMeshPro}()._localizedString = LocalizationAccess.GetLocalizedString("modName", "key");
        /// GameObject.GetComponent{LocalizedTextMeshPro}().UpdateLocalizedText();
        /// </code>
        /// </example>
        /// 
        /// </summary>
        /// 
        /// <param name="key">The key of the localization.</param>
        /// <param name="modName">The name of the mod.</param>
        public static LocalizedString GetLocalizedString(string modName, string key)
        {
            return new LocalizedString(modName, key);
        }


        /// <summary>
        /// Creates localization entries for a mod. The entries are grouped by language and written to the respective language file.
        /// </summary>
        /// <param name="entries"> The entries to create.</param>
        /// <param name="modName">The name of the mod (must be unique to other mod names).</param> 
        public static void CreateLocalizationEntries(List<LocalizationEntry> entries, string modName)
        {
            var groups = entries.GroupBy(x => x.Language);
            var map = new Dictionary<ELanguage, StringBuilder>();

            foreach (var entry in groups)
            {
                var sb = new StringBuilder();

                foreach (var item in entry)
                {
                    sb.AppendLine($"{item.Key} {{ {item.Value} }}");
                    sb.AppendLine();
                }
                map.Add(entry.Key, sb);
            }

            foreach (var item in map)
            {
                var path = LocalizationDB.Instance.GetLanguageFolderPath(item.Key);
                try
                {
                    File.WriteAllText(Path.Combine(path, modName + $"_{Enum.GetName(typeof(ELanguage), item.Key)}.loc"), item.Value.ToString());
                }
                catch (Exception e)
                {
                    MelonLogger.Error(e.Message);
                }
            }
            LocalizationDB.Instance.ReloadCurrentLanguage();
        }

        /// <summary>
        /// Localization entry for a mod.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Language"></param>
        public record LocalizationEntry(string Key, string Value, ELanguage Language)
        {
        }
    }

}
