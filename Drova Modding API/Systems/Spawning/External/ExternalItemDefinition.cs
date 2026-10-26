using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Systems.Quests;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// One item lying in the world, from <c>Mods/Modding_API/ItemPlacement/*.json</c>.
    /// </summary>
    public sealed class ExternalItemDefinition
    {
        /// <summary>
        /// Unique id. The pickup's save guid is derived from it, so never rename a placed item.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Readable id of the item, see <see cref="ItemReadableIds"/>.
        /// </summary>
        public string Item { get; set; } = string.Empty;

        /// <summary>
        /// How many the pickup gives.
        /// </summary>
        public int Amount { get; set; } = 1;

        /// <summary>
        /// Position x.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Position y.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Disabled items are not placed.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The item only lies there while all of these hold, and is taken away again when they stop holding.
        /// </summary>
        public List<GVarCondition>? Conditions { get; set; }

        /// <summary>
        /// GVarList of <see cref="TakenGBool"/>.
        /// </summary>
        public string? TakenGVarList { get; set; }

        /// <summary>
        /// Bool set to true when the player takes the item, for quests about finding or destroying something.
        /// </summary>
        public string? TakenGBool { get; set; }
    }
}
