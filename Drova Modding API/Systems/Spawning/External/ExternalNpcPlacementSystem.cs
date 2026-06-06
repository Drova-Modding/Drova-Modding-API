using Drova_Modding_API.Systems.SaveGame;
using Drova_Modding_API.Systems.SaveGame.Store;
using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Loads NPC placements from a folder of JSON files and materializes them as lazy actors.
    /// Each mod can drop its own <c>*.json</c> file into the <c>NpcPlacement</c> folder.
    /// The in-game wizard stores its definitions in <c>wizard_placements.json</c>.
    /// </summary>
    public static class ExternalNpcPlacementSystem
    {
        private const string NpcFolderName = "NpcPlacement";
        private const string WizardFileName = "wizard_placements.json";
        private static readonly Dictionary<string, LazyActor> SpawnedLazyActorsByDefinitionId = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> DefinitionSourceFilesById = new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Gets the absolute path of the NPC placement folder.
        /// Drop any <c>*.json</c> file here to have its NPCs spawned on game start.
        /// </summary>
        public static string GetNpcPlacementFolderPath()
            => Path.Combine(Utils.SavePath, NpcFolderName);

        /// <summary>
        /// Gets the absolute path of the wizard-managed <c>wizard_placements.json</c> file.
        /// </summary>
        public static string GetWizardFilePath()
            => Path.Combine(GetNpcPlacementFolderPath(), WizardFileName);

        /// <summary>
        /// Gets the file name that currently owns the specified definition id.
        /// Returns the wizard file when the definition has not been associated with a custom file yet.
        /// </summary>
        public static string GetDefinitionSourceFileName(string? definitionId)
        {
            if (!string.IsNullOrWhiteSpace(definitionId)
                && DefinitionSourceFilesById.TryGetValue(definitionId, out string filePath)
                && !string.IsNullOrWhiteSpace(filePath))
            {
                return Path.GetFileName(filePath);
            }

            return WizardFileName;
        }

        /// <summary>
        /// Gets the file name without extension that currently owns the specified definition id.
        /// </summary>
        public static string GetDefinitionSourceFileStem(string? definitionId)
            => Path.GetFileNameWithoutExtension(GetDefinitionSourceFileName(definitionId));

        /// <summary>
        /// Scans all <c>*.json</c> files in the NPC placement folder and spawns every enabled
        /// definition that has not already been spawned in the current lazy actor store.
        /// </summary>
        public static void SpawnConfiguredNpcs()
        {
            string folderPath = GetNpcPlacementFolderPath();
            EnsureFolder(folderPath);

            string[] files;
            try
            {
                files = Directory.GetFiles(folderPath, "*.json");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to enumerate NPC placement folder '{folderPath}': {ex.Message}");
                return;
            }

            if (files.Length == 0)
            {
                MelonLogger.Msg($"No NPC placement files found in '{folderPath}'. Drop a *.json file there to add custom NPCs.");
                return;
            }

            int spawned = 0;
            for (int f = 0; f < files.Length; f++)
            {
                ExternalNpcConfigFile? config = ReadConfigInternal(files[f]);
                if (config == null || config.Npcs.Count == 0)
                    continue;

                RegisterDefinitionSourceFile(config, files[f]);

                for (int i = 0; i < config.Npcs.Count; i++)
                {
                    if (SpawnDefinition(config.Npcs[i], skipIfAlreadySpawned: true, requireEnabled: true))
                        spawned++;
                }
            }

            if (spawned > 0)
                MelonLogger.Msg($"Spawned {spawned} NPC(s) from '{folderPath}'.");
        }

        /// <summary>
        /// Spawns a single external NPC definition as a lazy actor.
        /// </summary>
        /// <param name="definition">Definition to spawn.</param>
        /// <param name="skipIfAlreadySpawned">If true, checks the lazy actor store by definition id and skips duplicates.</param>
        /// <param name="requireEnabled">If true, only spawns definitions where <see cref="ExternalNpcDefinition.Enabled"/> is true.</param>
        /// <returns>True if spawned, false if skipped or invalid.</returns>
        public static bool SpawnDefinition(ExternalNpcDefinition? definition, bool skipIfAlreadySpawned = true, bool requireEnabled = true)
        {
            if (definition == null)
                return false;

            if (requireEnabled && !definition.Enabled)
                return false;

            if (string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name))
            {
                MelonLogger.Warning("Skipping external NPC definition: missing 'id' or 'name'.");
                return false;
            }

            IStore<LazyActorSaveData> lazyActorStore = SaveGameSystem.Instance.GetStore<LazyActorSaveData>();

            if (skipIfAlreadySpawned)
            {
                foreach (LazyActorSaveData saved in lazyActorStore.GetAll())
                {
                    if (string.Equals(saved.ExternalDefinitionId, definition.Id, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            ExternalNpcModuleRegistry.EnsureDefaults(definition);
            NpcCreator creator = new(definition.Name, new Vector2(definition.PositionX, definition.PositionY));
            ExternalNpcModuleRegistry.ApplyModules(creator, definition);
            LazyActor lazyActor = creator.CreateLazy(saveToLazyActorStore: true, externalDefinitionId: definition.Id);
            SpawnedLazyActorsByDefinitionId[definition.Id] = lazyActor;
            return true;
        }

        /// <summary>
        /// Checks whether a definition id currently exists in the lazy actor save store.
        /// </summary>
        public static bool IsDefinitionSpawned(string? definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            if (!SpawnedLazyActorsByDefinitionId.TryGetValue(definitionId, out LazyActor lazyActor))
                return false;

            if (lazyActor == null || lazyActor.gameObject == null)
            {
                SpawnedLazyActorsByDefinitionId.Remove(definitionId);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to resolve the runtime <see cref="Actor"/> instance for a spawned definition id.
        /// </summary>
        public static bool TryGetSpawnedActor(string? definitionId, out Actor? actor)
        {
            actor = null;
            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            if (!SpawnedLazyActorsByDefinitionId.TryGetValue(definitionId, out LazyActor lazyActor) || lazyActor == null || lazyActor.gameObject == null)
                return false;

            actor = lazyActor.gameObject.GetComponentInChildren<Actor>();
            return actor != null;
        }

        /// <summary>
        /// Removes all lazy actors and save entries associated with a definition id.
        /// </summary>
        /// <returns>True when at least one spawned instance was removed.</returns>
        public static bool DespawnDefinition(string? definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            if (!SpawnedLazyActorsByDefinitionId.TryGetValue(definitionId, out LazyActor lazyActor) || lazyActor == null || lazyActor.gameObject == null)
                return false;

            UnityEngine.Object.Destroy(lazyActor.gameObject);
            SpawnedLazyActorsByDefinitionId.Remove(definitionId);

            IStore<LazyActorSaveData> lazyActorStore = SaveGameSystem.Instance.GetStore<LazyActorSaveData>();
            LazyActorSaveData[] matchingEntries = lazyActorStore
                .GetAll()
                .Where(saved =>
                    string.Equals(saved.ExternalDefinitionId, definitionId, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(lazyActor._guidstring) && string.Equals(saved.ActorGuid, lazyActor._guidstring, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (matchingEntries.Length == 0)
                return true;

            for (int i = 0; i < matchingEntries.Length; i++)
                lazyActorStore.Remove(matchingEntries[i]);

            return true;
        }

        /// <summary>
        /// Rebuilds the spawn tracking dictionary after a save load.
        /// Call this after lazy actors have been restored from the save store.
        /// </summary>
        internal static void HydrationRebuildSpawnedActorsFromStore()
        {
            SpawnedLazyActorsByDefinitionId.Clear();
            IStore<LazyActorSaveData> lazyActorStore = SaveGameSystem.Instance.GetStore<LazyActorSaveData>();

            LazyActor[] allLazyActors = UnityEngine.Object.FindObjectsOfType<LazyActor>();
            Dictionary<string, LazyActor> lazyByGuid = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allLazyActors.Length; i++)
            {
                LazyActor lazyActor = allLazyActors[i];
                if (lazyActor == null || string.IsNullOrWhiteSpace(lazyActor._guidstring))
                    continue;

                lazyByGuid[lazyActor._guidstring] = lazyActor;
            }

            foreach (LazyActorSaveData saved in lazyActorStore.GetAll())
            {
                if (string.IsNullOrWhiteSpace(saved.ExternalDefinitionId) || string.IsNullOrWhiteSpace(saved.ActorGuid))
                    continue;

                if (lazyByGuid.TryGetValue(saved.ActorGuid, out LazyActor lazyActor))
                    SpawnedLazyActorsByDefinitionId[saved.ExternalDefinitionId] = lazyActor;
            }
        }

        /// <summary>
        /// Searches all <c>*.json</c> files in the placement folder for a definition with the given id.
        /// </summary>
        public static bool TryGetDefinition(string definitionId, out ExternalNpcDefinition? definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            string folderPath = GetNpcPlacementFolderPath();
            EnsureFolder(folderPath);

            string[] files;
            try { files = Directory.GetFiles(folderPath, "*.json"); }
            catch { return false; }

            for (int f = 0; f < files.Length; f++)
            {
                ExternalNpcConfigFile? config = ReadConfigInternal(files[f]);
                if (config == null) continue;

                RegisterDefinitionSourceFile(config, files[f]);

                definition = config.Npcs.FirstOrDefault(n => string.Equals(n.Id, definitionId, StringComparison.OrdinalIgnoreCase));
                if (definition != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Inserts or updates a definition inside <c>wizard_placements.json</c>.
        /// Mod-provided files are not modified.
        /// </summary>
        public static bool UpsertDefinition(ExternalNpcDefinition? definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                return false;

            string targetFile = DefinitionSourceFilesById.TryGetValue(definition.Id, out string existingFilePath)
                && !string.IsNullOrWhiteSpace(existingFilePath)
                ? existingFilePath
                : GetWizardFilePath();

            return UpsertDefinitionToFile(definition, targetFile);
        }

        /// <summary>
        /// Inserts or updates a definition inside a mod-specific placement file.
        /// </summary>
        public static bool UpsertDefinitionToModFile(ExternalNpcDefinition? definition, string modFileName, out string savedFileName)
        {
            savedFileName = string.Empty;
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                return false;

            if (!TryBuildModFilePath(modFileName, out string filePath, out savedFileName))
                return false;

            return UpsertDefinitionToFile(definition, filePath);
        }

        private static bool UpsertDefinitionToFile(ExternalNpcDefinition definition, string filePath)
        {
            EnsureFolder(GetNpcPlacementFolderPath());
            DefinitionSourceFilesById.TryGetValue(definition.Id, out string? previousFilePath);

            ExternalNpcConfigFile config = ReadConfigInternal(filePath) ?? new ExternalNpcConfigFile();

            ExternalNpcModuleRegistry.EnsureDefaults(definition);
            int existingIndex = config.Npcs.FindIndex(n => string.Equals(n.Id, definition.Id, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
                config.Npcs[existingIndex] = definition;
            else
                config.Npcs.Add(definition);

            bool saved = SaveConfigToFile(config, filePath);
            if (saved)
            {
                if (!string.IsNullOrWhiteSpace(previousFilePath)
                    && !string.Equals(previousFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    RemoveDefinitionFromFile(definition.Id, previousFilePath);
                }

                RegisterDefinitionSourceFile(definition.Id, filePath);
            }

            return saved;
        }

        /// <summary>
        /// Reads all definitions from every <c>*.json</c> file in the placement folder.
        /// </summary>
        public static List<ExternalNpcDefinition> ReadAllDefinitions()
        {
            string folderPath = GetNpcPlacementFolderPath();
            EnsureFolder(folderPath);

            List<ExternalNpcDefinition> result = [];
            DefinitionSourceFilesById.Clear();

            string[] files;
            try { files = Directory.GetFiles(folderPath, "*.json"); }
            catch { return result; }

            for (int f = 0; f < files.Length; f++)
            {
                ExternalNpcConfigFile? config = ReadConfigInternal(files[f]);
                if (config == null) continue;

                RegisterDefinitionSourceFile(config, files[f]);

                for (int i = 0; i < config.Npcs.Count; i++)
                {
                    ExternalNpcModuleRegistry.EnsureDefaults(config.Npcs[i]);
                    result.Add(config.Npcs[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a default definition template with baseline module payloads.
        /// </summary>
        public static ExternalNpcDefinition CreateDefaultDefinition()
        {
            ExternalNpcDefinition definition = new()
            {
                Id = "my_first_custom_npc",
                Name = "Custom NPC",
                PositionX = 0f,
                PositionY = 0f,
                Enabled = true
            };
            ExternalNpcModuleRegistry.EnsureDefaults(definition);
            return definition;
        }

        private static bool SaveConfigToFile(ExternalNpcConfigFile config, string filePath)
        {
            try
            {
                for (int i = 0; i < config.Npcs.Count; i++)
                    ExternalNpcModuleRegistry.EnsureDefaults(config.Npcs[i]);

                File.WriteAllText(filePath, JsonSerializer.Serialize(config, JsonOptions));
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save NPC config '{filePath}': {ex.Message}");
                return false;
            }
        }

        private static ExternalNpcConfigFile? ReadConfigInternal(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                string raw = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(raw))
                    return null;

                return JsonSerializer.Deserialize<ExternalNpcConfigFile>(raw, JsonOptions);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to read external NPC config '{filePath}': {ex.Message}");
                return null;
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                try { Directory.CreateDirectory(folderPath); }
                catch (Exception ex) { MelonLogger.Error($"Failed to create NPC placement folder '{folderPath}': {ex.Message}"); }
            }
        }

        private static void RegisterDefinitionSourceFile(ExternalNpcConfigFile config, string filePath)
        {
            for (int i = 0; i < config.Npcs.Count; i++)
            {
                ExternalNpcDefinition definition = config.Npcs[i];
                if (string.IsNullOrWhiteSpace(definition.Id))
                    continue;

                RegisterDefinitionSourceFile(definition.Id, filePath);
            }
        }

        private static void RegisterDefinitionSourceFile(string definitionId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(filePath))
                return;

            DefinitionSourceFilesById[definitionId] = filePath;
        }

        private static void RemoveDefinitionFromFile(string definitionId, string filePath)
        {
            ExternalNpcConfigFile? config = ReadConfigInternal(filePath);
            if (config == null)
                return;

            int removedCount = config.Npcs.RemoveAll(definition => string.Equals(definition.Id, definitionId, StringComparison.OrdinalIgnoreCase));
            if (removedCount == 0)
                return;

            SaveConfigToFile(config, filePath);
        }

        private static bool TryBuildModFilePath(string rawModFileName, out string filePath, out string fileName)
        {
            filePath = string.Empty;
            fileName = string.Empty;

            string sanitized = SanitizeFileName(rawModFileName);
            if (string.IsNullOrWhiteSpace(sanitized))
                return false;

            fileName = sanitized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? sanitized
                : $"{sanitized}.json";
            filePath = Path.Combine(GetNpcPlacementFolderPath(), fileName);
            return true;
        }

        private static string SanitizeFileName(string rawValue)
        {
            string sanitized = rawValue.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                sanitized = sanitized.Replace(invalidChars[i], '_');

            return sanitized;
        }

        /// <summary>
        /// Root DTO for an external NPC configuration file.
        /// </summary>
        public sealed class ExternalNpcConfigFile
        {
            /// <summary>
            /// Schema version for forward compatibility.
            /// </summary>
            public int Version { get; set; } = 1;

            /// <summary>
            /// NPC definitions contained in this file.
            /// </summary>
            public List<ExternalNpcDefinition> Npcs { get; set; } = [];
        }

        /// <summary>
        /// DTO for one external NPC definition.
        /// </summary>
        public sealed class ExternalNpcDefinition
        {
            /// <summary>
            /// Stable unique identifier for this definition.
            /// </summary>
            public string Id { get; set; } = "";

            /// <summary>
            /// NPC display name.
            /// </summary>
            public string Name { get; set; } = "";

            /// <summary>
            /// Spawn position X.
            /// </summary>
            public float PositionX { get; set; }

            /// <summary>
            /// Spawn position Y.
            /// </summary>
            public float PositionY { get; set; }

            /// <summary>
            /// Extensible module payloads keyed by module id.
            /// </summary>
            public Dictionary<string, string> ModuleConfig { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// If false, startup spawning skips this definition.
            /// </summary>
            public bool Enabled { get; set; } = true;
        }
    }
}

