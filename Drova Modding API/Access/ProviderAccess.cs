using System.Diagnostics.CodeAnalysis;
using Il2Cpp;
using Il2CppCustomFramework.Localization;
using Il2CppDrova;
using Il2CppDrova.Achievements;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.GUI;
using Il2CppDrova.Items;
using Il2CppDrova.Items.Stats;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.StatusEffects;
using Il2CppDrova.Utilities.LazyLoading;
using Il2CppDrova.Weather;

namespace Drova_Modding_API.Access
{
    /// <summary>
    ///  Easy access to the DrovaResourceProvider and its resources
    /// </summary>
    public static class ProviderAccess
    {
        private static DrovaResourceProvider? _drovaResourceProvider;
        private static GameManager? _gameManager;
        private static StatContainer? _cachedStateContainer;

        /// <summary>
        /// Access to the DrovaResourceProvider
        /// </summary>
        public static DrovaResourceProvider? GetDrovaResourceProvider()
        {
            if (_drovaResourceProvider != null)
                return _drovaResourceProvider;
            _drovaResourceProvider = UnityEngine.Object.FindAnyObjectByType<DrovaResourceProvider>();
            return _drovaResourceProvider;
        }

        /// <summary>
        /// Access to the GVarDatabase. Null before the game has loaded its database.
        /// </summary>
        public static SubDatabase_GVars? GVarDatabase
        {
            get
            {
                GameDatabase? database = GetGameDatabase();
                return database != null ? database._gvarDatabase : null;
            }
        }

        /// <summary>
        /// Access to the RecipeDatabase. Null before the game has loaded its database.
        /// </summary>
        public static SubDatabase_Recipe? RecipeDatabase
        {
            get
            {
                GameDatabase? database = GetGameDatabase();
                return database != null ? database._recipeDatabase : null;
            }
        }

        /// <summary>
        /// Access to the ItemDatabase. Null before the game has loaded its database.
        /// </summary>
        public static SubDatabase_Item? ItemDatabase
        {
            get
            {
                GameDatabase? database = GetGameDatabase();
                return database != null ? database._itemDatabase : null;
            }
        }

        /// <summary>
        /// Access to the StatusEffectDatabase. Null before the game has loaded its database.
        /// </summary>
        public static SubDatabase_StatusEffect? StatusEffectDatabase
        {
            get
            {
                GameDatabase? database = GetGameDatabase();
                return database != null ? database._statusEffectDatabase : null;
            }
        }

        /// <summary>
        /// Contains BHV Trees
        /// </summary>
        public static AIDatabase? GetAIDatabase()
        {
            return AIDatabase.Instance;
        }

        /// <summary>
        ///  Contains fading materials
        ///  </summary>
        public static FadingMaterials? GetFadingMaterials()
        {
            return FadingMaterials.Instance;
        }

        /// <summary>
        /// Contains Localized Strings for Stats
        /// </summary>
        public static GenericStatLoca? GetGenericStatLoca()
        {
            DrovaResourceProvider? provider = GetDrovaResourceProvider();
            return provider != null ? provider._handler[2].Cast<GenericStatLoca>() : null;
        }

        /// <summary>
        /// Contains all global stats for the game <see cref="GenericStatDesc"/>
        /// </summary>
        public static StatContainer? GetGlobalStatsContainer()
        {
            if (_cachedStateContainer != null) return _cachedStateContainer;

            DrovaResourceProvider? provider = GetDrovaResourceProvider();
            if (provider == null) return null;

            _cachedStateContainer = provider._handler[3].Cast<StatContainer>();
            return _cachedStateContainer;
        }

        /// <summary>
        /// Localization Database for the game, for better access use <see cref="LocalizationAccess"/>
        /// </summary>
        public static LocalizationDB? GetLocalizationDB()
        {
            return LocalizationDB.Instance;
        }

        /// <summary>
        /// Contains Water Animation speed
        /// </summary>
        public static TilemapDatabase? GetTilemapDatabase()
        {
            return TilemapDatabase.Instance;
        }

        /// <summary>
        ///  Tooltip Builder for the game <see cref="TooltipElement"/>
        /// </summary>
        public static TooltipBuilder? GetTooltipBuilder()
        {
            return TooltipBuilder.Instance;
        }

        /// <summary>
        /// Provide access to further Databases, like items, effects, recipes, etc. Null before the game has loaded it.
        /// </summary>
        public static GameDatabase? GetGameDatabase()
        {
            return GameDatabase.Instance;
        }

        /// <summary>
        /// Provides access to all GameHandlers for easier access use the method Get[HandlerName]GameHandler
        /// </summary>
        public static ScriptableGameHandlerDatabase? GetScriptableGameHandlerDatabase()
        {
            return ScriptableGameHandlerDatabase.Instance;
        }

        /// <summary>
        ///  Access to FSM and graphs
        /// </summary>
        public static FactoryAIGameHandler? GetFactoryAIGameHandler()
        {
            return FactoryAIGameHandler.TryGet();
        }

