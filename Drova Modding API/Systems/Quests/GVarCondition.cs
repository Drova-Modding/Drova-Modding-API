namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// A game variable compared to a value, the condition placements and areas wait for.
    /// </summary>
    public sealed class GVarCondition
    {
        /// <summary>
        /// Name of the GVarList, custom or vanilla.
        /// </summary>
        public string GVarList { get; set; } = string.Empty;

        /// <summary>
        /// Id of the variable, <c>QuestState</c> for a quest list's state.
        /// </summary>
        public string Var { get; set; } = string.Empty;

        /// <summary>
        /// <c>true</c>/<c>false</c>, a <c>QuestState</c> name such as <c>IsRunning</c>, or a number. A leading <c>!</c> negates.
        /// </summary>
        public string Value { get; set; } = "true";
    }
}
