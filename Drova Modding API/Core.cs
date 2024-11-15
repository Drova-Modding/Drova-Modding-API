#if DEBUG
using Drova_Modding_API.Access;
using UnityEngine;
#endif

using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Register;
using Drova_Modding_API.Systems;

using Drova_Modding_API.Systems.ModdingUI;
using Drova_Modding_API.Systems.WorldEvents;
using Drova_Modding_API.UI;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;

[assembly: MelonInfo(typeof(Drova_Modding_API.Core), "Drova Modding API", "0.3.0", "Drova Modding", null)]
[assembly: MelonGame("Just2D", "Drova")]
namespace Drova_Modding_API
{
    /**
     * The core class of the Drova Modding API.
     */
    public class Core : MelonMod
    {
        internal static string AssemblyLocation;
        internal static event Action OnMonoUpdate;


        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            SystemInit.RegisterIl2Cpp();
            SystemInit.RegisterStores();
            LoggerInstance.Msg("Initialized Modding API.");
        }

        /// <inheritdoc/>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {

            if (sceneName == SceneNames.MainMenu)
            {
                ModdingUI.RegisterLocalization();
#if DEBUG
                ProviderAccess.GetCheatGameHandler().EnableCheatMode(true);
#endif
            }

            if (sceneName == SceneNames.GameplayMain)
            {
                SystemInit.Init();
            }
        }


        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            AssemblyLocation = MelonAssembly.Location;
            ActionKeyRegister.Instance.LoadKeyCodes();
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