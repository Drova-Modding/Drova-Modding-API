
namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// Actors whose death sets a bool of the quest, like the game's own <c>GlobalObserverBhvr_SetGVarAfterActorDead</c>.
    /// Actors are tracked by guid, so they may wander anywhere and may die before the quest starts.
    /// </summary>
    public sealed class QuestKillDefinition
    {
        /// <summary>
        /// Id, unique within the quest.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The actors that must all be dead: ids of creatures from <c>CreaturePlacement</c> files, or vanilla actor guids.
        /// </summary>
        public List<string> Actors { get; set; } = [];

        /// <summary>
        /// Bool of the quest's GVarList set to true once every actor is dead.
        /// </summary>
        public string SetGBool { get; set; } = string.Empty;
    }
}