        /// <summary>
        ///  Access to the Achievement System
        /// </summary>
        public static AchievementGameHandler? GetAchievementGameHandler()
        {

            AchievementGameHandler.TryGet(out AchievementGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Provides access to PlayerCraftedEvent and PlayerUsedConsumable Event
        /// </summary>
        public static AnalyticsGameHandler? GetAnalyticsGameHandler()
        {
            AnalyticsGameHandler.TryGet(out AnalyticsGameHandler handler);
            return handler;
        }

        /// <summary>
        /// SFX Access and Play Sounds for Actors
        /// </summary>
        public static AudioGameHandler? GetAudioGameHandler()
        {
            AudioGameHandler.TryGet(out AudioGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Camera Target and Offset
        /// </summary>
        public static CameraGameHandler? GetCameraGameHandler()
        {
            CameraGameHandler.TryGet(out CameraGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Access to all Cheat related stuff, like register spawnable Actors/Items and other
        /// </summary>
        public static CheatGameHandler? GetCheatGameHandler()
        {
            CheatGameHandler.TryGet(out CheatGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Access to <see cref="ConfigFileBridgeOptions"/>
        /// </summary>
        public static ConfigGameHandler? GetConfigGameHandler()
        {
            ConfigGameHandler.TryGet(out ConfigGameHandler handler);
            return handler;

        }

        /// <summary>
        /// Create reports and get the Folder of crashes
        /// </summary>
        public static CrashGameHandler? GetCrashGameHandler()
        {
            ScriptableGameHandlerDatabase? database = GetScriptableGameHandlerDatabase();
            return database != null ? database._assets[7].Cast<CrashGameHandler>() : null;
        }

        /// <summary>
        /// Active dialogues and access to the Dialogue System
        /// </summary>
        public static DialogueSystemGameHandler? GetDialogueSystemGameHandler()
        {
            DialogueSystemGameHandler.TryGet(out DialogueSystemGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Access to the player object and events for entities spawning, despawning, etc. and entities in scene
        /// </summary>
        public static EntityGameHandler? GetEntityGameHandler()
        {
            EntityGameHandler.TryGet(out EntityGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Equipment slots, talents, status effects
        /// </summary>
        public static GameplaySettingsHandler? GetGameplaySettingsHandler()
        {
            GameplaySettingsHandler.TryGet(out GameplaySettingsHandler handler);
            return handler;
        }

        /// <summary>
        /// Access to common items, like torches, money
        /// Access to the perceptionColor and other stuff
        /// </summary>
        public static GlobalAssetsGameHandler? GetGlobalAssetsGameHandler()
        {
            GlobalAssetsGameHandler.TryGet(out GlobalAssetsGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Access to GUI Windows and their prefabs
        /// </summary>
        public static GUIGameHandler? GetGUIGameHandler()
        {
            GUIGameHandler.TryGet(out GUIGameHandler handler);
            return handler;
        }
        /// <summary>
        /// Provides Position and access to their Databases
        /// </summary>
        public static MapGameHandler? GetMapGameHandler()
        {
            MapGameHandler.TryGet(out MapGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Common ObjectPool Pattern
        /// </summary>
        public static ObjectPoolerGameHandler? GetObjectPoolerGameHandler()
        {
            ObjectPoolerGameHandler.TryGet(out ObjectPoolerGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Tracks active Quests
        /// </summary>
        public static QuestGameHandler? GetQuestGameHandler()
        {
            QuestGameHandler.TryGet(out QuestGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Events, Data related to SaveGames and the current Savegame
        /// </summary>
        public static SavegameGameHandler? GetSaveGameHandler()
        {
            SavegameGameHandler.TryGet(out SavegameGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Fading, Streaming, Events for Scene Changes and access if the player is teleporting
        /// </summary>
        public static SceneGameHandler? GetSceneGameHandler()
        {
            SceneGameHandler.TryGet(out SceneGameHandler handler);
            return handler;
        }

        /// <summary>
        /// Tutorial Events and access to the Tutorial System
        /// </summary>
        public static TutorialManager? GetTutorialManager()
        {
            TutorialManager.TryGet(out TutorialManager handler);
            return handler;
        }

        /// <summary>
        /// Get Game Manager 
        /// </summary>
        public static bool TryGetGameManager([NotNullWhen(true)] out GameManager? gameManager)
        {
            if (_gameManager)
            {
                gameManager = _gameManager;
                return true;
            }
            GameManager? gameManagerObject = UnityEngine.Object.FindObjectOfType<GameManager>();
            gameManager = gameManagerObject;
            if (gameManagerObject != null)
            {
                _gameManager = gameManagerObject;
                return true;
            }
            return false;
        }

        /// <summary>
        /// GameOverManager
        /// </summary>
        public static bool TryGetGameOverGameHandler([NotNullWhen(true)] out GameOverGameHandler? gameOverGameHandler)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                gameOverGameHandler = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("GameOverGameHandler", out IGameHandler handler))
            {
                gameOverGameHandler = handler.Cast<GameOverGameHandler>();
                return true;
            }

            gameOverGameHandler = null;
            return false;
        }

        /// <summary>
        /// Time scale
        /// </summary>
        public static bool TryGetGameStateGameHandler([NotNullWhen(true)] out GameStateGameHandler? gameStateGameHandler)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                gameStateGameHandler = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("GameStateGameHandler", out IGameHandler handler))
            {
                gameStateGameHandler = handler.Cast<GameStateGameHandler>();
                return true;
            }

            gameStateGameHandler = null;
            return false;
        }

        /// <summary>
        /// Access to journal, map collection, and crafting
        /// </summary>
        public static bool TryGetPlayerMetaDataGameHandler([NotNullWhen(true)] out PlayerMetaDataGameHandler? playerMetaDataGameHandler)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                playerMetaDataGameHandler = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("PlayerMetaDataGameHandler", out IGameHandler handler))
            {
                playerMetaDataGameHandler = handler.Cast<PlayerMetaDataGameHandler>();
                return true;
            }

            playerMetaDataGameHandler = null;
            return false;
        }

        /// <summary>
        /// weatherGameHandler Access to weather events and properties
        /// </summary>
        public static bool TryGetWeatherGameHandler([NotNullWhen(true)] out WeatherGameHandler? weatherGameHandler)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                weatherGameHandler = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("weatherGameHandler", out IGameHandler handler))
            {
                weatherGameHandler = handler.Cast<WeatherGameHandler>();
                return true;
            }

            weatherGameHandler = null;
            return false;
        }

        /// <summary>
        /// DaytimeGameHandler with events 
        /// </summary>
        public static bool TryGetDaytimeGameHandler([NotNullWhen(true)] out DaytimeGameHandler? daytimeGameHandler)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                daytimeGameHandler = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("DaytimeGameHandler", out IGameHandler handler))
            {
                daytimeGameHandler = handler.Cast<DaytimeGameHandler>();
                return true;
            }

            daytimeGameHandler = null;
            return false;
        }

        /// <summary>
        /// RootObjectHandler get root objects for scenes
        /// Possible values: "RoutineScene_Gameplay_Main", "RoutineScene_Creatures", "AIControllerScene_Creatures", "AIControllerScene_Actors", "RoutineScene_Actors"
        /// </summary>
        public static bool TryGetRootObjectHandler([NotNullWhen(true)] out RootObjectHandler? rootObjectHandler)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                rootObjectHandler = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("RootObjectHandler", out IGameHandler handler))
            {
                rootObjectHandler = handler.Cast<RootObjectHandler>();
                return true;
            }

            rootObjectHandler = null;
            return false;
        }

        /// <summary>
        /// LazyManager for other lazy Objects
        /// </summary>
        public static bool TryGetLazyManager([NotNullWhen(true)] out LazyManager? lazyManager)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                lazyManager = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("LazyManager", out IGameHandler handler))
            {
                lazyManager = handler.Cast<LazyManager>();
                return true;
            }

            lazyManager = null;
            return false;
        }

        /// <summary>
        /// Lazy Ai Factory Manager
        /// </summary>
        public static bool TryGetLazyAIFactoryManager([NotNullWhen(true)] out LazyAIFactoryManager? lazyAIFactoryManager)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                lazyAIFactoryManager = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("LazyAIFactoryManager", out IGameHandler handler))
            {
                lazyAIFactoryManager = handler.Cast<LazyAIFactoryManager>();
                return true;
            }

            lazyAIFactoryManager = null;
            return false;
        }

        /// <summary>
        /// Lazy Loaded Hit Objects
        /// </summary>
        public static bool TryGetLazyHitFactoryManager([NotNullWhen(true)] out LazyHitFactoryManager? lazyHitFactoryManager)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                lazyHitFactoryManager = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("LazyHitFactoryManager", out IGameHandler handler))
            {
                lazyHitFactoryManager = handler.Cast<LazyHitFactoryManager>();
                return true;
            }

            lazyHitFactoryManager = null;
            return false;
        }

        /// <summary>
        /// Congition Manager
        /// </summary>
        public static bool TryGetCognitionOctreeManager([NotNullWhen(true)] out CognitionOctreeManager? cognitionOctreeManager)
        {
            if (!TryGetGameManager(out GameManager? gameManager))
            {
                cognitionOctreeManager = null;
                return false;
            }
            if (gameManager.TryGetGameHandler("CognitionOctreeManager", out IGameHandler handler))
            {
                cognitionOctreeManager = handler.Cast<CognitionOctreeManager>();
                return true;
            }

            cognitionOctreeManager = null;
            return false;
        }

        /**
         * Access to the AstarPath for navigation and pathfinding, as an example <see cref="AstarPath.GetNearest(UnityEngine.Vector3)"/>
         */
        public static AstarPath? GetAstarPath()
        {
            return AstarPath.active;
        }
    }
}
