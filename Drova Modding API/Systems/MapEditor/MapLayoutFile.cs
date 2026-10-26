namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// Root of a map layout file in <c>Mods/Modding_API/MapEditor/*.json</c>.
    /// </summary>
    public sealed class MapLayoutFile
    {
        /// <summary>
        /// File format version.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The props in this file.
        /// </summary>
        public List<MapObjectDefinition> Objects { get; set; } = [];

        /// <summary>
        /// Vanilla objects this file hides.
        /// </summary>
        public List<MapRemovalDefinition> Removed { get; set; } = [];

        /// <summary>
        /// New walkable areas this file adds.
        /// </summary>
        public List<MapAreaDefinition> Areas { get; set; } = [];
    }
}
