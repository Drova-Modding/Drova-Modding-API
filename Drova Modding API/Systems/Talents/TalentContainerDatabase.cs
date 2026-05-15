using Il2CppDrova.Talent;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Talents
{
    /// <summary>
    /// Manages and caches TalentContainer assets, grouping them by category.
    /// The database is populated when entering the MainMenu.
    /// </summary>
    public static class TalentContainerDatabase
    {
        private static Dictionary<string, List<TalentContainer>>? _groupedDatabase;
        private static Dictionary<string, TalentContainer>? _talentCache;

        /// <summary>
        /// Initializes the TalentContainer database from the database container.
        /// Call this once when entering the MainMenu.
        /// </summary>
        public static void InitializeDatabase()
        {
            if (_groupedDatabase != null && _talentCache != null) return;

            var talentsInDatabase = Resources.FindObjectsOfTypeAll<TalentContainer>();
            
            // Build grouped dictionary by prefix
            _groupedDatabase = talentsInDatabase
                .GroupBy(tc => ExtractCategory(tc.name))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build flat cache for fast lookups
            _talentCache = talentsInDatabase.ToDictionary(tc => tc.name);

            MelonLogger.Msg($"[TalentContainerDatabase] Database initialized with {talentsInDatabase.Length} talents " +
                      $"across {_groupedDatabase.Count} categories.");
        }

        /// <summary>
        /// Gets all TalentContainers grouped by their name category.
        /// </summary>
        public static Dictionary<string, List<TalentContainer>> GetGroupedTalents()
        {
            if (_groupedDatabase == null)
            {
                MelonLogger.Warning("[TalentContainerDatabase] Database not initialized. Call InitializeDatabase() first.");
            }

            return _groupedDatabase ?? new Dictionary<string, List<TalentContainer>>();
        }

        /// <summary>
        /// Gets a specific TalentContainer by name from the database cache.
        /// </summary>
        public static bool TryGetTalent(string name, out TalentContainer? talent)
        {
            if (_talentCache == null)
            {
                MelonLogger.Warning("[TalentContainerDatabase] Database not initialized. Call InitializeDatabase() first.");
            }

            talent = null;
            return _talentCache != null && _talentCache.TryGetValue(name, out talent);
        }

        /// <summary>
        /// Gets all talents in a specific category (e.g., "Talent", "TalentBhvr", "Test").
        /// </summary>
        public static List<TalentContainer> GetTalentsByCategory(string category)
        {
            if (_groupedDatabase == null)
            {
                Debug.LogWarning("[TalentContainerDatabase] Database not initialized. Call InitializeDatabase() first.");
            }

            return _groupedDatabase?.TryGetValue(category, out var talents) == true 
                ? talents 
                : new List<TalentContainer>();
        }

        /// <summary>
        /// Extracts the category prefix from a talent name.
        /// </summary>
        private static string ExtractCategory(string talentName)
        {
            if (talentName.StartsWith("TalentBhvr_")) return "TalentBhvr";
            if (talentName.StartsWith("Talent_")) return "Talent";
            return talentName.StartsWith("Test_") ? "Test" : "Other";
        }

        /// <summary>
        /// Clears the cached database.
        /// </summary>
        public static void ClearDatabase()
        {
            _groupedDatabase = null;
            _talentCache = null;
        }
    }
}

