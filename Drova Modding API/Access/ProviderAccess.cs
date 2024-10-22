using Il2Cpp;
using Il2CppCustomFramework.Localization;
using Il2CppDrova;
using Il2CppDrova.Achievements;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GUI;
using Il2CppDrova.Items.Stats;
using Il2CppDrova.QuestSystem;
using UnityEngine;

namespace Drova_Modding_API.Access
{
    /// <summary>
    ///  Easy access to the DrovaResourceProvider and its ressources
    /// </summary>
    public class ProviderAccess
    {
        private static DrovaResourceProvider _DrovaResourceProvider;

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
        public TooltipBuilder GetTooltipBuilder()
        {
            return GetDrovaResourceProvider()._handler[6].Cast<TooltipBuilder>();
        }

        /// <summary>
        /// Provide access to further Databases, like items, effects, recipes, etc.
        /// </summary>
        public GameDatabase GetGameDatabase()
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


    }
}
