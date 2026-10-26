namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Declares cutscenes that belong to the world rather than to one player. A co-op session replays a declared
    /// cutscene on every machine. Undeclared ones, like dialogue staging, stay with the player who triggered them.
    /// Names are <c>CS_CutsceneData</c> asset names. Declare during melon initialization.
    /// </summary>
    public static class CutsceneSyncAccess
    {
        private static readonly Dictionary<string, string> SharedCutscenes = new(StringComparer.Ordinal);

        /// <summary>
        /// Declares one cutscene as shared by every player in a session. The addressable guid lets a machine far from
        /// the speaker, which has not streamed the cutscene data, load it on demand.
        /// </summary>
        /// <param name="cutsceneDataName">The cutscene data asset's name.</param>
        /// <param name="addressableGuid">The asset's addressable guid, when known.</param>
        public static void MarkShared(string cutsceneDataName, string? addressableGuid = null)
        {
            if (string.IsNullOrEmpty(cutsceneDataName)) return;

            SharedCutscenes[cutsceneDataName] = addressableGuid ?? string.Empty;
        }

        /// <summary>
        /// Whether a cutscene has been declared shared.
        /// </summary>
        /// <param name="cutsceneDataName">The cutscene data asset's name.</param>
        public static bool IsShared(string cutsceneDataName)
        {
            return !string.IsNullOrEmpty(cutsceneDataName) && SharedCutscenes.ContainsKey(cutsceneDataName);
        }

        /// <summary>
        /// The addressable guid a shared cutscene was declared with.
        /// </summary>
        /// <param name="cutsceneDataName">The cutscene data asset's name.</param>
        /// <param name="addressableGuid">The guid.</param>
        /// <returns>False when the cutscene is not shared or was declared without one.</returns>
        public static bool TryGetAddressableGuid(string cutsceneDataName, out string addressableGuid)
        {
            addressableGuid = string.Empty;

            if (string.IsNullOrEmpty(cutsceneDataName)) return false;
            if (!SharedCutscenes.TryGetValue(cutsceneDataName, out string stored)) return false;
            if (string.IsNullOrEmpty(stored)) return false;

            addressableGuid = stored;
            return true;
        }
    }
}
