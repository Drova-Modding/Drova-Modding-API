﻿using Il2Cpp;
using Il2CppCustomFramework.Localization;
using Il2CppDrova;
using Il2CppDrova.Achievements;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GUI;
using Il2CppDrova.Items.Stats;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.Utilities.LazyLoading;
using Il2CppDrova.Weather;

namespace Drova_Modding_API.Access
{
    /// <summary>
    ///  Easy access to the DrovaResourceProvider and its ressources
    /// </summary>
    public static class ProviderAccess
    {
        private static DrovaResourceProvider _DrovaResourceProvider;
        private static GameManager _gameManager;

        /// <summary>
        /// Access to the DrovaResourceProvider
        /// </summary>
        public static DrovaResourceProvider GetDrovaResourceProvider()
        {
            if (_DrovaResourceProvider != null)
                return _DrovaResourceProvider;
            _DrovaResourceProvider = UnityEngine.Object.FindAnyObjectByType<DrovaResourceProvider>();
            return _DrovaResourceProvider;
        }

        /// <summary>
        /// Contains BHV Trees
        /// </summary>
        public static AIDatabase GetAIDatabase()
        {
            return GetDrovaResourceProvider()._handler[0].Cast<AIDatabase>();
        }

        /// <summary>
        ///  Contains fading materials
        ///  </summary>
        public static FadingMaterials GetFadingMaterials()
        {
            return GetDrovaResourceProvider()._handler[1].Cast<FadingMaterials>();
        }

        /// <summary>
        /// Contains Localized Strings for Stats
        /// </summary>
        public static GenericStatLoca GetGenericStatLoca()
        {
            return GetDrovaResourceProvider()._handler[2].Cast<GenericStatLoca>();
        }

        /// <summary>
        /// Contains all global stats for the game <see cref="GenericStatDesc"/>
        /// </summary>

        public static StatContainer GetGlobalStatsContainer()
        {
            return GetDrovaResourceProvider()._handler[3].Cast<StatContainer>();
        }

        /// <summary>
        /// Localization Database for the game, for better access use <see cref="LocalizationAccess"/>
        /// </summary>
        public static LocalizationDB GetLocalizationDB()
        {
            return GetDrovaResourceProvider()._handler[4].Cast<LocalizationDB>();
        }

        /// <summary>
        /// Contains Water Animation speed
        /// </summary>
        public static TilemapDatabase GetTilemapDatabase()
        {
            return GetDrovaResourceProvider()._handler[5].Cast<TilemapDatabase>();
        }

        /// <summary>
        ///  Tooltip Builder for the game <see cref="TooltipElement"/>
        /// </summary>
        /// <returns></returns>
        public static TooltipBuilder GetTooltipBuilder()
        {
            return GetDrovaResourceProvider()._handler[6].Cast<TooltipBuilder>();
        }

        /// <summary>
        /// Provide access to further Databases, like items, effects, recipes, etc.
        /// </summary>
        public static GameDatabase GetGameDatabase()
        {
            return GetDrovaResourceProvider()._handler[7].Cast<GameDatabase>();
        }

        /// <summary>
        /// Provides access to all GameHandlers for easier access use the Methode Get[HandlerName]GameHandler
        /// </summary>
        public static ScriptableGameHandlerDatabase GetScriptableGameHandlerDatabase()
        {
            return GetDrovaResourceProvider()._handler[8].Cast<ScriptableGameHandlerDatabase>();
        }

        /// <summary>
        ///  Access to FSM and Graphes
        /// </summary>
        public static FactoryAIGameHandler GetFactoryAIGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[0].Cast<FactoryAIGameHandler>();
        }

