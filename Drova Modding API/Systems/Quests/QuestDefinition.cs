
namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// One quest. Its state lives in a quest GVarList, so dialogues read and write it like any vanilla quest.
    /// </summary>
    public sealed class QuestDefinition
    {
        /// <summary>
        /// Unique quest id. It is part of every journal and actor identity the save stores, so never rename it.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Name of the quest GVarList holding the <c>QuestState</c> and the quest's bools.
        /// A custom list comes from a <c>GlobalVars</c> file. A vanilla list name extends that quest.
        /// </summary>
        public string GVarList { get; set; } = string.Empty;

        /// <summary>
        /// The journal topic, shown as the quest title.
        /// </summary>
        public QuestTopicDefinition Topic { get; set; } = new();

        /// <summary>
        /// Journal lines added as the quest progresses.
        /// </summary>
        public List<QuestEntryDefinition> Entries { get; set; } = [];

        /// <summary>
        /// Actors whose death sets a bool of the quest.
        /// </summary>
        public List<QuestKillDefinition> Kills { get; set; } = [];

        /// <summary>
        /// Places whose reaching sets a bool of the quest.
        /// </summary>
        public List<QuestAreaDefinition> Areas { get; set; } = [];
    }
}
