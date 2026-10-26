
namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// Root of a quest file in <c>Mods/Modding_API/Quests/*.json</c>.
    /// </summary>
    public sealed class QuestConfigFile
    {
        /// <summary>
        /// File format version.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The quests in this file.
        /// </summary>
        public List<QuestDefinition> Quests { get; set; } = [];
    }
}
