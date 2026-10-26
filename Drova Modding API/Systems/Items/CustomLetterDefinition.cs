namespace Drova_Modding_API.Systems.Items
{
    /// <summary>
    /// The text of a custom letter. The item's base must be a vanilla letter, whose paper and layout it keeps.
    /// </summary>
    public sealed class CustomLetterDefinition
    {
        /// <summary>
        /// Localization key of the letter's text, in the item's <see cref="CustomItemDefinition.LocaPath"/>.
        /// </summary>
        public string TextKey { get; set; } = string.Empty;

        /// <summary>
        /// Localization key of the letter's title in the journal. Defaults to the item's name, <c>&lt;ReadableId&gt;_name</c>.
        /// </summary>
        public string? TitleKey { get; set; }
    }
}
