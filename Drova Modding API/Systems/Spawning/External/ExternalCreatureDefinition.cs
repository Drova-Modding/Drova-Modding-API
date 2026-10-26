using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Quests;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// One creature placed in the world from <c>Mods/Modding_API/CreaturePlacement/*.json</c>.
    /// </summary>
    public sealed class ExternalCreatureDefinition
    {
        /// <summary>
        /// Unique id. The actor guid is derived from it, so never rename a placed creature.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// A field name of <see cref="AddressableAccess.Creatures"/> or <see cref="AddressableAccess.Bandits"/>, or an addressable asset guid.
        /// </summary>
        public string Creature { get; set; } = string.Empty;

        /// <summary>
        /// A field name of <see cref="AddressableAccess.EntityInfos"/> or an asset guid. Defaults to <c>EntityInfo_Default</c>.
        /// </summary>
        public string? EntityInfo { get; set; }

        /// <summary>
        /// Position x.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Position y.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Disabled creatures are not placed.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The creature only exists while all of these hold, and leaves again when they stop holding.
        /// </summary>
        public List<GVarCondition>? Conditions { get; set; }
    }
}
