using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppDrova.Utilities;
using Il2CppInterop.Runtime;
using Il2CppTypeObj = Il2CppSystem.Type;
#if DEBUG
using Il2CppNodeCanvas.Framework.Internal;
using Il2CppSirenix.Serialization;
#endif
using MelonLoader;
using System.Text.Json;
using System.Reflection;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Store
{
    /// <summary>
    /// A store for dialogues.
    /// </summary>
    public static class DialogueStore
    {
        private const string DialogueFolderName = "NpcDialogues";

        /// <summary>
        /// If the patches were already applied
        /// </summary>
        private static bool _isInitialized;

        [Serializable]
        private sealed class SaveData
        {
            public int Version { get; set; } = 1;
            public string DialogueId { get; set; } = string.Empty;
            public string SerializedGraphBytesBase64 { get; set; } = string.Empty;
            public List<ObjectReferenceData> ObjectReferences { get; set; } = [];
        }

        [Serializable]
        private sealed class ObjectReferenceData
        {
            public bool IsNull { get; set; }
            public string Name { get; set; } = string.Empty;
            public string TypeName { get; set; } = string.Empty;
            public string HierarchyPath { get; set; } = string.Empty;
            public string StableId { get; set; } = string.Empty;
            public int InstanceId { get; set; }
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
            => Path.Combine(Utils.SavePath, DialogueFolderName);

        /// <summary>
        /// Gets the absolute path for one dialogue id.
        /// </summary>
        public static string GetDialogueFilePath(string dialogueId)
            => Path.Combine(GetDialogueFolderPath(), $"{SanitizeFileName(dialogueId)}.json");

        /// <summary>
        /// Saves a dialogue.
        /// </summary>
        public static bool SaveDialogue(string dialogueId, DialogueTree dialogue)
        {
            if (dialogue == null || string.IsNullOrWhiteSpace(dialogueId))
                return false;

            try
            {
                EnsureFolder();

                dialogue.Serialize(null);
                byte[] serializedBytes = dialogue._serializedBytes?.ToArray() ?? [];

                SaveData saveData = new()
                {
                    DialogueId = dialogueId.Trim(),
                    SerializedGraphBytesBase64 = serializedBytes.Length == 0
                        ? string.Empty
                        : Convert.ToBase64String(serializedBytes),
                    ObjectReferences = BuildObjectReferenceData(dialogue._objectByteReferences)
                };

                if (string.IsNullOrWhiteSpace(saveData.SerializedGraphBytesBase64))
                    return false;

                string savePath = GetDialogueFilePath(saveData.DialogueId);
                File.WriteAllText(savePath, JsonSerializer.Serialize(saveData, JsonOptions));
#if DEBUG
                TrySaveDebugGraphJson(saveData.DialogueId, dialogue);
#endif
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to save dialogue '{dialogueId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tries to patch an existing in-memory <see cref="DialogueTree"/> instance with the bytes
        /// previously saved to the store under <paramref name="dialogueId"/>.
        /// This allows modifying a vanilla game dialogue at runtime: the live object that NPCs
        /// already reference is mutated in-place, so no reference reassignment is required.
        /// </summary>
        /// <param name="dialogueId">The id that was used when saving the dialogue.</param>
        /// <returns><c>true</c> if a matching in-memory instance was found and patched successfully.</returns>
        public static bool TryPatchInMemoryDialogue(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
                return false;

            string filePath = GetDialogueFilePath(dialogueId.Trim());
            if (!File.Exists(filePath))
                return false;

            try
            {
                string raw = File.ReadAllText(filePath);
                SaveData? saveData = JsonSerializer.Deserialize<SaveData>(raw, JsonOptions);
                if (saveData == null || string.IsNullOrWhiteSpace(saveData.SerializedGraphBytesBase64))
                    return false;

                byte[] bytes = Convert.FromBase64String(saveData.SerializedGraphBytesBase64);
                if (bytes.Length == 0)
                    return false;

                // Find the matching live DialogueTree instance – first by Key, then by name.
                string trimmedId = dialogueId.Trim();
                DialogueTree[] allTrees = Resources.FindObjectsOfTypeAll<DialogueTree>();
                DialogueTree? target = allTrees.FirstOrDefault(t => t.Key == trimmedId)
                                    ?? allTrees.FirstOrDefault(t => t.name == trimmedId);

                if (target == null)
                {
                    MelonLogger.Warning($"TryPatchInMemoryDialogue: no in-memory DialogueTree found for id '{dialogueId}'.");
                    return false;
                }

                target._serializedBytes = bytes;
                Dictionary<Il2CppTypeObj, UnityEngine.Object[]> candidateCache = [];
                Il2CppSystem.Collections.Generic.List<UnityEngine.Object> restoredReferences =
                    BuildObjectReferenceList(saveData.ObjectReferences, out int resolvedReferences, candidateCache);
                if (resolvedReferences > 0 || target._objectByteReferences == null || target._objectByteReferences.Count == 0)
                    target._objectByteReferences = restoredReferences;
                target.SelfDeserialize();
                target.DeserializeIfNotDoneYet(true);

                MelonLogger.Msg($"TryPatchInMemoryDialogue: patched in-memory DialogueTree '{target.name}' (Key='{target.Key}').");
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to patch in-memory dialogue '{dialogueId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tries to load a dialogue by id from the custom dialogue store.
        /// </summary>
        public static bool TryLoadDialogue(string dialogueId, out DialogueTree? dialogue)
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
                if (saveData == null || string.IsNullOrWhiteSpace(saveData.SerializedGraphBytesBase64))
                    return false;

                DialogueTree loaded = ScriptableObject.CreateInstance<DialogueTree>();

                byte[] bytes = Convert.FromBase64String(saveData.SerializedGraphBytesBase64);
                if (bytes.Length == 0)
                    return false;

                loaded._serializedBytes = bytes;
                Dictionary<Il2CppTypeObj, UnityEngine.Object[]> candidateCache = [];
                loaded._objectByteReferences = BuildObjectReferenceList(saveData.ObjectReferences, out int resolvedReferences, candidateCache);
                if (resolvedReferences == 0 && TryGetLiveObjectReferences(dialogueId, out Il2CppSystem.Collections.Generic.List<UnityEngine.Object>? liveReferences))
                    loaded._objectByteReferences = liveReferences;
                loaded.UseByteSerialization = true;
                loaded.SelfDeserialize();

                if (loaded.allNodes == null || loaded.allNodes.Count == 0)
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

        /// <summary>
        /// One-time startup pass: iterates every JSON file in the dialogue store and patches
        /// the matching in-memory <see cref="Il2CppNodeCanvas.DialogueTrees.DialogueTree"/>
        /// instance (if already loaded). Call this once after all game assets are in memory
        /// (e.g. when the main menu scene finishes loading).
        /// </summary>
        public static void PatchAllSavedDialogues()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            string folderPath = GetDialogueFolderPath();
            if (!Directory.Exists(folderPath))
                return;

            string[] files = Directory.GetFiles(folderPath, "*.json");
            if (files.Length == 0)
                return;

            int patched = 0;
            foreach (string file in files)
            {
                try
                {
                    string raw = File.ReadAllText(file);
                    SaveData? saveData = JsonSerializer.Deserialize<SaveData>(raw, JsonOptions);
                    if (saveData == null || string.IsNullOrWhiteSpace(saveData.DialogueId))
                        continue;

                    if (TryPatchInMemoryDialogue(saveData.DialogueId))
                        patched++;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"PatchAllSavedDialogues: failed to process '{file}': {ex.Message}");
                }
            }

            MelonLogger.Msg($"PatchAllSavedDialogues: patched {patched}/{files.Length} dialogue tree(s).");
        }

        private static void EnsureFolder()
        {
            string folderPath = GetDialogueFolderPath();
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }

        private static List<ObjectReferenceData> BuildObjectReferenceData(Il2CppSystem.Collections.Generic.List<UnityEngine.Object>? objectReferences)
        {
            List<ObjectReferenceData> serializedReferences = [];
            if (objectReferences == null || objectReferences.Count == 0)
                return serializedReferences;

            for (int i = 0; i < objectReferences.Count; i++)
            {
                UnityEngine.Object unityObject = objectReferences[i];
                if (unityObject == null)
                {
                    serializedReferences.Add(new ObjectReferenceData { IsNull = true });
                    continue;
                }

                Type objectType = unityObject.GetType();
                string typeName = objectType.AssemblyQualifiedName ?? objectType.FullName ?? string.Empty;

                serializedReferences.Add(new ObjectReferenceData
                {
                    IsNull = false,
                    Name = unityObject.name,
                    TypeName = typeName,
                    HierarchyPath = GetHierarchyPath(unityObject),
                    StableId = TryGetStableObjectId(unityObject),
                    InstanceId = unityObject.GetInstanceID()
                });
            }

            return serializedReferences;
        }

        private static Il2CppSystem.Collections.Generic.List<UnityEngine.Object> BuildObjectReferenceList(
            List<ObjectReferenceData>? serializedReferences,
            out int resolvedReferences,
            Dictionary<Il2CppTypeObj, UnityEngine.Object[]>? candidateCache = null)
        {
            Il2CppSystem.Collections.Generic.List<UnityEngine.Object> references = new();
            resolvedReferences = 0;
            if (serializedReferences == null || serializedReferences.Count == 0)
                return references;

            candidateCache ??= [];

            for (int i = 0; i < serializedReferences.Count; i++)
            {
                ObjectReferenceData referenceData = serializedReferences[i];
                if (referenceData.IsNull)
                {
                    references.Add(null);
                    continue;
                }

                UnityEngine.Object resolved = ResolveObjectReference(referenceData, candidateCache);
                if (resolved != null)
                    resolvedReferences++;

                references.Add(resolved);
            }

            return references;
        }

        private static UnityEngine.Object? ResolveObjectReference(
            ObjectReferenceData referenceData,
            Dictionary<Il2CppTypeObj, UnityEngine.Object[]> candidateCache)
        {
            if (string.IsNullOrWhiteSpace(referenceData.TypeName))
                return null;

            Type? objectType = Type.GetType(referenceData.TypeName, false)
                ?? Type.GetType(referenceData.TypeName.Split(',')[0].Trim(), false);
            if (objectType == null)
                return null;

            Il2CppTypeObj? il2CppType = Il2CppType.From(objectType, false);
            if (il2CppType == null)
                return null;

            UnityEngine.Object[] candidates = GetCandidatesForType(il2CppType, candidateCache);
            if (candidates.Length == 0)
                return null;

            UnityEngine.Object byInstanceId = null;
            UnityEngine.Object byStableId = null;
            UnityEngine.Object fallbackByName = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                UnityEngine.Object candidate = candidates[i];
                if (candidate == null)
                    continue;

                if (!il2CppType.IsAssignableFrom(candidate.GetIl2CppType()))
                    continue;

                if (referenceData.InstanceId != 0 && candidate.GetInstanceID() == referenceData.InstanceId)
                    byInstanceId = candidate;
                if (!string.IsNullOrWhiteSpace(referenceData.StableId)
                    && string.Equals(TryGetStableObjectId(candidate), referenceData.StableId, StringComparison.Ordinal))
                {
                    byStableId = candidate;
                    break;
                }

                if (!string.Equals(candidate.name, referenceData.Name, StringComparison.Ordinal))
                    continue;

                if (fallbackByName == null)
                    fallbackByName = candidate;

                if (string.IsNullOrWhiteSpace(referenceData.HierarchyPath))
                    continue;

                string candidatePath = GetHierarchyPath(candidate);
                if (string.Equals(candidatePath, referenceData.HierarchyPath, StringComparison.Ordinal))
                    return candidate;
            }
            
            if (byStableId != null)
                return byStableId;
            
            if (fallbackByName != null)
                return fallbackByName;

            return byInstanceId;
        }

        private static UnityEngine.Object[] GetCandidatesForType(
            Il2CppTypeObj il2CppType,
            Dictionary<Il2CppTypeObj, UnityEngine.Object[]> candidateCache)
        {
            if (candidateCache.TryGetValue(il2CppType, out UnityEngine.Object[] cachedCandidates))
                return cachedCandidates;

            UnityEngine.Object[] candidates;
            try
            {
                candidates = Resources.FindObjectsOfTypeAll(il2CppType);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"DialogueStore: failed to enumerate objects of type '{il2CppType.FullName}': {ex.Message}");
                candidates = [];
            }

            candidateCache[il2CppType] = candidates;
            return candidates;
        }

        private static bool TryGetLiveObjectReferences(string dialogueId, out Il2CppSystem.Collections.Generic.List<UnityEngine.Object>? references)
        {
            references = null;
            string trimmedId = dialogueId.Trim();
            DialogueTree[] allTrees = Resources.FindObjectsOfTypeAll<DialogueTree>();
            DialogueTree? liveTree = allTrees.FirstOrDefault(t => t.Key == trimmedId)
                                  ?? allTrees.FirstOrDefault(t => t.name == trimmedId);
            if (liveTree == null || liveTree._objectByteReferences == null)
                return false;

            references = liveTree._objectByteReferences;
            return references.Count > 0;
        }

        private static string TryGetStableObjectId(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
                return string.Empty;

            // IL2CPP objects are safer to probe via TryCast than C# type pattern matching.
            ScriptableGuidObject guidObject = unityObject.TryCast<ScriptableGuidObject>();
            if (guidObject != null)
            {
                string guid = guidObject.GUID;
                if (!string.IsNullOrWhiteSpace(guid))
                    return guid;
            }
            Item item = unityObject.TryCast<Item>();
            if (item != null)
            {
                string guid = item.Guid;
                if (!string.IsNullOrWhiteSpace(guid))
                    return guid;
            }
            AGVarBase varBase = unityObject.TryCast<AGVarBase>();
            if (varBase != null)
            {
                string id = varBase.Id;
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }
            GVarList list = unityObject.TryCast<GVarList>();
            if (list != null) {
                string guid = list.Guid;
                if (!string.IsNullOrWhiteSpace(guid))
                    return guid;
            }

            Type objectType = unityObject.GetType();
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string[] propertyNames = ["Key", "ID", "Guid", "GUID", "guid"];
            for (int i = 0; i < propertyNames.Length; i++)
            {
                PropertyInfo? propertyInfo = objectType.GetProperty(propertyNames[i], bindingFlags);
                if (propertyInfo == null || propertyInfo.GetIndexParameters().Length != 0)
                    continue;

                object? value = propertyInfo.GetValue(unityObject, null);
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                    return stringValue;
            }

            string[] fieldNames = ["_key", "_id", "_guidString", "guid", "Guid"];
            for (int i = 0; i < fieldNames.Length; i++)
            {
                FieldInfo? fieldInfo = objectType.GetField(fieldNames[i], bindingFlags);
                if (fieldInfo == null)
                    continue;

                object? value = fieldInfo.GetValue(unityObject);
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                    return stringValue;
            }

            return string.Empty;
        }

        private static string GetHierarchyPath(UnityEngine.Object unityObject)
        {
            Transform targetTransform = null;
            if (unityObject is GameObject gameObject)
                targetTransform = gameObject.transform;
            else if (unityObject is Component component)
                targetTransform = component.transform;

            if (targetTransform == null)
                return string.Empty;

            List<string> pathSegments = [];
            Transform current = targetTransform;
            while (current != null)
            {
                pathSegments.Add(current.name);
                current = current.parent;
            }

            pathSegments.Reverse();
            return string.Join("/", pathSegments);
        }

#if DEBUG
        private static void TrySaveDebugGraphJson(string dialogueId, DialogueTree dialogue)
        {
            try
            {
                Il2CppSystem.Collections.Generic.List<UnityEngine.Object> objectReferences;
                Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte> graphJsonBytes =
                    SerializationUtility.SerializeValue<GraphSource>(
                        dialogue.graphSource.Pack(dialogue),
                        DataFormat.JSON,
                        out objectReferences,
                        null);

                string debugPath = Path.Combine(GetDialogueFolderPath(), $"{SanitizeFileName(dialogueId)}.graph.dev.json");
                File.WriteAllBytes(debugPath, graphJsonBytes);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to write debug graph JSON for dialogue '{dialogueId}': {ex.Message}");
            }
        }
#endif

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
