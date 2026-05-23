using UnityEngine;
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Register;
using Drova_Modding_API.Systems;

using Drova_Modding_API.Systems.ModdingUI;
using MelonLoader;
using UnityEngine.SceneManagement;

#if DEBUG
using Drova_Modding_API.Systems.Spawning.Templates;
using UnityEngine.InputSystem;
#endif

[assembly: MelonInfo(typeof(Drova_Modding_API.Core), "Drova Modding API", "0.4.3", "Drova Modding", null)]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: VerifyLoaderVersion(0, 7, 0, true)]
[assembly: MelonPriority(-1)]
namespace Drova_Modding_API
{
    /**
     * The core class of the Drova Modding API.
     */
    public class Core : MelonMod
    {
        internal static string? AssemblyLocation;
        internal bool InMainMenu = false;
#if DEBUG
        private readonly InputAction _consoleAction = new("Console", InputActionType.Button, "<Keyboard>/backquote");
#endif

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
#if DEBUG
            _consoleAction.Enable();
#endif
            SystemInit.RegisterStores();
            LoggerInstance.Msg("Initialized Modding API.");
            OptionMenuAccess.Instance.OnOptionMenuClose += () =>
            {
                if (!InMainMenu) InputActionRegister.Instance.EnableGameplayActions();
                InputActionRegister.Instance.SaveActions();
            };
            OptionMenuAccess.Instance.OnOptionMenuOpen += () =>
            {
                InputActionRegister.Instance.DisableGameplayActions();
            };
        }

        /// <inheritdoc/>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            if (sceneName == SceneNames.MainMenu)
            {
                // Retrigger it to make sure that the close call is registered
                OptionMenuAccess.OnOptionClose();
                ModdingUI.RegisterLocalization();
                LocalizationAccess.CreateLocalizationEntriesFromFolder();
#if DEBUG
                ProviderAccess.GetCheatGameHandler().EnableCheatMode(true);
                //_ttsFile.CreateDialogueFile();
#endif
                InputActionRegister.Instance.DisableGameplayActions();
                InMainMenu = true;
            }
            if (sceneName == SceneNames.GameplayMain)
            {
                // Retrigger it to make sure that the close call is registered
                OptionMenuAccess.OnOptionClose();
                SystemInit.GameplayInit();
                InputActionRegister.Instance.EnableGameplayActions();
                InMainMenu = false;
            }
            if (sceneName == SceneNames.AILogic)
            {
                SystemInit.AiLogicInit(SceneManager.GetSceneByName(sceneName));
            }
            // if (sceneName == SceneNames.AILogic)
            // {
            //     _ttsFile.GenerateWorldDialogues();
            // }
        }

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            AssemblyLocation = MelonAssembly.Location;
            GameObject gameObject = new("ModdingAPIRegister");
            gameObject.AddComponent<InputActionRegister>();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            ModdingUI.RegisterModdingUI();
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
#if DEBUG
            if (_consoleAction.WasReleasedThisFrame())
            {
                var cheatHandler = ProviderAccess.GetCheatGameHandler();
                if (cheatHandler == null) return;
                cheatHandler.EnableCheatMode(!cheatHandler.IsCheatModeEnabled);
            }
#endif
        }
    }
}