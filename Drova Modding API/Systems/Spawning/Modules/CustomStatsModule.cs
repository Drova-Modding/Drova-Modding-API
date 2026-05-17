using Drova_Modding_API.GlobalFields;
using Il2CppDrova;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Applies custom stat overrides directly on <see cref="AttributeStats"/>.
    /// Supports base stat overrides via <see cref="GenericStatDesc"/> or readable id (scriptable object name),
    /// armor value overrides, and exp-related fields.
    /// </summary>
    public class CustomStatsModule : INpcModule
    {
        private static readonly Dictionary<string, GenericStatDesc> CachedStatDescsByReadableId = new(StringComparer.Ordinal);
        private static bool _statDescCacheInitialized;

        private readonly Dictionary<GenericStatDesc, float> _baseStatOverrides = new();
        private readonly Dictionary<string, float> _baseStatOverridesByReadableId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _armorOverrides = new(StringComparer.Ordinal);

        private int? _expOnBrawl;
        private int? _expOnDead;
        private bool _giveXp = true;

        /// <summary>
        /// Sets a base stat override by descriptor.
        /// </summary>
        public CustomStatsModule WithBaseStat(GenericStatDesc statDesc, float value)
        {
            _baseStatOverrides[statDesc] = value;
            return this;
        }

        /// <summary>
        /// Sets a base stat override by readable id (the scriptable object name).
        /// </summary>
        public CustomStatsModule WithBaseStat(string readableId, float value)
        {
            if (string.IsNullOrWhiteSpace(readableId)) return this;
            _baseStatOverridesByReadableId[readableId] = value;
            return this;
        }

        /// <summary>
        /// Sets an armor override value for one of the supported armor stats in <see cref="StatReadableIds.Armor"/>.
        /// </summary>
        public CustomStatsModule WithArmor(string armorReadableId, float value)
        {
            if (!IsSupportedArmorReadableId(armorReadableId))
            {
                MelonLogger.Warning($"Unsupported armor readable id '{armorReadableId}'. Use values from {nameof(StatReadableIds)}.{nameof(StatReadableIds.Armor)} only.");
                return this;
            }

            _armorOverrides[armorReadableId] = value;
            return this;
        }

        /// <summary>
        /// Sets physical armor.
        /// </summary>
        public CustomStatsModule WithPhysicalArmor(float value)
            => WithArmor(StatReadableIds.Armor.Physical, value);

        /// <summary>
        /// Sets magical armor.
        /// </summary>
        public CustomStatsModule WithMagicalArmor(float value)
            => WithArmor(StatReadableIds.Armor.Magical, value);

        /// <summary>
        /// Sets experience gained from brawls.
        /// </summary>
        public CustomStatsModule WithExpOnBrawl(int value)
        {
            _expOnBrawl = value;
            return this;
        }

        /// <summary>
        /// Sets experience gained on death.
        /// </summary>
        public CustomStatsModule WithExpOnDead(int value)
        {
            _expOnDead = value;
            return this;
        }
        
        /// <summary>
        /// Sets whether the NPC should give experience at all. Default is true.
        /// </summary>
        /// <param name="giveXp"></param>
        /// <returns></returns>
        public CustomStatsModule WithGiveXp(bool giveXp)
        {
            _giveXp = giveXp;
            return this;
        }

        /// <summary>
        /// Returns a read-only map of all known readable ids for <see cref="GenericStatDesc"/> assets.
        /// Key and value are both the scriptable object name.
        /// </summary>
        public static IReadOnlyDictionary<string, string> GetKnownReadableIds()
        {
            EnsureStatDescCache();
            return CachedStatDescsByReadableId.Keys.ToDictionary(static key => key, static key => key);
        }

        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            var attributeStats = context.GetComponentInChildren<AttributeStats>();
            if (attributeStats == null) return;

            if (attributeStats._attributeMap != null)
            {
                foreach (var readableOverride in _baseStatOverridesByReadableId)
                {
                    if (!TryResolveStatDesc(readableOverride.Key, out var statDesc))
                    {
                        MelonLogger.Warning($"Could not resolve GenericStatDesc readable id '{readableOverride.Key}'.");
                        continue;
                    }

                    _baseStatOverrides[statDesc] = readableOverride.Value;
                }

                foreach (var statOverride in _baseStatOverrides)
                {
                    if (!attributeStats._attributeMap.TryGetValue(statOverride.Key, out var genericStat) || genericStat == null)
                    {
                        MelonLogger.Warning($"Attribute map does not contain stat '{statOverride.Key.name}' on this actor.");
                        continue;
                    }

                    genericStat.SetBaseValue(statOverride.Value);
                }
            }

            if (_armorOverrides.Count > 0)
            {
                var armorList = attributeStats.GetArmorValues();
                if (armorList == null)
                {
                    MelonLogger.Warning("Attribute armor values were null; skipping armor overrides.");
                }
                else
                {
                    foreach (var armorOverride in _armorOverrides)
                    {
                        if (!TryResolveArmorStatDesc(armorOverride.Key, out var armorStatDesc))
                        {
                            MelonLogger.Warning($"Could not resolve armor readable id '{armorOverride.Key}'.");
                            continue;
                        }

                        var updatedExistingArmorValue = false;
                        for (var i = 0; i < armorList.Count; i++)
                        {
                            var armorValue = armorList[i];
                            if (armorValue == null || armorValue.Stat != armorStatDesc) continue;

                            armorValue.Value = armorOverride.Value;
                            armorList[i] = armorValue;
                            updatedExistingArmorValue = true;
                            break;
                        }

                        if (!updatedExistingArmorValue)
                        {
                            MelonLogger.Warning($"Could not find existing armor entry for readable id '{armorOverride.Key}' on this actor.");
                        }
                    }

                    attributeStats.SetArmorValues(armorList);
                }
            }

            if (_expOnBrawl.HasValue) attributeStats.SetBrawlExp(_expOnBrawl.Value);
            if (_expOnDead.HasValue) attributeStats.SetExpOnDead(_expOnDead.Value);
            attributeStats._givesNoExp = !_giveXp;
        }

        private static bool TryResolveStatDesc(string readableId, out GenericStatDesc statDesc)
        {
            EnsureStatDescCache();
            return CachedStatDescsByReadableId.TryGetValue(readableId, out statDesc);
        }

        private static bool TryResolveArmorStatDesc(string readableId, out ResistanceStatDesc statDesc)
        {
            statDesc = null!;
            if (!TryResolveStatDesc(readableId, out var genericStatDesc)) return false;

            var resistanceStatDesc = genericStatDesc.TryCast<ResistanceStatDesc>();
            if (resistanceStatDesc == null) return false;

            statDesc = resistanceStatDesc;
            return true;
        }

        private static void EnsureStatDescCache()
        {
            if (_statDescCacheInitialized) return;

            _statDescCacheInitialized = true;
            var allStats = Resources.FindObjectsOfTypeAll<GenericStatDesc>();
            for (var i = 0; i < allStats.Length; i++)
            {
                var statDesc = allStats[i];
                if (statDesc == null || string.IsNullOrWhiteSpace(statDesc.name)) continue;
                CachedStatDescsByReadableId[statDesc.name] = statDesc;
            }
        }


        private static bool IsSupportedArmorReadableId(string? readableId)
            => readableId == StatReadableIds.Armor.Physical || readableId == StatReadableIds.Armor.Magical;
    }
}