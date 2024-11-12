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
using Drova_Modding_API.Systems.SaveGame;
using Il2CppInterop.Runtime;
using Il2CppSirenix.Serialization;
using Il2CppSystem.Runtime;

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
            ClassInjector.RegisterTypeInIl2Cpp<LazyActorSaveData>();
            ClassInjector.RegisterTypeInIl2Cpp<SaveGameSystem>();
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                MelonLogger.Msg("AssemblyResolve: " + args.Name);
                return null;
            };
            AssemblyLocation = MelonAssembly.Location;
            LoggerInstance.Msg("Initialized Modding API.");
        }

        /// <inheritdoc/>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {

            if (sceneName == SceneNames.MainMenu)
            {
                //if (DefaultSerializationBinder.typeMap.TryAdd("Drova_Modding_API.Systems.SaveGame.ModdingSave, InjectedMonoTypes", Il2CppType.Of<ModdingSave>()))
                //{
                //    MelonLogger.Msg("Added ModdingSave to typeMap");
                //}
                //MelonLogger.Msg("Queued Assemblies: " + DefaultSerializationBinder.assembliesQueuedForRegister.Count);
                //foreach (var assembly in DefaultSerializationBinder.assembliesQueuedForRegister)
                //{
                //    MelonLogger.Msg(assembly.FullName);
                //}
                //MelonLogger.Msg("Queued Assembly Load Events: " + DefaultSerializationBinder.assemblyLoadEventsQueuedForRegister.Count);
                //foreach (var assembly in DefaultSerializationBinder.assemblyLoadEventsQueuedForRegister)
                //{
                //    MelonLogger.Msg(assembly.LoadedAssembly.FullName);
                //}
                if (DefaultSerializationBinder.typeMap.TryAdd("Drova_Modding_API.Systems.SaveGame.LazyActorSaveData, InjectedMonoTypes", Il2CppType.Of<LazyActorSaveData>()))
                {
                    MelonLogger.Msg("Added LazyActorSaveData to typeMap");
                }
                if (DefaultSerializationBinder.nameMap.TryAdd(Il2CppType.Of<LazyActorSaveData>(), "Drova_Modding_API.Systems.SaveGame.LazyActorSaveData, InjectedMonoTypes"))
                {
                    MelonLogger.Msg("Added LazyActorSaveData to nameMap");
                }
                DefaultSerializationBinder.RegisterAssembly(Il2CppType.Of<LazyActorSaveData>().Assembly);
                MelonLogger.Msg("Added LazyActorSaveData to assemblyNameLookUp");

                //foreach (var type in DefaultSerializationBinder.assemblyNameLookUp)
                //{
                //    MelonLogger.Msg(type.Key + " : " + type.Value);
                //    foreach (var t in type.Value.GetTypes())
                //    {
                //        MelonLogger.Msg(t.FullName);
                //        MelonLogger.Msg(t.IsClass);
                //    }
                //}




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
