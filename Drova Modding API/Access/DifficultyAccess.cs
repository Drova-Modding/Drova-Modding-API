using Il2CppDrova;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Access to the game mode (difficulty) of the currently loaded savegame.
    /// The game has no numeric difficulty multipliers: it only distinguishes insane from non-insane modes
    /// and selects data-authored insane variants (spawn data, AI graphs, stats) based on that.
    /// </summary>
    public static class DifficultyAccess
    {
        /// <summary>
        /// Tries to get the game mode of the currently loaded savegame.
        /// </summary>
        /// <param name="gameMode">The current game mode, or <see cref="GameMode.Default"/> when unavailable</param>
        /// <returns>False while the game is still bootstrapping or no savegame is loaded</returns>
        public static bool TryGetGameMode(out GameMode gameMode)
        {
            if (SavegameGameHandler.TryGet(out SavegameGameHandler handler))
            {
                gameMode = handler.GetGameMode();
                return true;
            }
            gameMode = GameMode.Default;
            return false;
        }

        /// <summary>
        /// Whether the current savegame runs in one of the insane game modes.
        /// Returns false while the game is still bootstrapping.
        /// </summary>
        public static bool IsInsaneMode()
        {
            return SavegameGameHandler.TryGet(out SavegameGameHandler handler) && handler.IsInsaneGameMode();
        }
    }
}
