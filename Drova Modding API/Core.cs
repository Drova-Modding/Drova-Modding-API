﻿#if DEBUG
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
using Il2CppDrova.SaveData;
using Il2CppInterop.Runtime.InteropTypes.Fields;

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
        /**
         * The tag for GO for the modding API
         */
        public const string ModdingApiTag = "ModdingAPI";


        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            ClassInjector.RegisterTypeInIl2Cpp<GUI_ConfigOption_Slider_Float>();
            ClassInjector.RegisterTypeInIl2Cpp<DropdownHandler>();
            ClassInjector.RegisterTypeInIl2Cpp<GUI_Options_Controls_KeyFieldElement_Custom>();
            ClassInjector.RegisterTypeInIl2Cpp<WorldEventSystemManager>();
            ClassInjector.RegisterTypeInIl2Cpp<AreaNameSystem>();

            AssemblyLocation = MelonAssembly.Location;
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
            ActionKeyRegister.Instance.LoadKeyCodes();
            ModdingUI.RegisterModdingUI();

            new MyData().Test = 5;
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

[RegisterTypeInIl2CppWithInterfaces(typeof(ISaveData))]
public class MyData : Il2CppSystem.Object
{
    public Il2CppValueField<int> Test;

    public MyData(IntPtr ptr): base(ptr)
    {
    }

    public MyData(): this(ClassInjector.DerivedConstructorPointer<MyData>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public Il2CppSystem.Object DeepCopy()
    {
        return new MyData
        {
            Test = Test
        };
    }
}