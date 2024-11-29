using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        /// Override this method to add custom save logic.
        /// </summary>
        /// <returns>The json converted string</returns>
        string Save();

        /// <summary>
        /// Called when the savegame is loaded.
        /// </summary>
        void Load(string result);
    }
}
