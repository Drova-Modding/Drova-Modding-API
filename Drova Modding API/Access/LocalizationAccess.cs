using Il2Cpp;
using Il2CppCustomFramework.Localization;
using Il2CppInterop.Runtime.Injection;
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
        /// Injects a new language enum value into the game.
        /// </summary>
        /// <param name="name">Short name of the language</param>
        /// <param name="value">Int of the Enum, at the moment of writing this it would be something >35</param>
        public static void InjectLanguageEnum(string name, int value)
        {
            var injection = new Dictionary<string, object>
            {
                { name, value }
            };
            EnumInjector.InjectEnumValues<ELanguage>(injection);
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
        /// Creates localization entries for a mod. The entries are grouped by language and copied to the respective language file.
        /// Copys all content from the folder to the localization folder.
        /// </summary>
        internal static void CreateLocalizationEntriesFromFolder()
        {
            var localizationFolder = Path.Combine(Utils.SavePath, "Localization");
            if (!Directory.Exists(localizationFolder))
            {
                MelonLogger.Msg("Localization folder not found. Created one for convient reason");
                try { Directory.CreateDirectory(localizationFolder); }
                catch (Exception e) { MelonLogger.Error(e.Message); }
                return;
            }

            // collect all folders
            var folders = Directory.GetDirectories(localizationFolder);
            foreach (var folder in folders)
            {
                try
                {
                    var language = folder.Split(Path.DirectorySeparatorChar).Last();
                    if (!Enum.TryParse(language, true, out ELanguage _))
                    {
                        // If language is not found, inject it to the enum
                        var count = Enum.GetValues(typeof(ELanguage)).Length;
                        InjectLanguageEnum(language, ++count);
                    }
                    // Here we should be safe and the language enum should contain the result
                    if (Enum.TryParse(language, true, out ELanguage result))
                    {
                        var copyPath = LocalizationDB.Instance.GetLanguageFolderPath(result);
                        // Copy all files and directories recursively
                        CopyFilesRecursively(folder, copyPath);
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Failed to load localiazations for {0}, because {1}", folder, e.Message);
                }
            }
        }

        static void CopyFilesRecursively(string sourcePath, string destinationPath)
        {
            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourcePath))
            {
                var destDir = Path.Combine(destinationPath, Path.GetFileName(dir));
                Directory.CreateDirectory(destDir);
                CopyFilesRecursively(dir, destDir);
            }
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
