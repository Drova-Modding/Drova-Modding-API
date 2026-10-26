namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Root of an item placement file.
    /// </summary>
    public sealed class ExternalItemConfigFile
    {
        /// <summary>
        /// File format version.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The items in this file.
        /// </summary>
        public List<ExternalItemDefinition> Items { get; set; } = [];
    }
}
