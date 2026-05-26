using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Store
{
    /// <summary>
    /// A store for dialogues.
    /// </summary>
    public class DialogueStore
    {
        private const string DialogueFolderName = "NpcDialogues";

        [Serializable]
        private sealed class SaveData
        {
            public int Version { get; set; } = 1;
            public string DialogueId { get; set; } = string.Empty;
            public string SerializedGraph { get; set; } = string.Empty;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Gets the absolute path where custom dialogue files are stored.
        /// </summary>
        public static string GetDialogueFolderPath()
            => Path.Combine(Utils.SavePath, "..", DialogueFolderName);

        /// <summary>
        /// Gets the absolute path for one dialogue id.
        /// </summary>
        public static string GetDialogueFilePath(string dialogueId)
            => Path.Combine(GetDialogueFolderPath(), $"{SanitizeFileName(dialogueId)}.json");

        /// <summary>
        /// Saves a dialogue.
        /// </summary>
        public bool SaveDialogue(string dialogueId, DialogueTree dialogue)
        {
            if (dialogue == null || string.IsNullOrWhiteSpace(dialogueId))
                return false;

            try
            {
                EnsureFolder();
                SaveData saveData = new()
                {
                    DialogueId = dialogueId.Trim(),
                    SerializedGraph = dialogue.Serialize(null) ?? string.Empty
                };

                if (string.IsNullOrWhiteSpace(saveData.SerializedGraph))
                    return false;

                string savePath = GetDialogueFilePath(saveData.DialogueId);
                File.WriteAllText(savePath, JsonSerializer.Serialize(saveData, JsonOptions));
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to save dialogue '{dialogueId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tries to load a dialogue by id from the custom dialogue store.
        /// </summary>
        public bool TryLoadDialogue(string dialogueId, out DialogueTree? dialogue)
        {
            dialogue = null;
            if (string.IsNullOrWhiteSpace(dialogueId))
                return false;

            string filePath = GetDialogueFilePath(dialogueId.Trim());
            if (!File.Exists(filePath))
                return false;

            try
            {
                string raw = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(raw))
                    return false;

                SaveData? saveData = JsonSerializer.Deserialize<SaveData>(raw, JsonOptions);
                if (saveData == null || string.IsNullOrWhiteSpace(saveData.SerializedGraph))
                    return false;

                DialogueTree loaded = ScriptableObject.CreateInstance<DialogueTree>();
                bool deserialized = loaded.Deserialize(
                    saveData.SerializedGraph,
                    null,
                    null,
                    null,
                    true);

                if (!deserialized)
                    return false;

                loaded.DeserializeIfNotDoneYet(true);
                loaded.name = saveData.DialogueId;
                dialogue = loaded;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to load dialogue '{dialogueId}': {ex.Message}");
                return false;
            }
        }

        private static void EnsureFolder()
        {
            string folderPath = GetDialogueFolderPath();
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }

        private static string SanitizeFileName(string dialogueId)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string trimmed = dialogueId.Trim();
            for (int i = 0; i < invalid.Length; i++)
                trimmed = trimmed.Replace(invalid[i], '_');

            return string.IsNullOrWhiteSpace(trimmed) ? "dialogue" : trimmed;
        }
    }
}
