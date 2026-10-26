using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Quests;
using Il2Cpp;
using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Places creatures at fixed positions with fixed actor guids, the way the game places its own.
    /// The game saves an actor's death under its guid, so a killed creature stays dead in that save.
    /// </summary>
    public static class ExternalCreaturePlacementSystem
    {
        private const string FolderName = "CreaturePlacement";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly Dictionary<string, ExternalCreatureDefinition> DefinitionsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, LazyActor> SpawnedById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> DeadGuids = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Absolute path of the folder creature files are read from.
        /// </summary>
        public static string GetFolderPath() => Path.Combine(Utils.SavePath, FolderName);

        /// <summary>
        /// The creature definitions read at the last world load, by id.
        /// </summary>
        public static IReadOnlyDictionary<string, ExternalCreatureDefinition> Definitions => DefinitionsById;

        /// <summary>
        /// The actor guid of a placed creature id, as the save and <see cref="Il2CppDrova.EntityGameHandler.GetActorFromGuid"/> know it.
        /// </summary>
        public static string GetActorGuid(string id) => LazyActorCreator.StableGuid("Modding_API.Creature", id);

        /// <summary>
        /// Resolves a field name of the given <see cref="AddressableAccess"/> classes, or passes a raw asset guid through.
        /// </summary>
        public static bool TryResolveAsset(string nameOrGuid, [NotNullWhen(true)] out string? guid, params Type[] sources)
        {
            foreach (Type source in sources)
            {
                FieldInfo? field = source.GetField(nameOrGuid, BindingFlags.Public | BindingFlags.Static);
                if (field?.GetValue(null) is AssetReference reference)
                {
                    guid = reference.AssetGUID;
                    return true;
                }
            }

            guid = nameOrGuid.Length == 32 && nameOrGuid.All(Uri.IsHexDigit) ? nameOrGuid : null;
            return guid != null;
        }

        internal static void Initialize()
        {
            GVarConditions.Changed += Refresh;
            GameEvents.OnEntityDied += OnEntityDied;
        }

        /// <summary>
        /// Reads every creature file and places the creatures still alive in this save. Runs on each world load.
        /// </summary>
        internal static void SpawnConfiguredCreatures()
        {
            DefinitionsById.Clear();
            SpawnedById.Clear();
            DeadGuids.Clear();
            string folder = GetFolderPath();
            Directory.CreateDirectory(folder);

            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    ExternalCreatureConfigFile? config = JsonSerializer.Deserialize<ExternalCreatureConfigFile>(File.ReadAllText(file), JsonOptions);
                    foreach (ExternalCreatureDefinition definition in config?.Creatures ?? [])
                    {
                        if (!string.IsNullOrWhiteSpace(definition.Id))
                        {
                            DefinitionsById[definition.Id] = definition;
                        }
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("[Creatures] Could not read " + file + ": " + e.Message);
                }
            }

            Refresh();
        }

        private static void Refresh()
        {
            foreach (ExternalCreatureDefinition definition in DefinitionsById.Values)
            {
                SpawnedById.TryGetValue(definition.Id, out LazyActor? spawned);
                bool isSpawned = spawned != null;
                bool wanted = definition.Enabled && GVarConditions.AreMetForPlacement(definition.Conditions);

                if (wanted && !isSpawned && IsAlive(GetActorGuid(definition.Id)))
                {
                    LazyActor? created = Spawn(definition);
                    if (created != null)
                    {
                        SpawnedById[definition.Id] = created;
                    }
                }
                else if (!wanted && isSpawned)
                {
                    ActorSpawnAccess.TryDespawn(spawned);
                    SpawnedById.Remove(definition.Id);
                }
            }
        }

        /// <summary>
        /// The save only learns of a death when the game saves, so deaths of this session are remembered here.
        /// </summary>
        private static bool IsAlive(string actorGuid)
            => !DeadGuids.Contains(actorGuid) && SavegameDataExtensions.IsActorAlive(actorGuid);

        private static void OnEntityDied(Entity entity)
        {
            Actor? actor = entity.TryCast<Actor>();
            GuidComponent? guid = actor != null ? actor.GetGuidComponent() : null;
            if (guid != null)
            {
                DeadGuids.Add(guid.GuidString);
            }
        }

        private static LazyActor? Spawn(ExternalCreatureDefinition definition)
        {
            string actorGuid = GetActorGuid(definition.Id);

            if (!TryResolveAsset(definition.Creature, out string? creatureGuid, typeof(AddressableAccess.Creatures), typeof(AddressableAccess.Bandits)))
            {
                MelonLogger.Warning($"[Creatures] '{definition.Id}': unknown creature '{definition.Creature}'.");
                return null;
            }

            string? entityInfoGuid = AddressableAccess.EntityInfos.EntityInfo_Default.AssetGUID;
            if (definition.EntityInfo != null && !TryResolveAsset(definition.EntityInfo, out entityInfoGuid, typeof(AddressableAccess.EntityInfos)))
            {
                MelonLogger.Warning($"[Creatures] '{definition.Id}': unknown entity info '{definition.EntityInfo}'.");
                return null;
            }

            return LazyActorCreator.CreateLazyActorCreature(new LazyActorCreator.LazyActorParams
            {
                AssetReference = new AssetReferenceGameObject(creatureGuid),
                EntityInfo = new AssetReference(entityInfoGuid),
                Position = new Vector2(definition.X, definition.Y),
                Guid = actorGuid
            });
        }
    }
}
