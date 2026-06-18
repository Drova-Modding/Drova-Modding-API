using Il2CppDrova;
using MelonLoader;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Pre-creates and caches <see cref="EntityInfo"/> objects for all external NPC definitions
    /// so they are available for cross-references before any NPC is actually spawned.
    /// <para>
    /// Call <see cref="Initialize"/> once at main-menu load (before gameplay starts).
    /// External mods that need to reference a custom NPC's <see cref="EntityInfo"/> inside
    /// dialogue graphs, quest conditions, or other cross-references can look it up by
    /// definition id (<see cref="TryGetByDefinitionId"/>) or by GUID (<see cref="TryGetByGuid"/>)
    /// immediately after the menu has loaded, without waiting for the NPC to spawn.
    /// </para>
    /// <para>
    /// When the NPC eventually spawns, <see cref="ExternalEntityInfoModule"/> reuses the
    /// same <see cref="EntityInfo"/> instance from this registry so all previously captured
    /// references to remain valid.
    /// </para>
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
            // Previously registered instances are HideAndDontSave, so Unity will not unload
            // them on its own. Destroy the still-living ones before dropping our references to
            // avoid leaking a ScriptableObject per main-menu visit.
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
        public static bool TryGetByDefinitionId(string? definitionId, out EntityInfo? entityInfo)
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
        public static bool TryGetByGuid(string? guid, out EntityInfo? entityInfo)
        {
            entityInfo = null;
            if (string.IsNullOrWhiteSpace(guid))
                return false;

            return ByGuid.TryGetValue(guid, out entityInfo);
        }

        /// <summary>
        /// Returns the pre-created <see cref="EntityInfo"/> for <paramref name="guid"/> if one
        /// already exists in the registry. Otherwise, creates a new bare instance (GUID only),
        /// registers it, and returns it. Used internally by <see cref="ExternalEntityInfoModule"/>
        /// to guarantee a single consistent instance per GUID.
        /// </summary>
        internal static EntityInfo GetOrCreate(string guid)
        {
            // A cached wrapper whose underlying ScriptableObject has been unloaded by Unity
            // (e.g. Resources.UnloadUnusedAssets during a scene transition) compares == null.
            // Treat that as a miss and recreate so callers never receive a destroyed instance.
            if (ByGuid.TryGetValue(guid, out EntityInfo existing) && existing != null)
                return existing;

            EntityInfo entityInfo = CreateBare(guid);
            ByGuid[guid] = entityInfo;
            return entityInfo;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a minimal <see cref="EntityInfo"/> (GUID only) and marks it
        /// <see cref="HideFlags.HideAndDontSave"/> so Unity does not unload it via
        /// <c>Resources.UnloadUnusedAssets</c> during scene transitions. These instances are
        /// referenced only by this registry's managed dictionaries until an NPC spawns, and a
        /// managed reference alone does not keep a Unity asset alive.
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
