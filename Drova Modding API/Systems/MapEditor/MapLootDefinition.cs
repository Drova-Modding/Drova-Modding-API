namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// One item stack a placed container starts with.
    /// </summary>
    public sealed class MapLootDefinition
    {
        /// <summary>
        /// Readable id of the item, for example <c>item_potion_health_1</c>.
        /// </summary>
        public string Item { get; set; } = string.Empty;

        /// <summary>
        /// Stack size.
        /// </summary>
        public int Amount { get; set; } = 1;
    }
}
