namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Where a mod declares cutscenes that belong to the world rather than to one player.
    ///
    /// A coop session replays a declared cutscene on every machine, so a story transition that moves
    /// one player moves all of them. Undeclared cutscenes stay where they were triggered, which is
    /// right for almost all of them: dialogue staging like camera focuses, black blends and stage
    /// teleports would hijack the screen of a player who is not in the conversation.
    ///
    /// Names are the <c>CS_CutsceneData</c> asset names. Declare during melon initialization.
    /// </summary>
    public static class CutsceneSyncAccess
    {
        private static readonly Dictionary<string, string> _shared = new(StringComparer.Ordinal);

        /// <summary>
        /// Declares one cutscene as shared by every player in a session.
        ///
        /// The addressable guid is optional but load-bearing when the far machine has not streamed
        /// the asset: cutscene data rides in with the dialogue graph that uses it, so a player far
        /// from the speaker may hold nothing to replay. With the guid the asset can be loaded on
        /// demand instead.
        /// </summary>
        /// <param name="cutsceneDataName">The cutscene data asset's name.</param>
        /// <param name="addressableGuid">The asset's addressable guid, when known.</param>
        public static void MarkShared(string cutsceneDataName, string addressableGuid = null)
        {
            if (string.IsNullOrEmpty(cutsceneDataName)) return;

            _shared[cutsceneDataName] = addressableGuid ?? string.Empty;
        }

        /// <summary>
        /// Whether a cutscene has been declared shared.
        /// </summary>
        /// <param name="cutsceneDataName">The cutscene data asset's name.</param>
        public static bool IsShared(string cutsceneDataName)
        {
            return !string.IsNullOrEmpty(cutsceneDataName) && _shared.ContainsKey(cutsceneDataName);
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
            if (!_shared.TryGetValue(cutsceneDataName, out string stored)) return false;
            if (string.IsNullOrEmpty(stored)) return false;

            addressableGuid = stored;
            return true;
        }
    }
}