        /// <summary>
        ///  Access to the Achievement System
        /// </summary>
        public static AchievementGameHandler GetAchievementGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[1].Cast<AchievementGameHandler>();
        }

        /// <summary>
        /// Provides access to PlayerCraftedEvent and PlayerUsedConsumable Event
        /// </summary>
        public static AnalyticsGameHandler GetAnalyticsGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[2].Cast<AnalyticsGameHandler>();
        }

        /// <summary>
        /// SFX Access and Play Sounds for Actors
        /// </summary>
        public static AudioGameHandler GetAudioGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[3].Cast<AudioGameHandler>();
        }

        /// <summary>
        /// Camera Target and Offset
        /// </summary>
        public static CameraGameHandler GetCameraGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[4].Cast<CameraGameHandler>();
        }

        /// <summary>
        /// Access to all Cheat related stuff, like register spawnable Actors/Items and other
        /// </summary>
        public static CheatGameHandler GetCheatGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[5].Cast<CheatGameHandler>();
        }

        /// <summary>
        /// Access to <see cref="ConfigFileBridgeOptions"/>
        /// </summary>
        public static ConfigGameHandler GetConfigGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[6].Cast<ConfigGameHandler>();
        }

        /// <summary>
        /// Create reports and get the Folder of crashes
        /// </summary>
        public static CrashGameHandler GetCrashGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[7].Cast<CrashGameHandler>();
        }

        /// <summary>
        /// Active dialogues and access to the Dialogue System
        /// </summary>
        public static DialogueSystemGameHandler GetDialogueSystemGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[8].Cast<DialogueSystemGameHandler>();
        }

        /// <summary>
        /// Access to the player object and events for entities spawning, despawning, etc. and entities in scene
        /// </summary>
        /// <returns></returns>
        public static EntityGameHandler GetEntityGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[9].Cast<EntityGameHandler>();
        }

        /// <summary>
        /// Eqiupmentslots, Talents, StatusEffects
        /// </summary>
        public static GameplaySettingsHandler GetGameplaySettingsHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[10].Cast<GameplaySettingsHandler>();
        }

        /// <summary>
        /// Access to commom Items, like torches, money
        /// Access to the perceptionColor and other stuff
        /// </summary>
        public static GlobalAssetsGameHandler GetGlobalAssetsGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[11].Cast<GlobalAssetsGameHandler>();
        }

        /// <summary>
        /// Access to GUI Windows and their prefabs
        /// </summary>
        public static GUIGameHandler GetGUIGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[12].Cast<GUIGameHandler>();
        }
        /// <summary>
        /// Provides Position and access to their Databases
        /// </summary>
        public static MapGameHandler GetMapGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[13].Cast<MapGameHandler>();
        }

        /// <summary>
        /// Common ObjectPool Pattern
        /// </summary>
        public static ObjectPoolerGameHandler GetObjectPoolerGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[14].Cast<ObjectPoolerGameHandler>();
        }

        /// <summary>
        /// Tracks active Quests
        /// </summary>
        /// <returns></returns>
        public static QuestGameHandler GetQuestGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[15].Cast<QuestGameHandler>();
        }

        /// <summary>
        /// Events, Data related to SaveGames and the current Savegame
        /// </summary>
        public static SavegameGameHandler GetSaveGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[16].Cast<SavegameGameHandler>();
        }

        /// <summary>
        /// Fading, Streaming, Events for Scene Changes and access if the player is teleporting
        /// </summary>
        /// <returns></returns>
        public static SceneGameHandler GetSceneGameHandler()
        {
            return GetScriptableGameHandlerDatabase()._assets[17].Cast<SceneGameHandler>();
        }

        /// <summary>
        /// Tutorial Events and access to the Tutorial System
        /// </summary>
        public static TutorialManager GetTutorialManager()
        {
            return GetScriptableGameHandlerDatabase()._assets[18].Cast<TutorialManager>();
        }

        /// <summary>
        /// Get Game Manager 
        /// </summary>
        public static bool TryGetGameManager(out GameManager gameManager)
        {
            if (_gameManager)
            {
                gameManager = _gameManager;
                return true;
            }
            var gameManagerObject = UnityEngine.Object.FindObjectOfType<GameManager>();
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
        public static bool TryGetGameOverGameHandler(out GameOverGameHandler gameOverGameHandler)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                gameOverGameHandler = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("GameOverGameHandler", out IGameHandler handler))
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
        public static bool TryGetGameStateGameHandler(out GameStateGameHandler gameStateGameHandler)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                gameStateGameHandler = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("GameStateGameHandler", out IGameHandler handler))
            {
                gameStateGameHandler = handler.Cast<GameStateGameHandler>();
                return true;
            }

            gameStateGameHandler = null;
            return false;
        }

        /// <summary>
        /// Access to joarnal, map collection and crafting
        /// </summary>
        public static bool TryGetPlayerMetaDataGameHandler(out PlayerMetaDataGameHandler playerMetaDataGameHandler)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                playerMetaDataGameHandler = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("PlayerMetaDataGameHandler", out IGameHandler handler))
            {
                playerMetaDataGameHandler = handler.Cast<PlayerMetaDataGameHandler>();
                return true;
            }

            playerMetaDataGameHandler = null;
            return false;
        }

        /// <summary>
        /// WeatherGameHandler Access to weather events and properties
        /// </summary>
        public static bool TryGetWeatherGameHandler(out WeatherGameHandler WeatherGameHandler)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                WeatherGameHandler = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("WeatherGameHandler", out IGameHandler handler))
            {
                WeatherGameHandler = handler.Cast<WeatherGameHandler>();
                return true;
            }

            WeatherGameHandler = null;
            return false;
        }

        /// <summary>
        /// DaytimeGameHandler with events 
        /// </summary>
        public static bool TryGetDaytimeGameHandler(out DaytimeGameHandler daytimeGameHandler)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                daytimeGameHandler = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("DaytimeGameHandler", out IGameHandler handler))
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
        public static bool TryGetRootObjectHandler(out RootObjectHandler rootObjectHandler)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                rootObjectHandler = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("RootObjectHandler", out IGameHandler handler))
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
        public static bool TryGetLazyManager(out LazyManager lazyManager)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                lazyManager = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("LazyManager", out IGameHandler handler))
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
        public static bool TryGetLazyAIFactoryManager(out LazyAIFactoryManager lazyAIFactoryManager)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                lazyAIFactoryManager = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("LazyAIFactoryManager", out IGameHandler handler))
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
        public static bool TryGetLazyHitFactoryManager(out LazyHitFactoryManager lazyHitFactoryManager)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                lazyHitFactoryManager = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("LazyHitFactoryManager", out IGameHandler handler))
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
        public static bool TryGetCognitionOctreeManager(out CognitionOctreeManager cognitionOctreeManager)
        {
            if (!_gameManager && !TryGetGameManager(out GameManager _))
            {
                cognitionOctreeManager = null;
                return false;
            }
            if (_gameManager.TryGetGameHandler("CognitionOctreeManager", out IGameHandler handler))
            {
                cognitionOctreeManager = handler.Cast<CognitionOctreeManager>();
                return true;
            }

            cognitionOctreeManager = null;
            return false;
        }

        /**
         * Access to the AstarPath for navigation and pathfinding, as example <see cref="AstarPath.GetNearest(UnityEngine.Vector3)"/>
         */
        public static AstarPath GetAstarPath()
        {
            return AstarPath.active;
        }
    }
}
