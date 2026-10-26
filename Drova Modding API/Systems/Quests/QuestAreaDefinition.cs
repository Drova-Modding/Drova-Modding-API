
namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// A place the player has to reach. It is the game's own trigger volume (<c>HitReceiveHandler</c> with
    /// <c>HitReceiveBhvr_SetGVars</c>), so it sets the bool exactly like the vanilla quest areas do.
    /// </summary>
    public sealed class QuestAreaDefinition
    {
        /// <summary>
        /// Id, unique within the quest.
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
        /// Radius in world units.
        /// </summary>
        public float Radius { get; set; } = 100f;

        /// <summary>
        /// Bool of the quest's GVarList set to true when the player enters.
        /// </summary>
        public string SetGBool { get; set; } = string.Empty;

        /// <summary>
        /// The area only exists while all of these hold, for example the quest state being <c>IsRunning</c>.
        /// </summary>
        public List<GVarCondition>? Conditions { get; set; }
    }
}
