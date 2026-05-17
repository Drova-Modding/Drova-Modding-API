using Drova_Modding_API.Systems.Talents;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Talent;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Applies starting talents to an NPC spawn configuration.
    /// Talents can be added directly as containers or resolved from readable ids.
    /// </summary>
    public class TalentPresetModule : INpcModule
    {
        private readonly HashSet<TalentContainer> _talentContainers = new();
        private readonly HashSet<string> _readableIds = new();

        /// <summary>
        /// Adds a concrete talent container to this preset.
        /// </summary>
        /// <param name="talentContainer">The talent container to apply as a starting talent.</param>
        public TalentPresetModule With(TalentContainer talentContainer)
        {
            _talentContainers.Add(talentContainer);
            return this;
        }

        /// <summary>
        /// Adds a readable talent id that will be resolved when the module is applied.
        /// </summary>
        /// <param name="readableId">The readable talent id from the talent container database.</param>
        public TalentPresetModule With(string readableId)
        {
            if (string.IsNullOrWhiteSpace(readableId)) return this;
            _readableIds.Add(readableId);
            return this;
        }

        /// <summary>
        /// Resolves all configured talents and writes them into the actor equip preset.
        /// </summary>
        /// <param name="context">The spawning module context that provides access to NPC components.</param>
        public void Apply(ModuleContext context)
        {
            var inventory = context.GetComponentInChildren<Inventory_StartupEquipSettings>();
            if (inventory == null) return;

            if (inventory._equipPreset == null)
            {
                inventory._equipPreset = ScriptableObject.CreateInstance<ActorEquipPreset>();
            }

            foreach (var readableId in _readableIds)
            {
                var hasTalent = TalentContainerDatabase.TryGetTalent(readableId, out TalentContainer? talent);
                if (!hasTalent)
                {
                    MelonLogger.Warning($"Could not resolve talent readable id '{readableId}'.");
                    continue;
                }

                inventory._equipPreset._startTalents.Add(talent);
            }

            foreach (var talent in _talentContainers)
            {
                inventory._equipPreset._startTalents.Add(talent);
            }
        }
    }
}