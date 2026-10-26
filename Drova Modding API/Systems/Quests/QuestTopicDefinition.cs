
namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// The journal topic of a quest.
    /// </summary>
    public sealed class QuestTopicDefinition
    {
        /// <summary>
        /// Name of a vanilla topic such as <c>Topic_HuntTheRippers</c>. When set, the entries are added to it
        /// and <see cref="LocaKey"/> and <see cref="Group"/> are ignored.
        /// </summary>
        public string? Vanilla { get; set; }

        /// <summary>
        /// Localization file name all texts of this quest are read from, usually the mod name.
        /// </summary>
        public string LocaPath { get; set; } = string.Empty;

        /// <summary>
        /// Localization key of the quest title.
        /// </summary>
        public string LocaKey { get; set; } = string.Empty;

        /// <summary>
        /// Journal group the topic is sorted into, for example <c>TopicGroup_World</c>.
        /// </summary>
        public string Group { get; set; } = "TopicGroup_Unsorted";
    }
}
