using System.Diagnostics.CodeAnalysis;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using Il2CppNodeCanvas.Framework.Internal;
using Il2CppDrova.Utilities;
using Il2CppInterop.Runtime;
using Il2CppTypeObj = Il2CppSystem.Type;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using NodeTask = Il2CppNodeCanvas.Framework.Task;
#if DEBUG
using Il2CppSirenix.Serialization;
#endif
using MelonLoader;
using System.Text.Json;
using System.Reflection;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Store
{
    /// <summary>
    /// Persists a NodeCanvas <see cref="Graph"/> as serialized bytes plus its object references, one JSON file per id.
    /// Patching mutates the live asset in place, so existing references pick up the change.
    /// </summary>
    internal static class GraphStore
    {
        private const string DevJsonSuffix = ".graph.dev.json";

        private static readonly HashSet<int> CheckedInstances = [];

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

        internal static string GetFilePath(string folderPath, string graphId)
            => Path.Combine(folderPath, $"{SanitizeFileName(graphId)}.json");

        /// <summary>
        /// Serializes <paramref name="graph"/> and writes it to the store folder under <paramref name="graphId"/>.
        /// Lazily loaded routine trees hold no nodes until deserialized, so they are deserialized first and an empty graph is never written.
        /// </summary>
        internal static bool Save(string folderPath, string graphId, Graph graph)
        {
            if (graph == null || string.IsNullOrWhiteSpace(graphId))
                return false;

            try
            {
                Directory.CreateDirectory(folderPath);

                if (graph.allNodes == null || graph.allNodes.Count == 0)
                {
                    graph.SelfDeserialize();
                    graph.DeserializeIfNotDoneYet(true);
                }
                if (graph.allNodes == null || graph.allNodes.Count == 0)
                {
                    MelonLogger.Warning($"Not saving graph '{graphId}': it has no nodes, and saving it would wipe the stored graph.");
                    return false;
                }
                if (!HasValidStartNode(graph))
                {
                    MelonLogger.Warning($"Not saving graph '{graphId}': it has no start node, so it would do nothing when it runs.");
                    return false;
                }

                CollectTasks(graph);
                graph.Serialize(null);
                byte[] serializedBytes = graph._serializedBytes?.ToArray() ?? [];

                SaveData saveData = new()
                {
                    DialogueId = graphId.Trim(),
                    SerializedGraphBytesBase64 = serializedBytes.Length == 0
                        ? string.Empty
                        : Convert.ToBase64String(serializedBytes),
                    ObjectReferences = BuildObjectReferenceData(graph._objectByteReferences)
                };

                if (string.IsNullOrWhiteSpace(saveData.SerializedGraphBytesBase64))
                    return false;

                File.WriteAllText(GetFilePath(folderPath, saveData.DialogueId), JsonSerializer.Serialize(saveData, JsonOptions));
#if DEBUG
                TrySaveDebugGraphJson(folderPath, saveData.DialogueId, graph);
#endif
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to save graph '{graphId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Overwrites the live <typeparamref name="T"/> asset whose key or name is <paramref name="graphId"/> with the stored bytes.
        /// Ids in <paramref name="skipInstances"/> and runtime graphs (negative ids) are skipped. A null set always patches.
        /// </summary>
        internal static bool TryPatchInMemory<T>(string folderPath, string graphId, HashSet<int>? skipInstances = null) where T : Graph
        {
            if (string.IsNullOrWhiteSpace(graphId))
                return false;

            string trimmedId = graphId.Trim();
            if (!TryReadSaveData(GetFilePath(folderPath, trimmedId), out SaveData? saveData, out byte[]? bytes))
                return false;

            return Patch(FindLive(LiveGraphs<T>(), trimmedId), trimmedId, saveData, bytes, skipInstances);
        }

        /// <summary>
        /// Patches <paramref name="graph"/> from the file named after it, if there is one and the graph was not patched yet.
        /// </summary>
        internal static void PatchIfStored<T>(string folderPath, T graph, HashSet<int> patchedInstances) where T : Graph
        {
            // Each instance is looked up once: most graphs have no stored file, and a use asks again on every conversation.
            if (patchedInstances.Contains(graph.GetInstanceID()) || !CheckedInstances.Add(graph.GetInstanceID()))
                return;

            string graphId = graph.name;
            if (TryReadSaveData(GetFilePath(folderPath, graphId), out SaveData? saveData, out byte[]? bytes))
                Patch(graph, graphId, saveData, bytes, patchedInstances);
        }

        private static bool Patch<T>(T? target, string graphId, SaveData saveData, byte[] bytes, HashSet<int>? skipInstances) where T : Graph
        {
            try
            {
                if (target == null)
                {
                    if (skipInstances == null)
                        MelonLogger.Warning($"TryPatchInMemory: no in-memory {typeof(T).Name} found for id '{graphId}'.");
                    return false;
                }

                if (skipInstances != null && (target.GetInstanceID() < 0 || !skipInstances.Add(target.GetInstanceID())))
                    return false;

                target._serializedBytes = bytes;
                Il2CppSystem.Collections.Generic.List<UnityEngine.Object> restoredReferences =
                    BuildObjectReferenceList(saveData.ObjectReferences, out int resolvedReferences, []);
                if (resolvedReferences > 0 || target._objectByteReferences == null || target._objectByteReferences.Count == 0)
                    target._objectByteReferences = restoredReferences;
                target.SelfDeserialize();
                target.DeserializeIfNotDoneYet(true);
                CollectTasks(target);

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to patch in-memory graph '{graphId}': {ex}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new <typeparamref name="T"/> from the stored bytes of <paramref name="graphId"/>.
        /// </summary>
        internal static bool TryLoad<T>(string folderPath, string graphId, [NotNullWhen(true)] out T? graph) where T : Graph
        {
            graph = null;
            if (string.IsNullOrWhiteSpace(graphId))
                return false;

            if (!TryReadSaveData(GetFilePath(folderPath, graphId.Trim()), out SaveData? saveData, out byte[]? bytes))
                return false;

            try
            {
                T loaded = ScriptableObject.CreateInstance<T>();
                loaded._serializedBytes = bytes;
                loaded._objectByteReferences = BuildObjectReferenceList(saveData.ObjectReferences, out int resolvedReferences, []);
                if (resolvedReferences == 0)
                {
                    T? live = FindLive(LiveGraphs<T>(), graphId.Trim());
                    if (live != null && live._objectByteReferences != null && live._objectByteReferences.Count > 0)
                        loaded._objectByteReferences = live._objectByteReferences;
                }
                loaded.UseByteSerialization = true;
                loaded.LazyLoad = false;
                loaded.SelfDeserialize();
                loaded.DeserializeIfNotDoneYet(true);
                CollectTasks(loaded);

                if (loaded.allNodes == null || loaded.allNodes.Count == 0)
                {
                    MelonLogger.Warning($"Graph '{graphId}' deserialized to no nodes.");
                    return false;
                }
                loaded.name = graphId.Trim();
                graph = loaded;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to load graph '{graphId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Patches the live <typeparamref name="T"/> asset of every file in the store folder. The file name is the graph id.
        /// The id stored inside the file is used only when the file name is its sanitized form.
        /// </summary>
        /// <returns>How many graphs were patched</returns>
        internal static int PatchAll<T>(string folderPath, HashSet<int>? skipInstances = null) where T : Graph
        {
            if (!Directory.Exists(folderPath))
                return 0;

            string[] files = Directory.GetFiles(folderPath, "*.json");
            List<T>? live = null;
            int patched = 0;
            foreach (string file in files)
            {
                if (file.EndsWith(DevJsonSuffix, StringComparison.OrdinalIgnoreCase)) continue;
                if (!TryReadSaveData(file, out SaveData? saveData, out byte[]? bytes)) continue;

                string fileId = Path.GetFileNameWithoutExtension(file);
                string graphId = SanitizeFileName(saveData.DialogueId) == fileId ? saveData.DialogueId.Trim() : fileId;
                live ??= LiveGraphs<T>();
                if (Patch(FindLive(live, graphId), graphId, saveData, bytes, skipInstances)) patched++;
            }

            return patched;
        }

        /// <summary>
        /// Whether <paramref name="graph"/> is the live asset that saving under <paramref name="graphId"/> patches.
        /// Patching it again would rebuild its nodes under an editor still holding the old ones.
        /// </summary>
        internal static bool IsLiveAsset<T>(string graphId, T graph) where T : Graph
            => FindLive(LiveGraphs<T>(), graphId.Trim()) == graph;

        /// <summary>
        /// A graph only gives its tasks an owner through <c>GraphSource.allTasks</c>, and only the Unity editor fills that list.
        /// Without it every condition and action added at runtime throws when it runs, so the list is rebuilt from the nodes.
        /// </summary>
        private static void CollectTasks(Graph graph)
        {
            GraphSource? source = graph.graphSource;
            if (source == null || graph.allNodes == null)
            {
                return;
            }

            source.allTasks.Clear();
            source.allParameters.Clear();
            for (int i = 0; i < graph.allNodes.Count; i++)
            {
                Node node = graph.allNodes[i];
                Collect(source, node.Cast<IGraphElement>());
                for (int c = 0; c < node.outConnections.Count; c++)
                {
                    Collect(source, node.outConnections[c].Cast<IGraphElement>());
                }
            }

            graph.Cast<ITaskSystem>().UpdateTasksOwner();
            RepairEndChoices(graph);
        }

        /// <summary>
        /// The game sets the end flag only at runtime for a hub's "back" choice. A stored end choice throws in
        /// <c>RemoveBackChoice</c>, and an unconnected choice already ends the dialogue.
        /// </summary>
        private static void RepairEndChoices(Graph graph)
        {
            for (int i = 0; i < graph.allNodes.Count; i++)
            {
                DS_MultipleChoiceNode? node = graph.allNodes[i].TryCast<DS_MultipleChoiceNode>();
                if (node == null || node.TryCast<DS_HubNode>() != null || node.availableChoices == null)
                {
                    continue;
                }

                for (int c = 0; c < node.availableChoices.Count; c++)
                {
                    node.availableChoices[c].isEndNode = false;
                }
            }
        }

        private static void Collect(GraphSource source, IGraphElement element)
        {
            Il2CppCollections.List<NodeTask>? tasks = Graph.GetTasksInElement(element).TryCast<Il2CppCollections.List<NodeTask>>();
            for (int i = 0; tasks != null && i < tasks.Count; i++)
            {
                source.allTasks.Add(tasks[i]);
            }

            Il2CppCollections.List<BBParameter>? parameters = Graph.GetParametersInElement(element).TryCast<Il2CppCollections.List<BBParameter>>();
            for (int i = 0; parameters != null && i < parameters.Count; i++)
            {
                source.allParameters.Add(parameters[i]);
            }
        }

        /// <summary>
        /// Whether the graph has a start node that is one of its own nodes.
        /// </summary>
        internal static bool HasValidStartNode(Graph graph)
            => graph.primeNode != null && graph.allNodes != null && graph.allNodes.Contains(graph.primeNode);

        private static List<T> LiveGraphs<T>() where T : Graph
            => [.. Resources.FindObjectsOfTypeAll<T>()];

        /// <summary>
        /// The live graph with the id as key or name. Runtime copies share the asset's name, so a persistent asset is preferred.
        /// </summary>
        private static T? FindLive<T>(List<T> candidates, string graphId) where T : Graph
        {
            T? fallback = null;
            foreach (T candidate in candidates)
            {
                if (candidate.Key != graphId && candidate.name != graphId) continue;
                if (candidate.GetInstanceID() > 0) return candidate;
                fallback ??= candidate;
            }
            return fallback;
        }

        private static bool TryReadSaveData(string filePath, [NotNullWhen(true)] out SaveData? saveData, [NotNullWhen(true)] out byte[]? bytes)
        {
            saveData = null;
            bytes = null;
            if (!File.Exists(filePath))
                return false;

            try
            {
                saveData = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(filePath), JsonOptions);
                if (saveData == null || string.IsNullOrWhiteSpace(saveData.SerializedGraphBytesBase64))
                    return false;

                bytes = Convert.FromBase64String(saveData.SerializedGraphBytesBase64);
                return bytes.Length > 0;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"GraphStore: failed to read '{filePath}': {ex.Message}");
                return false;
            }
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
            Dictionary<Il2CppTypeObj, UnityEngine.Object[]> candidateCache)
        {
            Il2CppSystem.Collections.Generic.List<UnityEngine.Object> references = new();
            resolvedReferences = 0;
            if (serializedReferences == null || serializedReferences.Count == 0)
                return references;

            for (int i = 0; i < serializedReferences.Count; i++)
            {
                ObjectReferenceData referenceData = serializedReferences[i];
                if (referenceData.IsNull)
                {
                    references.Add(null);
                    continue;
                }

                UnityEngine.Object? resolved = ResolveObjectReference(referenceData, candidateCache);
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

            UnityEngine.Object? byInstanceId = null;
            UnityEngine.Object? byStableId = null;
            UnityEngine.Object? fallbackByName = null;
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
                    && IsStableIdMatch(TryGetStableObjectId(candidate), referenceData.StableId))
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
                MelonLogger.Warning($"GraphStore: failed to enumerate objects of type '{il2CppType.FullName}': {ex.Message}");
                candidates = [];
            }

            candidateCache[il2CppType] = candidates;
            return candidates;
        }

        /// <summary>
        /// Files written before variable ids were qualified by their list guid still match on the bare id.
        /// </summary>
        private static bool IsStableIdMatch(string candidateId, string storedId)
            => string.Equals(candidateId, storedId, StringComparison.Ordinal)
               || (!storedId.Contains('/') && candidateId.EndsWith("/" + storedId, StringComparison.Ordinal));

        private static string TryGetStableObjectId(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
                return string.Empty;

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
            // A variable id is only unique within its list (every quest list has a "QuestState"), so the list guid qualifies it.
            AGVarBase varBase = unityObject.TryCast<AGVarBase>();
            if (varBase != null)
            {
                string id = varBase.Id;
                GVarList? parent = varBase.GetParent();
                if (!string.IsNullOrWhiteSpace(id))
                    return parent != null && !string.IsNullOrWhiteSpace(parent.Guid) ? parent.Guid + "/" + id : id;
            }
            GVarList list = unityObject.TryCast<GVarList>();
            if (list != null)
            {
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
            Transform? targetTransform = null;
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
        private static void TrySaveDebugGraphJson(string folderPath, string graphId, Graph graph)
        {
            try
            {
                Il2CppSystem.Collections.Generic.List<UnityEngine.Object> objectReferences;
                Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte> graphJsonBytes =
                    SerializationUtility.SerializeValue(
                        graph.graphSource.Pack(graph),
                        DataFormat.JSON,
                        out objectReferences);

                File.WriteAllBytes(Path.Combine(folderPath, SanitizeFileName(graphId) + DevJsonSuffix), graphJsonBytes);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to write debug graph JSON for '{graphId}': {ex.Message}");
            }
        }
#endif

        private static string SanitizeFileName(string graphId)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string trimmed = graphId.Trim();
            for (int i = 0; i < invalid.Length; i++)
                trimmed = trimmed.Replace(invalid[i], '_');

            return string.IsNullOrWhiteSpace(trimmed) ? "graph" : trimmed;
        }
    }
}
