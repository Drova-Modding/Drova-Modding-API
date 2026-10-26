namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// A new walkable area of a map layout file: a rectangle that gets its own navmesh graph, so NPCs and creatures find
    /// paths in it. Build it far from the map (for example around 80000, -80000) with stamps, blockers and props, and
    /// connect it with copied gates (<see cref="MapObjectDefinition.TargetX"/>).
    /// </summary>
    public sealed class MapAreaDefinition
    {
        /// <summary>
        /// Unique id across all layout files.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Center x.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Center y.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Width in world units.
        /// </summary>
        public float Width { get; set; } = 1600f;

        /// <summary>
        /// Height in world units.
        /// </summary>
        public float Height { get; set; } = 1600f;

        /// <summary>
        /// The game treats the player inside the area as indoors: no rain or snow, the indoor light.
        /// </summary>
        public bool Indoor { get; set; }
    }
}
