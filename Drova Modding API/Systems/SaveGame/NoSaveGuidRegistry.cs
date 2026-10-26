namespace Drova_Modding_API.Systems.SaveGame
{
    /// <summary>
    /// Actor GUIDs (the <c>InstanceKey</c> of an <c>ASaveRoot</c>) excluded from saving and loading entirely: their
    /// <c>SaveState</c> and <c>LoadState</c> never run and <c>GetGameObjectData</c> returns null.
    /// </summary>
    public static class NoSaveGuidRegistry
    {
        private static readonly HashSet<string> Guids = [];

        /// <summary>
        /// Excludes the actor with this instance key from save and load.
        /// </summary>
        public static void Register(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
                Guids.Add(guid);
        }

        /// <summary>
        /// Lets a registered GUID take part in save and load again.
        /// </summary>
        public static void Unregister(string guid)
        {
            Guids.Remove(guid);
        }

        /// <summary>
        /// Whether the GUID is excluded from saving and loading.
        /// </summary>
        public static bool IsRegistered(string guid)
        {
            return !string.IsNullOrEmpty(guid) && Guids.Contains(guid);
        }

        /// <summary>
        /// Clears all registered GUIDs.
        /// </summary>
        public static void Clear() => Guids.Clear();
    }
}

