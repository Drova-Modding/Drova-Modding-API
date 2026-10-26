
namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// One journal line. It is added once, when its trigger is first met.
    /// </summary>
    public sealed class QuestEntryDefinition
    {
        /// <summary>
        /// Entry id, unique within the quest. Never rename it, the save stores it.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Localization key of the line, read from <see cref="QuestTopicDefinition.LocaPath"/>.
        /// </summary>
        public string LocaKey { get; set; } = string.Empty;

        /// <summary>
        /// Experience granted when the line is added. The game rounds it down to tens.
        /// </summary>
        public int Exp { get; set; }

        /// <summary>
        /// Adds the line when the quest state becomes this <c>QuestState</c> name, for example <c>IsRunning</c>.
        /// </summary>
        public string? OnState { get; set; }

        /// <summary>
        /// Adds the line when this bool of the quest's GVarList becomes true.
        /// </summary>
        public string? OnGBool { get; set; }
    }
}
