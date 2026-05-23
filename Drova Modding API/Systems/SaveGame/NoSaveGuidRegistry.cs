namespace Drova_Modding_API.Systems.SaveGame
{
    /// <summary>
    /// A static registry that allows modders to register Actor GUIDs (instance keys)
    /// that should be excluded from the save/load cycle entirely.
    ///
    /// The <c>InstanceKey</c> of an <c>ASaveRoot</c> is the GUID string returned by
    /// <c>GetGameObjectInstanceKey()</c>. Registering a GUID here prevents both
    /// <c>SaveState</c> and <c>LoadState</c>/<c>LoadStateAsync</c> from running for
    /// that actor, and causes <c>GetGameObjectData</c> to return <c>null</c>.
    ///
    /// Usage examples:
    /// <code>
    /// // Register a GUID so the actor is never saved or loaded
    /// NoSaveGuidRegistry.Register("550e8400-e29b-41d4-a716-446655440000");
    ///
    /// // Unregister when the actor should be saved again
    /// NoSaveGuidRegistry.Unregister("550e8400-e29b-41d4-a716-446655440000");
    ///
    /// // Check programmatically
    /// if (NoSaveGuidRegistry.IsRegistered(guid)) { ... }
    /// </code>
    /// </summary>
    public static class NoSaveGuidRegistry
    {
        private static readonly HashSet<string> Guids = [];

        /// <summary>
        /// Registers a GUID so that any save whose instance key matches
        /// will be skipped during save and load.
        /// </summary>
        /// <param name="guid">The instance key (GUID) of the actor to exclude.</param>
        public static void Register(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
                Guids.Add(guid);
        }

        /// <summary>
        /// Removes a previously registered GUID, allowing it to participate in
        /// save/load again.
        /// </summary>
        /// <param name="guid">The instance key (GUID) to re-enable.</param>
        public static void Unregister(string guid)
        {
            Guids.Remove(guid);
        }

        /// <summary>
        /// Returns <c>true</c> if the given GUID is currently registered to be
        /// excluded from saving and loading.
        /// </summary>
        /// <param name="guid">The instance key (GUID) to check.</param>
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

