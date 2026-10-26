namespace Drova_Modding_API.Systems.ModdingUI
{
    /// <summary>
    /// A tool listed in the Modding Tools window.
    /// </summary>
    /// <param name="Id">Unique id. Registering the same id again replaces the tool.</param>
    /// <param name="Name">Button text.</param>
    /// <param name="Description">One line under the button.</param>
    /// <param name="Open">Opens the tool. The Modding Tools window closes first.</param>
    public sealed record ModdingTool(string Id, string Name, string Description, Action Open)
    {
        /// <summary>
        /// Whether the tool can be opened right now, for example only while a world is loaded. Null for always.
        /// </summary>
        public Func<bool>? IsAvailable { get; init; }
    }
}
