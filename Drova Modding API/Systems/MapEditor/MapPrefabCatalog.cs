namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// The prefabs the map editor offers: every addressable environment prefab of the game, see the generated table.
    /// Only addressable prefabs are safe to place: art that lives in a chunk's scene bundle is destroyed with the chunk.
    /// </summary>
    internal static partial class MapPrefabCatalog
    {
        private const string EnvironmentRoot = "Environment/";

        private static Dictionary<string, string>? _pathByGuid;

        internal static string NameOf(string path) => Path.GetFileNameWithoutExtension(path);

        /// <summary>
        /// The folder below <c>Environment/</c>, for example <c>Outerior/Natural/Bushes</c>.
        /// </summary>
        internal static string CategoryOf(string path)
        {
            int start = path.IndexOf(EnvironmentRoot, StringComparison.Ordinal);
            string relative = start >= 0 ? path[(start + EnvironmentRoot.Length)..] : path;
            int end = relative.LastIndexOf('/');
            return end > 0 ? relative[..end] : string.Empty;
        }

        /// <summary>
        /// The asset path of a prefab guid, or null when the catalog does not know it.
        /// </summary>
        internal static string? PathOf(string guid)
        {
            _pathByGuid ??= Prefabs.ToDictionary(prefab => prefab.Guid, prefab => prefab.Path, StringComparer.OrdinalIgnoreCase);
            return _pathByGuid.TryGetValue(guid, out string? path) ? path : null;
        }
    }
}
