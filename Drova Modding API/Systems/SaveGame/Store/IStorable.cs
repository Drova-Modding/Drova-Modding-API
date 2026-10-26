namespace Drova_Modding_API.Systems.SaveGame.Store
{
    /// <summary>
    /// Base interface for storable objects.
    /// </summary>
    public interface IStorable
    {
        /**
        * The key for the savegame
        */
        string SaveGameKey { get; }

        /// <summary>
        /// Called when the savegame is saved.
        /// </summary>
        /// <returns>The JSON to store under <see cref="SaveGameKey"/>.</returns>
        string Save();

        /// <summary>
        /// Called when the savegame is loaded.
        /// </summary>
        void Load(string result);
    }
}
