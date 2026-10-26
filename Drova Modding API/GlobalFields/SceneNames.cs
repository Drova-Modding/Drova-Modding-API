namespace Drova_Modding_API.GlobalFields
{
    /// <summary>
    /// Contains common scene names.
    /// </summary>
    public static class SceneNames
    {
        /**
         * The name of the main menu scene.
         */
        public const string MainMenu = "Scene_MainMenu";

        /**
         * The name of the gameplay main scene. Where the Player, Camera, and GameManager live.
         */
        public const string GameplayMain = "Scene_Gameplay_Main";

        /**
         * The name of the AI logic scene.
         */
        public const string AILogic = "Scene_AILogic";
        
        /**
        * The name of the scene where all predefined creatures live.
        */
        public const string Creatures = "Scene_Creatures";
        
        /**
        * The name of the scene where some predefined creatures live.
        */
        public const string CreaturesStandard = "Scene_CreaturesStandard";

        /**
         * The name of the Actor scene where all actors live.
         */
        public const string Actor = "Scene_Actors";
        /**
        * The name of the scene holding the option menu and other GUIs like cheats and the player HUD.
        */
        public const string OptionSceneName = "RuntimeScene_GUI";
    }
}
