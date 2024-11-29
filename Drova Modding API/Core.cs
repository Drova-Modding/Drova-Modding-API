
using UnityEngine;
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Register;
using Drova_Modding_API.Systems;

using Drova_Modding_API.Systems.ModdingUI;
using MelonLoader;

[assembly: MelonInfo(typeof(Drova_Modding_API.Core), "Drova Modding API", "0.3.0", "Drova Modding", null)]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: VerifyLoaderVersion(0, 6, 6, true)]
[assembly: MelonPriority(-1)]
namespace Drova_Modding_API
{
    /**
     * The core class of the Drova Modding API.
     */
    public class Core : MelonMod
    {
        internal static string AssemblyLocation;
        internal static event Action OnMonoUpdate;
        internal bool _inMainMenu = false;

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            SystemInit.RegisterIl2Cpp();
            SystemInit.RegisterStores();
            LoggerInstance.Msg("Initialized Modding API.");
            OptionMenuAccess.Instance.OnOptionMenuClose += () => { 
                if (!_inMainMenu) InputActionRegister.Instance.EnableGameplayActions();
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

#if DEBUG
                ProviderAccess.GetCheatGameHandler().EnableCheatMode(true);
#endif
                InputActionRegister.Instance.DisableGameplayActions();
                _inMainMenu = true;
            }
            if (sceneName == SceneNames.GameplayMain)
            {
                // Retrigger it to make sure that the close call is registered
                OptionMenuAccess.OnOptionClose();
                SystemInit.Init();
                InputActionRegister.Instance.EnableGameplayActions();
                _inMainMenu = false;
            }
        }

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            AssemblyLocation = MelonAssembly.Location;
            ActionKeyRegister.Instance.LoadKeyCodes();
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
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                ProviderAccess.GetCheatGameHandler().EnableCheatMode(!ProviderAccess.GetCheatGameHandler()._cheatModeEnabled);
            }
#endif
            OnMonoUpdate?.Invoke();
        }
    }
}