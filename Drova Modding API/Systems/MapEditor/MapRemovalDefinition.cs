namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// A vanilla object hidden by a map layout file, found by its scene and hierarchy path.
    /// </summary>
    public sealed class MapRemovalDefinition
    {
        /// <summary>
        /// Name of the scene holding the object, for example <c>Scene_Overworld_Main_1_2</c>.
        /// </summary>
        public string Scene { get; set; } = string.Empty;

        /// <summary>
        /// Path from the scene root, names joined by <c>/</c>.
        /// </summary>
        public string Path { get; set; } = string.Empty;
    }
}
