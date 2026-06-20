using Drova_Modding_API.Systems.Dialogues.Store;
using Drova_Modding_API.Systems.GlobalVars;
using MelonLoader;
using System.IO.Compression;

namespace Drova_Modding_API.Systems.Spawning.Export
{
    /// <summary>
    /// Bundles every file that belongs to a single mod into one distributable zip.
    /// The archive is rooted at <c>Mods/Modding_API/...</c> so a user only has to extract it into the
    /// game folder. A mod owns the files named after it across the runtime folders:
    /// <list type="bullet">
    /// <item><c>Localization/&lt;lang&gt;/&lt;modName&gt;_&lt;lang&gt;.loc</c></item>
    /// <item><c>NpcPlacement/&lt;modName&gt;.json</c></item>
    /// <item><c>GlobalVars/&lt;modName&gt;.json</c></item>
    /// <item><c>NpcDialogues/&lt;dialogueId&gt;.json</c> (only the dialogues referenced by this mod's NPCs)</item>
    /// </list>
    /// </summary>
    public static class ModBundleExporter
    {
        private const string LocalizationFolderName = "Localization";
        private const string NpcDialoguesFolderName = "NpcDialogues";
        private const string NpcPlacementFolderName = "NpcPlacement";
        private const string GlobalVarsFolderName = "GlobalVars";
        private const string ExportsFolderName = "Exports";
        private const string ModdingApiArchiveRoot = "Modding_API";

        /// <summary>
        /// Collects all files belonging to <paramref name="modName"/> and writes a zip to
        /// <c>Mods/Modding_API/Exports/&lt;modName&gt;.zip</c>.
        /// </summary>
        /// <param name="modName">The mod identifier, i.e. the file stem used for its loca/npc/gvar files.</param>
        /// <param name="zipPath">The absolute path of the produced zip when the export succeeds.</param>
        /// <param name="message">A human readable summary, or the reason the export failed.</param>
        /// <returns>True when at least one file was bundled and the zip was written.</returns>
        public static bool TryExportMod(string modName, out string zipPath, out string message)
        {
            zipPath = string.Empty;
            message = string.Empty;

            string sanitizedModName = SanitizeFileName(modName);
            if (string.IsNullOrWhiteSpace(sanitizedModName))
            {
                message = "Mod name is empty.";
                return false;
            }

            string stagingRoot = Path.Combine(Path.GetTempPath(), "DrovaModExport", sanitizedModName);
            // The archive must start at Mods/Modding_API, so stage the files under that layout.
            string stagedModdingApi = Path.Combine(stagingRoot, "Mods", ModdingApiArchiveRoot);

            try
            {
                if (Directory.Exists(stagingRoot))
                    Directory.Delete(stagingRoot, true);
                Directory.CreateDirectory(stagedModdingApi);

                int locaCount = StageLocalization(sanitizedModName, stagedModdingApi);
                int npcCount = StageJsonFile(
                    ExternalNpcPlacementSystem.GetNpcPlacementFolderPath(),
                    sanitizedModName,
                    Path.Combine(stagedModdingApi, NpcPlacementFolderName));
                int gvarCount = StageJsonFile(
                    CustomGVarStore.GetGlobalVarsFolderPath(),
                    sanitizedModName,
                    Path.Combine(stagedModdingApi, GlobalVarsFolderName));
                int dialogueCount = StageDialogues(sanitizedModName, stagedModdingApi);

                int total = locaCount + npcCount + gvarCount + dialogueCount;
                if (total == 0)
                {
                    message = $"No files found for mod '{sanitizedModName}'.";
                    return false;
                }

                string exportsFolder = Path.Combine(Utils.SavePath, ExportsFolderName);
                Directory.CreateDirectory(exportsFolder);
                zipPath = Path.Combine(exportsFolder, sanitizedModName + ".zip");

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(stagingRoot, zipPath);

                message = $"Bundled {locaCount} loca, {npcCount} npc, {gvarCount} gvar, {dialogueCount} dialogue file(s).";
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to export mod '{sanitizedModName}': {ex}");
                message = ex.Message;
                return false;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(stagingRoot))
                        Directory.Delete(stagingRoot, true);
                }
                catch (Exception cleanupEx)
                {
                    MelonLogger.Warning($"Failed to clean export staging '{stagingRoot}': {cleanupEx.Message}");
                }
            }
        }

        private static int StageLocalization(string modName, string stagedModdingApi)
        {
            string localizationRoot = Path.Combine(Utils.SavePath, LocalizationFolderName);
            if (!Directory.Exists(localizationRoot))
                return 0;

            int copied = 0;
            foreach (string languageDir in Directory.GetDirectories(localizationRoot))
            {
                string languageName = Path.GetFileName(languageDir);
                string[] files = Directory.GetFiles(languageDir, modName + "_*.loc");
                if (files.Length == 0)
                    continue;

                string destDir = Path.Combine(stagedModdingApi, LocalizationFolderName, languageName);
                Directory.CreateDirectory(destDir);

                for (int i = 0; i < files.Length; i++)
                {
                    File.Copy(files[i], Path.Combine(destDir, Path.GetFileName(files[i])), true);
                    copied++;
                }
            }

            return copied;
        }

        private static int StageJsonFile(string sourceFolder, string modName, string destFolder)
        {
            string sourceFile = Path.Combine(sourceFolder, modName + ".json");
            if (!File.Exists(sourceFile))
                return 0;

            Directory.CreateDirectory(destFolder);
            File.Copy(sourceFile, Path.Combine(destFolder, modName + ".json"), true);
            return 1;
        }

        private static int StageDialogues(string modName, string stagedModdingApi)
        {
            string placementFile = Path.Combine(ExternalNpcPlacementSystem.GetNpcPlacementFolderPath(), modName + ".json");
            if (!File.Exists(placementFile))
                return 0;

            HashSet<string> dialogueIds = new(StringComparer.OrdinalIgnoreCase);
            List<ExternalNpcPlacementSystem.ExternalNpcDefinition> definitions =
                ExternalNpcPlacementSystem.ReadDefinitionsFromFile(placementFile);

            for (int i = 0; i < definitions.Count; i++)
            {
                if (!definitions[i].ModuleConfig.TryGetValue(ExternalDialogueModule.ModuleKey, out string? payload))
                    continue;

                string dialogueId = ExternalDialogueModule.GetDialogueId(payload);
                if (!string.IsNullOrWhiteSpace(dialogueId))
                    dialogueIds.Add(dialogueId);
            }

            if (dialogueIds.Count == 0)
                return 0;

            string destFolder = Path.Combine(stagedModdingApi, NpcDialoguesFolderName);
            int copied = 0;
            foreach (string dialogueId in dialogueIds)
            {
                string dialogueFile = DialogueStore.GetDialogueFilePath(dialogueId);
                if (!File.Exists(dialogueFile))
                    continue;

                Directory.CreateDirectory(destFolder);
                File.Copy(dialogueFile, Path.Combine(destFolder, Path.GetFileName(dialogueFile)), true);
                copied++;
            }

            return copied;
        }

        private static string SanitizeFileName(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return string.Empty;

            string sanitized = rawValue.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                sanitized = sanitized.Replace(invalidChars[i], '_');

            return sanitized;
        }
    }
}
