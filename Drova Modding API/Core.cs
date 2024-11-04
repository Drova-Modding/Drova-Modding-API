#if DEBUG
using Drova_Modding_API.Access;
#endif

using Drova_Modding_API.Register;
using Drova_Modding_API.Systems.WorldEvents;
using Drova_Modding_API.UI;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;

[assembly: MelonInfo(typeof(Drova_Modding_API.Core), "Drova Modding API", "0.2.3", "Drova Modding", null)]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: MelonPriority(1000)]
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
            base.OnLateInitializeMelon();
            LoggerInstance.Msg("Initialized Modding API.");
            ClassInjector.RegisterTypeInIl2Cpp<GUI_ConfigOption_Slider_Float>();
            ClassInjector.RegisterTypeInIl2Cpp<DropdownHandler>();
            ClassInjector.RegisterTypeInIl2Cpp<GUI_Options_Controls_KeyFieldElement_Custom>();
            ClassInjector.RegisterTypeInIl2Cpp<WorldEventSystemManager>();
            AssemblyLocation = MelonAssembly.Location;

        }

#if DEBUG
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Scene_MainMenu")
            {
                ProviderAccess.GetCheatGameHandler().EnableCheatMode(true);
            }
        }

#endif

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            ActionKeyRegister.Instance.LoadKeyCodes();

        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            OnMonoUpdate?.Invoke();
        }
    }
}
