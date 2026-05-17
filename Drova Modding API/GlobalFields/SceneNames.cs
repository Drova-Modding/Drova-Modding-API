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
        * The name of the Creates scene where all predefined Creatures live.
        */
        public const string Creatures = "Scene_Creatures";
        
        /**
        * The name of the Creates scene where some predefined Creatures live.
        */
        public const string CreaturesStandard = "Scene_CreaturesStandard";

        /**
         * The name of the Actor scene where all actors live.
         */
        public const string Actor = "Scene_Actors";
        /**
        * The name of the scene where the option menu is located. And other GUIS like Cheats and Player HUD
        */
        public const string OptionSceneName = "RuntimeScene_GUI";
    }
}
