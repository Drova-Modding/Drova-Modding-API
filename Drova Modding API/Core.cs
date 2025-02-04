
using UnityEngine;
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Register;
using Drova_Modding_API.Systems;

using Drova_Modding_API.Systems.ModdingUI;
using MelonLoader;
using Il2CppDrova;
using Drova_Modding_API.Systems.Dialogues;
using Il2CppDrova.MapDatabases;
using Drova_Modding_API.Systems.Audio.Dialogue;

#if DEBUG
using UnityEngine.InputSystem;
using System.Collections;
#endif

[assembly: MelonInfo(typeof(Drova_Modding_API.Core), "Drova Modding API", "0.4.0", "Drova Modding", null)]
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
        internal bool _inMainMenu = false;
        CreateTTSDialogueFile creation = new();
        bool created = false;

#if DEBUG
        internal bool actorsLoaded = false;
        internal bool aiLogicLoaded = false;
        private readonly InputAction consoleAction = new("Console", InputActionType.Button, "<Keyboard>/backquote");
#endif

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
#if DEBUG
            consoleAction.Enable();
#endif
            SystemInit.RegisterStores();
            LoggerInstance.Msg("Initialized Modding API.");
            OptionMenuAccess.Instance.OnOptionMenuClose += () =>
            {
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
                LocalizationAccess.CreateLocalizationEntriesFromFolder();

#if DEBUG
                ProviderAccess.GetCheatGameHandler().EnableCheatMode(true);
#endif
                InputActionRegister.Instance.DisableGameplayActions();
                _inMainMenu = true;
                creation.Init();
                creation.CreateDialogueFile();
            }
            if (sceneName == SceneNames.GameplayMain)
            {
                // Retrigger it to make sure that the close call is registered
                OptionMenuAccess.OnOptionClose();
                SystemInit.Init();
                InputActionRegister.Instance.EnableGameplayActions();
                _inMainMenu = false;
            }
            if (sceneName == SceneNames.AILogic)
            {
                aiLogicLoaded = true;
            }
            if (sceneName == SceneNames.Actor)
            {
                actorsLoaded = true;
            }
            if (actorsLoaded && aiLogicLoaded && !created)
            {
                created = true;
                creation.GeneratePointDialogues();
            }
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
            if (consoleAction.WasReleasedThisFrame())
            {
                ProviderAccess.GetCheatGameHandler().EnableCheatMode(!ProviderAccess.GetCheatGameHandler().IsCheatModeEnabled);
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                MelonCoroutines.Start(SetupNPC());
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                MapDatabase db = MapDatabaseProvider.Database.TryCast<MapDatabase>();
                Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.Dictionary<string, BaseMapDatabaseObject>>.Enumerator enumerator = db._dataCache.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    MelonLogger.Msg("Masterkey: " + enumerator.Current.Key);
                    Il2CppSystem.Collections.Generic.Dictionary<string, BaseMapDatabaseObject>.Enumerator nextEnumerator = enumerator.Current.Value.GetEnumerator();
                    while (nextEnumerator.MoveNext())
                    {
                        MelonLogger.Msg(nextEnumerator.Current.Key);
                        MelonLogger.Msg(nextEnumerator.Current.Value);
                    }
                }
                //AddressableAccess.Bandits.Human_Bandit_Mine_01.InstantiateAsync(PlayerAccess.GetPlayer().gameObject.transform.position, Quaternion.identity);
            }
#endif
        }

        private IEnumerator SetupNPC()
        {
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> npc = AddressableAccess.NPCs.Human_Template.InstantiateAsync(PlayerAccess.GetPlayer().gameObject.transform.position, Quaternion.identity);
            while (!npc.IsDone)
            {
                yield return new WaitForSeconds(0.1f);
            }
            DialogGraph.AddDialogGraph(npc.Result.GetComponentInChildren<Actor>());
        }
    }
}