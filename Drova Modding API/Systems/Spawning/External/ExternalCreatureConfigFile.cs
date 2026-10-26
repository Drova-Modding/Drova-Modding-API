namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Root of a creature placement file.
    /// </summary>
    public sealed class ExternalCreatureConfigFile
    {
        /// <summary>
        /// File format version.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The creatures in this file.
        /// </summary>
        public List<ExternalCreatureDefinition> Creatures { get; set; } = [];
    }
}
