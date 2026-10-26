namespace Drova_Modding_API.Systems.Items
{
    /// <summary>
    /// Root of a custom item file.
    /// </summary>
    public sealed class CustomItemConfigFile
    {
        /// <summary>
        /// File format version.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The items in this file.
        /// </summary>
        public List<CustomItemDefinition> Items { get; set; } = [];
    }
}
