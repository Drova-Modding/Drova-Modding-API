using System.Diagnostics.CodeAnalysis;
using Il2CppDrova;
using MelonLoader;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Pre-creates the <see cref="EntityInfo"/> of every external NPC definition at main-menu load, so dialogues and
    /// quests can reference it before the NPC spawns. <see cref="ExternalEntityInfoModule"/> reuses the same instance.
    /// </summary>
    public static class ExternalEntityInfoRegistry
    {
        private static readonly Dictionary<string, EntityInfo> ByDefinitionId = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, EntityInfo> ByGuid = new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Scans all external NPC placement JSON files and pre-creates a minimal
        /// <see cref="EntityInfo"/> (GUID only) for every definition that declares an
        /// <c>entityInfo</c> module payload. Existing entries are replaced on each call
        /// so the registry stays in sync when the main menu is revisited.
        /// </summary>
        public static void Initialize()
        {
            // HideAndDontSave instances are never unloaded by Unity, so they would leak once per main-menu visit.
            foreach (EntityInfo previous in ByGuid.Values)
            {
                if (previous != null)
                    UnityEngine.Object.Destroy(previous);
            }

            ByDefinitionId.Clear();
            ByGuid.Clear();

            List<ExternalNpcPlacementSystem.ExternalNpcDefinition> definitions = ExternalNpcPlacementSystem.ReadAllDefinitions();
            int registered = 0;

            for (int i = 0; i < definitions.Count; i++)
            {
                ExternalNpcPlacementSystem.ExternalNpcDefinition definition = definitions[i];

                if (string.IsNullOrWhiteSpace(definition.Id))
                    continue;

                if (!definition.ModuleConfig.TryGetValue(ExternalEntityInfoModule.ModuleKey, out string? payload)
                    || string.IsNullOrWhiteSpace(payload))
                {
                    continue;
                }

                GuidOnlyPayload? parsed = TryParse(payload);
                if (string.IsNullOrWhiteSpace(parsed?.Guid))
                    continue;

                string guid = parsed.Guid;
                EntityInfo entityInfo = CreateBare(guid, parsed.UnityPropertyName);

                ByDefinitionId[definition.Id] = entityInfo;
                ByGuid[guid] = entityInfo;
                registered++;
            }

            if (registered > 0)
                MelonLogger.Msg($"[ExternalEntityInfoRegistry] Pre-registered {registered} EntityInfo(s) from external NPC definitions.");
        }

        /// <summary>
        /// Tries to get the pre-created <see cref="EntityInfo"/> for the given definition id.
        /// Returns <see langword="false"/> when the definition is unknown or has no entity-info payload.
        /// </summary>
        /// <param name="definitionId">The stable definition id set in the placement JSON (e.g. <c>"my_custom_npc"</c>).</param>
        /// <param name="entityInfo">The pre-created instance, or <see langword="null"/>.</param>
        public static bool TryGetByDefinitionId(string? definitionId, [NotNullWhen(true)] out EntityInfo? entityInfo)
        {
            entityInfo = null;
            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            return ByDefinitionId.TryGetValue(definitionId, out entityInfo);
        }

        /// <summary>
        /// Tries to get the pre-created <see cref="EntityInfo"/> by its stable GUID.
        /// Returns <see langword="false"/> when no entry with that GUID exists in the registry.
        /// </summary>
        /// <param name="guid">The GUID string stored in the definition's <c>entityInfo.Guid</c> field.</param>
        /// <param name="entityInfo">The pre-created instance, or <see langword="null"/>.</param>
        public static bool TryGetByGuid(string? guid, [NotNullWhen(true)] out EntityInfo? entityInfo)
        {
            entityInfo = null;
            if (string.IsNullOrWhiteSpace(guid))
                return false;

            return ByGuid.TryGetValue(guid, out entityInfo);
        }

        /// <summary>
        /// Returns the registered <see cref="EntityInfo"/> for <paramref name="guid"/>, creating a GUID-only one if missing.
        /// </summary>
        internal static EntityInfo GetOrCreate(string guid)
        {
            if (ByGuid.TryGetValue(guid, out EntityInfo existing) && existing != null)
                return existing;

            EntityInfo entityInfo = CreateBare(guid);
            ByGuid[guid] = entityInfo;
            return entityInfo;
        }

        /// <summary>
        /// Creates a GUID-only <see cref="EntityInfo"/> marked <see cref="HideFlags.HideAndDontSave"/>, since until an NPC
        /// spawns only managed references hold it and <c>Resources.UnloadUnusedAssets</c> would unload it.
        /// </summary>
        private static EntityInfo CreateBare(string guid, string? name = null)
        {
            EntityInfo entityInfo = EntityInfo.CreateUndefined();
            entityInfo._guid = guid;
            if (!string.IsNullOrWhiteSpace(name))
                entityInfo.name = name;
            entityInfo.hideFlags = HideFlags.HideAndDontSave;
            return entityInfo;
        }

        private static GuidOnlyPayload? TryParse(string payload)
        {
            try
            {
                return JsonSerializer.Deserialize<GuidOnlyPayload>(payload, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Minimal DTO used to extract the GUID and name from an entityInfo payload.</summary>
        private sealed class GuidOnlyPayload
        {
            public string? Guid { get; set; }
            public string? UnityPropertyName { get; set; }
        }
    }
}
