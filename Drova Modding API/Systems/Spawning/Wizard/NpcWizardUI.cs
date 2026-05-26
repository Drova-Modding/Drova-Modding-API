using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues;
using Drova_Modding_API.Systems.Dialogues.Editor;
using Drova_Modding_API.Systems.Dialogues.Store;
using Il2CppDrova;
using Il2CppInterop.Runtime.Attributes;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    [RegisterTypeInIl2Cpp]
    internal class NpcWizardUI(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private static NpcWizardUI? _instance;

        private const float WindowWidthRatio = 0.62f;
        private const float WindowHeightRatio = 0.80f;
        private const float MinWindowWidth = 820f;
        private const float MinWindowHeight = 620f;
        private const float DefinitionsWindowWidth = 760f;
        private const float DefinitionsWindowHeight = 520f;
        private const float ScreenPadding = 16f;
        private const string WizardGraphEditorName = "ModdingAPI_WizardGraphEditor";

        private bool _visible;
        private bool _initialPlayerPositionApplied;
        private Rect _windowRect = new(40f, 40f, 700f, 760f);
        private bool _windowRectInitialized;
        private bool _isResizing;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartSize;
        private Vector2 _scroll;
        private bool _definitionsVisible;
        private Rect _definitionsWindowRect = new(70f, 70f, DefinitionsWindowWidth, DefinitionsWindowHeight);
        private bool _definitionsWindowRectInitialized;
        private Vector2 _definitionsScroll;
        private List<ExternalNpcPlacementSystem.ExternalNpcDefinition> _cachedDefinitions = [];
        private readonly Dictionary<string, bool> _moduleExpandedByKey = new(StringComparer.OrdinalIgnoreCase);
        private GraphEditorManager? _graphEditorManager;
        private bool _gameplayInputBlockedByWizard;
        private string? _loadedDefinitionId;
        private string? _loadedDialogueDefinitionId;
        private string? _loadedDialogueId;
        private PendingOverwriteAction _pendingOverwriteAction;
        private string _pendingOverwriteMessage = string.Empty;

        private ExternalNpcPlacementSystem.ExternalNpcDefinition _definition = CreateDefaultDefinitionAtPlayerPosition();
        private string _status = "Use cheat command 'npc_wizard' to open this window.";
        private string _positionXInput = string.Empty;
        private string _positionYInput = string.Empty;

        private enum PendingOverwriteAction
        {
            None,
            SaveDefinition,
            SaveDefinitionAndSpawn,
            SaveDialogue
        }

        internal void Awake()
        {
            _instance = this;
            ExternalNpcModuleRegistry.EnsureDefaults(_definition);
            TrySyncDefinitionPositionFromPlayer();
            SyncPositionInputs();
        }

        internal void OnDestroy()
        {
            SetGameplayInputBlocked(false);

            if (_instance == this)
                _instance = null;
        }

        internal void Update()
        {
            // Keep gameplay maps disabled while the wizard is open, even if another
            // tool toggles them back on.
            if (_visible && !_gameplayInputBlockedByWizard)
                SetGameplayInputBlocked(true);
        }

        internal void OnGUI()
        {
            if (!_visible)
                return;

            EnsureWindowRectFitsScreen();
            _windowRect = GUI.Window(487319, _windowRect, new Action<int>(DrawWindow), "NPC Wizard");

            if (_definitionsVisible)
            {
                EnsureDefinitionsWindowRectFitsScreen();
                _definitionsWindowRect = GUI.Window(487320, _definitionsWindowRect, new Action<int>(DrawDefinitionsWindow), "NPC Definitions");
            }
        }

        [HideFromIl2Cpp]
        public static void Toggle()
        {
            if (_instance == null)
                return;

            _instance.SetWizardVisible(!_instance._visible);
            if (_instance._visible)
            {
                ExternalNpcModuleRegistry.EnsureDefaults(_instance._definition);
                ReadableIdModuleSupport.WarmUpCache();
                _instance.TrySyncDefinitionPositionFromPlayer();
                _instance.SyncPositionInputs();
                _instance.RefreshDefinitionsCache();
                _instance._status = $"Placement folder: {ExternalNpcPlacementSystem.GetNpcPlacementFolderPath()}  |  Wizard file: {ExternalNpcPlacementSystem.GetWizardFilePath()}";
            }
        }

        [HideFromIl2Cpp]
        private void SetWizardVisible(bool visible)
        {
            _visible = visible;
            if (!_visible)
                _definitionsVisible = false;

            SetGameplayInputBlocked(_visible);
        }

        [HideFromIl2Cpp]
        private void SetGameplayInputBlocked(bool blocked)
        {
            if (_gameplayInputBlockedByWizard == blocked)
                return;

            InputAccess.ToggleGameplayActionMaps(!blocked);
            _gameplayInputBlockedByWizard = blocked;
        }

        [HideFromIl2Cpp]
        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(_windowRect.height - 150f));

            DrawBaseFields();
            DrawModuleFields();

            GUILayout.EndScrollView();

            if (!string.IsNullOrWhiteSpace(_status))
                GUILayout.Label(_status);

            if (HasPendingOverwrite())
                DrawOverwriteConfirmationPrompt();
            else
                DrawActionButtons();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset Size", GUILayout.Width(110f)))
                ResetWindowRectToScreen();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            Rect resizeHandleRect = new(_windowRect.width - 24f, _windowRect.height - 24f, 18f, 18f);
            GUI.Box(resizeHandleRect, "");
            HandleResize(resizeHandleRect);

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        [HideFromIl2Cpp]
        private void DrawBaseFields()
        {
            GUILayout.Label("Base Definition");
            GUILayout.Label("Definition Id");
            _definition.Id = GUILayout.TextField(_definition.Id);

            GUILayout.Label("Display Name");
            _definition.Name = GUILayout.TextField(_definition.Name);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Position X", GUILayout.Width(140f));
            _positionXInput = GUILayout.TextField(_positionXInput);
            GUILayout.Label("Position Y", GUILayout.Width(140f));
            _positionYInput = GUILayout.TextField(_positionYInput);
            GUILayout.EndHorizontal();

            if (float.TryParse(_positionXInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x))
                _definition.PositionX = x;
            if (float.TryParse(_positionYInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                _definition.PositionY = y;

            _definition.Enabled = GUILayout.Toggle(_definition.Enabled, "Enabled for auto spawn on game start");
        }

        [HideFromIl2Cpp]
        private void DrawModuleFields()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Modules");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All", GUILayout.Width(100f)))
                SetAllModuleExpandedState(true);
            if (GUILayout.Button("Collapse All", GUILayout.Width(100f)))
                SetAllModuleExpandedState(false);
            GUILayout.EndHorizontal();

            IReadOnlyList<IExternalNpcModule> modules = ExternalNpcModuleRegistry.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                IExternalNpcModule module = modules[i];
                bool expanded = GetModuleExpandedState(module.Key);

                GUILayout.BeginVertical("box");
                if (GUILayout.Button($"{(expanded ? "v" : ">")} {module.DisplayName} ({module.Key})", GUILayout.ExpandWidth(true)))
                {
                    expanded = !expanded;
                    _moduleExpandedByKey[module.Key] = expanded;
                }

                if (!_definition.ModuleConfig.TryGetValue(module.Key, out string? payload))
                {
                    payload = module.CreateDefaultPayload();
                    _definition.ModuleConfig[module.Key] = payload;
                }

                if (expanded)
                {
                    payload ??= module.CreateDefaultPayload();
                    _definition.ModuleConfig[module.Key] = module.DrawWizardUiAndSerialize(payload);

                    if (string.Equals(module.Key, ExternalDialogueModule.ModuleKey, StringComparison.OrdinalIgnoreCase))
                        DrawDialogueModuleActions();
                }
                GUILayout.EndVertical();
            }
        }

        [HideFromIl2Cpp]
        private void DrawDialogueModuleActions()
        {
            bool isCurrentDefinitionSpawned = ExternalNpcPlacementSystem.IsDefinitionSpawned(_definition.Id);

            GUILayout.Space(4f);
            if (!isCurrentDefinitionSpawned)
                GUILayout.Label("Dialogue actions require a spawned NPC.");

            GUILayout.BeginHorizontal();
            GUI.enabled = isCurrentDefinitionSpawned && !HasPendingOverwrite();

            if (GUILayout.Button("Edit Graph", GUILayout.Width(120f)))
            {
                if (TryOpenDialogueEditorForCurrentDefinition())
                    _status = "Opened dialogue editor for spawned NPC.";
            }

            if (GUILayout.Button("Save To Store", GUILayout.Width(120f)))
            {
                if (TrySaveDialogueForCurrentDefinition())
                    _status = "Saved spawned NPC dialogue to dialogue store.";
            }

            if (GUILayout.Button("Load To Spawned", GUILayout.Width(130f)))
            {
                if (TryLoadDialogueForCurrentDefinition())
                    _status = "Loaded stored dialogue into spawned NPC.";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        [HideFromIl2Cpp]
        private bool GetModuleExpandedState(string moduleKey)
        {
            if (_moduleExpandedByKey.TryGetValue(moduleKey, out bool expanded))
                return expanded;

            _moduleExpandedByKey[moduleKey] = false;
            return false;
        }

        [HideFromIl2Cpp]
        private void SetAllModuleExpandedState(bool expanded)
        {
            IReadOnlyList<IExternalNpcModule> modules = ExternalNpcModuleRegistry.Modules;
            for (int i = 0; i < modules.Count; i++)
                _moduleExpandedByKey[modules[i].Key] = expanded;
        }

        [HideFromIl2Cpp]
        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();
            bool isCurrentDefinitionSpawned = ExternalNpcPlacementSystem.IsDefinitionSpawned(_definition.Id);

            if (GUILayout.Button("New"))
            {
                _definition = CreateDefaultDefinitionAtPlayerPosition();
                MarkCurrentDefinitionAsUnloaded();
                SyncPositionInputs();
                _status = "Created a fresh definition template.";
            }

            if (GUILayout.Button("Load By Id"))
            {
                if (ExternalNpcPlacementSystem.TryGetDefinition(_definition.Id, out ExternalNpcPlacementSystem.ExternalNpcDefinition? found) && found != null)
                {
                    _definition = found;
                    ExternalNpcModuleRegistry.EnsureDefaults(_definition);
                    MarkCurrentDefinitionAsLoaded();
                    ClearDialogueLoadedMarker();
                    SyncPositionInputs();
                    _status = $"Loaded definition '{_definition.Id}'.";
                }
                else
                {
                    _status = $"Definition '{_definition.Id}' was not found.";
                }
            }

            if (GUILayout.Button("Save Definition"))
            {
                if (!ValidateForSave())
                    return;

                TrySaveDefinitionWithOptionalConfirmation(spawnAfterSave: false);
            }

            if (GUILayout.Button("Save + Spawn"))
            {
                if (!ValidateForSave())
                    return;

                TrySaveDefinitionWithOptionalConfirmation(spawnAfterSave: true);
            }

            GUI.enabled = isCurrentDefinitionSpawned;
            if (GUILayout.Button("Despawn"))
            {
                bool despawned = ExternalNpcPlacementSystem.DespawnDefinition(_definition.Id);
                _status = despawned
                    ? $"Despawned definition '{_definition.Id}'."
                    : $"Definition '{_definition.Id}' is not currently spawned.";
                RefreshDefinitionsCache();
            }
            GUI.enabled = true;

            if (GUILayout.Button(_definitionsVisible ? "Hide Definitions" : "Open Definitions"))
            {
                _definitionsVisible = !_definitionsVisible;
                if (_definitionsVisible)
                    RefreshDefinitionsCache();
            }

            if (GUILayout.Button("Close"))
            {
                SetWizardVisible(false);
            }

            GUILayout.EndHorizontal();
        }

        [HideFromIl2Cpp]
        private void DrawDefinitionsWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
                RefreshDefinitionsCache();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(90f)))
            {
                _definitionsVisible = false;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }
            GUILayout.EndHorizontal();

            _definitionsScroll = GUILayout.BeginScrollView(_definitionsScroll);
            for (int i = 0; i < _cachedDefinitions.Count; i++)
            {
                ExternalNpcPlacementSystem.ExternalNpcDefinition definition = _cachedDefinitions[i];
                bool isSpawned = ExternalNpcPlacementSystem.IsDefinitionSpawned(definition.Id);

                GUILayout.BeginVertical("box");
                GUILayout.Label($"{definition.Name} ({definition.Id})");
                GUILayout.Label($"Pos: {definition.PositionX:0.##}, {definition.PositionY:0.##}  |  Enabled: {(definition.Enabled ? "yes" : "no")}  |  Spawned: {(isSpawned ? "yes" : "no")}");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Load", GUILayout.Width(90f)))
                {
                    _definition = definition;
                    ExternalNpcModuleRegistry.EnsureDefaults(_definition);
                    MarkCurrentDefinitionAsLoaded();
                    ClearDialogueLoadedMarker();
                    SyncPositionInputs();
                    _status = $"Loaded definition '{_definition.Id}' from browser.";
                }

                if (GUILayout.Button("Teleport", GUILayout.Width(90f)))
                {
                    _status = TryTeleportPlayerToDefinition(definition) ? $"Teleported player to '{definition.Id}'." : "Player was not found. Teleport failed.";
                }

                if (GUILayout.Button("Spawn", GUILayout.Width(90f)))
                {
                    bool spawned = ExternalNpcPlacementSystem.SpawnDefinition(definition, skipIfAlreadySpawned: true, requireEnabled: false);
                    _status = spawned ? $"Spawned '{definition.Id}'." : $"Spawn skipped for '{definition.Id}'.";
                    RefreshDefinitionsCache();
                }

                GUI.enabled = isSpawned;
                if (GUILayout.Button("Despawn", GUILayout.Width(90f)))
                {
                    bool despawned = ExternalNpcPlacementSystem.DespawnDefinition(definition.Id);
                    _status = despawned ? $"Despawned '{definition.Id}'." : $"Despawn skipped for '{definition.Id}'.";
                    RefreshDefinitionsCache();
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        [HideFromIl2Cpp]
        private void RefreshDefinitionsCache()
        {
            _cachedDefinitions = ExternalNpcPlacementSystem.ReadAllDefinitions();
        }

        [HideFromIl2Cpp]
        private static bool TryTeleportPlayerToDefinition(ExternalNpcPlacementSystem.ExternalNpcDefinition definition)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor player) || player == null)
                return false;

            Vector3 current = player.transform.position;
            player.transform.position = new Vector3(definition.PositionX, definition.PositionY, current.z);
            return true;
        }

        [HideFromIl2Cpp]
        private bool ValidateForSave()
        {
            _definition.Id = _definition.Id.Trim();
            _definition.Name = _definition.Name.Trim();

            if (string.IsNullOrWhiteSpace(_definition.Id))
            {
                _status = "Definition Id is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_definition.Name))
            {
                _status = "Display Name is required.";
                return false;
            }

            return true;
        }

        [HideFromIl2Cpp]
        private bool TryOpenDialogueEditorForCurrentDefinition()
        {
            if (!TryGetCurrentDialogueId(out string dialogueId))
                return false;

            if (!ExternalNpcPlacementSystem.TryGetSpawnedActor(_definition.Id, out Actor? actor) || actor == null)
            {
                _status = "Spawn the current definition first, then open dialogue editor.";
                return false;
            }

            DS_DialogueTreeController controller = actor.GetComponentInChildren<DS_DialogueTreeController>();
            if (controller == null)
            {
                _status = "Selected NPC has no dialogue controller.";
                return false;
            }

            if (!TryEnsureDialogueGraphForActor(actor, controller, dialogueId))
                return false;

            EnsureWizardGraphEditor();
            if (_graphEditorManager == null)
            {
                _status = "Failed to initialize graph editor.";
                return false;
            }

            _graphEditorManager.gameObject.SetActive(true);
            _graphEditorManager.Init(actor);
            return true;
        }

        [HideFromIl2Cpp]
        private bool TryEnsureDialogueGraphForActor(Actor actor, DS_DialogueTreeController controller, string dialogueId)
        {
            if (controller.graph != null)
                return true;

            if (DialogueStore.TryLoadDialogue(dialogueId, out DialogueTree? loadedDialogue) && loadedDialogue != null)
            {
                loadedDialogue.Serialize(null);
                controller.graph = loadedDialogue;
                MarkCurrentDialogueAsLoaded(dialogueId);
                _status = $"Loaded stored dialogue '{dialogueId}'.";
                return true;
            }

            DialogueTree createdDialogue = CreateStarterDialogueTree(actor, dialogueId);
            if (createdDialogue == null)
            {
                _status = "Failed to create starter dialogue graph.";
                return false;
            }

            createdDialogue.Serialize(null);
            controller.graph = createdDialogue;
            ClearDialogueLoadedMarker();
            _status = $"Created new starter dialogue '{dialogueId}'.";
            return true;
        }

        [HideFromIl2Cpp]
        private static DialogueTree CreateStarterDialogueTree(Actor actor, string dialogueId)
        {
            DialogueTree tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.actorParameters = new Il2CppSystem.Collections.Generic.List<DialogueTree.ActorParameter>();

            string actorParameterId = Il2CppSystem.Guid.NewGuid().ToString();
            tree.actorParameters.Add(new DialogueTree.ActorParameter
            {
                _keyName = actor.name,
                Actor = null,
                ActorGuid = actor.GetGuidComponent()._guidString,
                _id = actorParameterId
            });

            DialogueTree.ActorParameter playerParameter = ActorParameterHelper.GetPlayerActorParameters();
            tree.actorParameters.Add(playerParameter);

            DS_StatementNode startNode = tree.AddNode<DS_StatementNode>();
            DS_Statement statement = new()
            {
                useGlobalLoca = true,
                GlobalLocaPath = "Modding_API/NpcWizard",
                locaKey = "StarterLine"
            };
            startNode.statement = statement;
            startNode.actorName = actor.name;
            startNode._actorParameterID = actorParameterId;
            startNode.TryGenerateUID();

            DS_MultipleChoiceNode endNode = tree.AddNode<DS_MultipleChoiceNode>();
            endNode.actorName = playerParameter._keyName;
            endNode._actorParameterID = playerParameter.ID;
            endNode.availableChoices = new Il2CppSystem.Collections.Generic.List<DS_MultipleChoiceNode.Choice>();
            endNode.availableChoices.Add(new DS_MultipleChoiceNode.Choice
            {
                statement = statement,
                isEndNode = true,
                UID = Il2CppSystem.Guid.NewGuid().ToString()
            });
            endNode.TryGenerateUID();

            tree.ConnectNodes(startNode, endNode);
            tree.primeNode = startNode;
            tree.name = dialogueId;
            return tree;
        }

        [HideFromIl2Cpp]
        private bool TrySaveDialogueForCurrentDefinition()
        {
            if (HasPendingOverwrite())
            {
                _status = "Confirm or cancel the pending overwrite first.";
                return false;
            }

            return TrySaveDialogueForCurrentDefinitionInternal(overwriteConfirmed: false);
        }

        [HideFromIl2Cpp]
        private bool TrySaveDialogueForCurrentDefinitionInternal(bool overwriteConfirmed)
        {
            if (!TryGetCurrentDialogueId(out string dialogueId))
                return false;

            if (!ExternalNpcPlacementSystem.TryGetSpawnedActor(_definition.Id, out Actor? actor) || actor == null)
            {
                _status = "Spawn the current definition first, then save dialogue.";
                return false;
            }

            DS_DialogueTreeController controller = actor.GetComponentInChildren<DS_DialogueTreeController>();
            if (controller == null || controller.graph == null)
            {
                _status = "Selected NPC has no dialogue graph to save.";
                return false;
            }

            DialogueTree dialogueTree = controller.graph.TryCast<DialogueTree>();
            if (dialogueTree == null)
            {
                _status = "Current NPC graph is not a DialogueTree.";
                return false;
            }

            if (!overwriteConfirmed && RequiresDialogueOverwriteConfirmation(dialogueId))
            {
                QueueOverwriteConfirmation(
                    PendingOverwriteAction.SaveDialogue,
                    $"Dialogue '{dialogueId}' already exists in store and is not currently loaded. Overwrite existing dialogue configuration?");
                return false;
            }

            bool saved = DialogueStore.SaveDialogue(dialogueId, dialogueTree);
            if (!saved)
            {
                _status = $"Failed to save dialogue '{dialogueId}'.";
                return false;
            }

            MarkCurrentDialogueAsLoaded(dialogueId);
            return true;
        }

        [HideFromIl2Cpp]
        private bool TryLoadDialogueForCurrentDefinition()
        {
            if (!TryGetCurrentDialogueId(out string dialogueId))
                return false;

            if (!ExternalNpcPlacementSystem.TryGetSpawnedActor(_definition.Id, out Actor? actor) || actor == null)
            {
                _status = "Spawn the current definition first, then load dialogue.";
                return false;
            }

            DS_DialogueTreeController controller = actor.GetComponentInChildren<DS_DialogueTreeController>();
            if (controller == null)
            {
                _status = "Selected NPC has no dialogue controller.";
                return false;
            }

            if (!DialogueStore.TryLoadDialogue(dialogueId, out DialogueTree? loadedDialogue) || loadedDialogue == null)
            {
                _status = $"No stored dialogue found for '{dialogueId}'.";
                return false;
            }

            loadedDialogue.Serialize(null);
            controller.graph = loadedDialogue;
            MarkCurrentDialogueAsLoaded(dialogueId);
            return true;
        }

        [HideFromIl2Cpp]
        private bool HasPendingOverwrite()
            => _pendingOverwriteAction != PendingOverwriteAction.None;

        [HideFromIl2Cpp]
        private void DrawOverwriteConfirmationPrompt()
        {
            GUILayout.Space(6f);
            GUILayout.BeginVertical("box");
            GUILayout.Label(_pendingOverwriteMessage);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Overwrite", GUILayout.Width(130f)))
                ExecutePendingOverwrite();

            if (GUILayout.Button("Cancel", GUILayout.Width(130f)))
            {
                ClearPendingOverwrite();
                _status = "Overwrite canceled.";
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void QueueOverwriteConfirmation(PendingOverwriteAction action, string message)
        {
            _pendingOverwriteAction = action;
            _pendingOverwriteMessage = message;
            _status = "Overwrite confirmation required.";
        }

        [HideFromIl2Cpp]
        private void ClearPendingOverwrite()
        {
            _pendingOverwriteAction = PendingOverwriteAction.None;
            _pendingOverwriteMessage = string.Empty;
        }

        [HideFromIl2Cpp]
        private void ExecutePendingOverwrite()
        {
            PendingOverwriteAction action = _pendingOverwriteAction;
            ClearPendingOverwrite();

            switch (action)
            {
                case PendingOverwriteAction.SaveDefinition:
                    SaveDefinitionInternal(spawnAfterSave: false);
                    break;
                case PendingOverwriteAction.SaveDefinitionAndSpawn:
                    SaveDefinitionInternal(spawnAfterSave: true);
                    break;
                case PendingOverwriteAction.SaveDialogue:
                    if (TrySaveDialogueForCurrentDefinitionInternal(overwriteConfirmed: true))
                        _status = "Saved spawned NPC dialogue to dialogue store.";
                    break;
            }
        }

        [HideFromIl2Cpp]
        private void TrySaveDefinitionWithOptionalConfirmation(bool spawnAfterSave)
        {
            if (HasPendingOverwrite())
            {
                _status = "Confirm or cancel the pending overwrite first.";
                return;
            }

            if (RequiresDefinitionOverwriteConfirmation())
            {
                QueueOverwriteConfirmation(
                    spawnAfterSave ? PendingOverwriteAction.SaveDefinitionAndSpawn : PendingOverwriteAction.SaveDefinition,
                    $"Definition '{_definition.Id}' already exists and is not currently loaded. Overwrite existing configuration?");
                return;
            }

            SaveDefinitionInternal(spawnAfterSave);
        }

        [HideFromIl2Cpp]
        private void SaveDefinitionInternal(bool spawnAfterSave)
        {
            bool saved = ExternalNpcPlacementSystem.UpsertDefinition(_definition);
            if (!saved)
            {
                _status = "Failed to save definition.";
                return;
            }

            MarkCurrentDefinitionAsLoaded();
            if (!spawnAfterSave)
            {
                _status = $"Saved definition '{_definition.Id}'.";
                RefreshDefinitionsCache();
                return;
            }

            bool spawned = ExternalNpcPlacementSystem.SpawnDefinition(_definition, skipIfAlreadySpawned: true, requireEnabled: false);
            _status = $"Save: ok, Spawn: {(spawned ? "spawned" : "skipped")}.";
            RefreshDefinitionsCache();
        }

        [HideFromIl2Cpp]
        private bool RequiresDefinitionOverwriteConfirmation()
        {
            if (string.IsNullOrWhiteSpace(_definition.Id))
                return false;

            if (!ExternalNpcPlacementSystem.TryGetDefinition(_definition.Id, out ExternalNpcPlacementSystem.ExternalNpcDefinition? existingDefinition) || existingDefinition == null)
                return false;

            return !string.Equals(_loadedDefinitionId, _definition.Id, StringComparison.OrdinalIgnoreCase);
        }

        [HideFromIl2Cpp]
        private bool RequiresDialogueOverwriteConfirmation(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
                return false;

            if (!DialogueExistsInStore(dialogueId))
                return false;

            bool loadedForCurrentDefinition =
                string.Equals(_loadedDialogueDefinitionId, _definition.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_loadedDialogueId, dialogueId, StringComparison.OrdinalIgnoreCase);
            return !loadedForCurrentDefinition;
        }

        [HideFromIl2Cpp]
        private static bool DialogueExistsInStore(string dialogueId)
            => File.Exists(DialogueStore.GetDialogueFilePath(dialogueId));

        [HideFromIl2Cpp]
        private void MarkCurrentDefinitionAsLoaded()
        {
            _loadedDefinitionId = _definition.Id;
        }

        [HideFromIl2Cpp]
        private void MarkCurrentDefinitionAsUnloaded()
        {
            _loadedDefinitionId = null;
            ClearDialogueLoadedMarker();
        }

        [HideFromIl2Cpp]
        private void MarkCurrentDialogueAsLoaded(string dialogueId)
        {
            _loadedDialogueDefinitionId = _definition.Id;
            _loadedDialogueId = dialogueId;
        }

        [HideFromIl2Cpp]
        private void ClearDialogueLoadedMarker()
        {
            _loadedDialogueDefinitionId = null;
            _loadedDialogueId = null;
        }

        [HideFromIl2Cpp]
        private bool TryGetCurrentDialogueId(out string dialogueId)
        {
            dialogueId = string.Empty;
            if (!_definition.ModuleConfig.TryGetValue(ExternalDialogueModule.ModuleKey, out string? payload))
            {
                _status = "Dialogue module payload was not found on this definition.";
                return false;
            }

            dialogueId = ExternalDialogueModule.GetDialogueId(payload);
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                _status = "Set a Dialogue Id in the Dialogue module first.";
                return false;
            }

            return true;
        }

        [HideFromIl2Cpp]
        private void EnsureWizardGraphEditor()
        {
            if (_graphEditorManager != null)
                return;

            GameObject graphEditorRoot = GameObject.Find(WizardGraphEditorName);
            if (graphEditorRoot == null)
            {
                graphEditorRoot = new GameObject(WizardGraphEditorName);
                DontDestroyOnLoad(graphEditorRoot);
            }

            _graphEditorManager = graphEditorRoot.GetComponent<GraphEditorManager>();
            if (_graphEditorManager == null)
                _graphEditorManager = graphEditorRoot.AddComponent<GraphEditorManager>();
        }

        [HideFromIl2Cpp]
        private void SyncPositionInputs()
        {
            _positionXInput = _definition.PositionX.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _positionYInput = _definition.PositionY.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        [HideFromIl2Cpp]
        private void TrySyncDefinitionPositionFromPlayer()
        {
            if (_initialPlayerPositionApplied)
                return;

            if (!PlayerAccess.TryGetPlayer(out Actor player) || player == null)
                return;

            Vector3 playerPosition = player.transform.position;
            _definition.PositionX = playerPosition.x;
            _definition.PositionY = playerPosition.y;
            _initialPlayerPositionApplied = true;
        }

        [HideFromIl2Cpp]
        private static ExternalNpcPlacementSystem.ExternalNpcDefinition CreateDefaultDefinitionAtPlayerPosition()
        {
            ExternalNpcPlacementSystem.ExternalNpcDefinition definition = ExternalNpcPlacementSystem.CreateDefaultDefinition();
            if (PlayerAccess.TryGetPlayer(out Actor player) && player != null)
            {
                Vector3 playerPosition = player.transform.position;
                definition.PositionX = playerPosition.x;
                definition.PositionY = playerPosition.y;
            }

            return definition;
        }

        [HideFromIl2Cpp]
        private void EnsureWindowRectFitsScreen()
        {
            if (!_windowRectInitialized)
            {
                ResetWindowRectToScreen();
                return;
            }

            float availableWidth = Mathf.Max(360f, Screen.width - (ScreenPadding * 2f));
            float availableHeight = Mathf.Max(320f, Screen.height - (ScreenPadding * 2f));
            float minWidth = Mathf.Min(MinWindowWidth, availableWidth);
            float minHeight = Mathf.Min(MinWindowHeight, availableHeight);
            float maxWidth = Mathf.Max(minWidth, Screen.width - (_windowRect.x + ScreenPadding));
            float maxHeight = Mathf.Max(minHeight, Screen.height - (_windowRect.y + ScreenPadding));
            _windowRect.width = Mathf.Clamp(_windowRect.width, minWidth, maxWidth);
            _windowRect.height = Mathf.Clamp(_windowRect.height, minHeight, maxHeight);
            _windowRect.x = Mathf.Clamp(_windowRect.x, ScreenPadding, Mathf.Max(ScreenPadding, Screen.width - _windowRect.width - ScreenPadding));
            _windowRect.y = Mathf.Clamp(_windowRect.y, ScreenPadding, Mathf.Max(ScreenPadding, Screen.height - _windowRect.height - ScreenPadding));
        }

        [HideFromIl2Cpp]
        private void ResetWindowRectToScreen()
        {
            float availableWidth = Mathf.Max(360f, Screen.width - (ScreenPadding * 2f));
            float availableHeight = Mathf.Max(320f, Screen.height - (ScreenPadding * 2f));
            float minWidth = Mathf.Min(MinWindowWidth, availableWidth);
            float minHeight = Mathf.Min(MinWindowHeight, availableHeight);
            float width = Mathf.Clamp(Screen.width * WindowWidthRatio, minWidth, availableWidth);
            float height = Mathf.Clamp(Screen.height * WindowHeightRatio, minHeight, availableHeight);
            float x = Mathf.Max(ScreenPadding, (Screen.width - width) * 0.5f);
            float y = Mathf.Max(ScreenPadding, (Screen.height - height) * 0.5f);
            _windowRect = new Rect(x, y, width, height);
            _windowRectInitialized = true;
        }

        [HideFromIl2Cpp]
        private void EnsureDefinitionsWindowRectFitsScreen()
        {
            if (!_definitionsWindowRectInitialized)
            {
                float width = Mathf.Min(DefinitionsWindowWidth, Screen.width - (ScreenPadding * 2f));
                float height = Mathf.Min(DefinitionsWindowHeight, Screen.height - (ScreenPadding * 2f));
                float x = Mathf.Max(ScreenPadding, (Screen.width - width) * 0.5f + 40f);
                float y = Mathf.Max(ScreenPadding, (Screen.height - height) * 0.5f + 20f);
                _definitionsWindowRect = new Rect(x, y, width, height);
                _definitionsWindowRectInitialized = true;
                return;
            }

            float maxX = Mathf.Max(ScreenPadding, Screen.width - _definitionsWindowRect.width - ScreenPadding);
            float maxY = Mathf.Max(ScreenPadding, Screen.height - _definitionsWindowRect.height - ScreenPadding);
            _definitionsWindowRect.x = Mathf.Clamp(_definitionsWindowRect.x, ScreenPadding, maxX);
            _definitionsWindowRect.y = Mathf.Clamp(_definitionsWindowRect.y, ScreenPadding, maxY);
        }

        [HideFromIl2Cpp]
        private void HandleResize(Rect resizeHandleRect)
        {
            Event evt = Event.current;
            if (evt == null)
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0 && resizeHandleRect.Contains(evt.mousePosition))
            {
                _isResizing = true;
                _resizeStartMouse = evt.mousePosition;
                _resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseDrag && _isResizing)
            {
                Vector2 delta = evt.mousePosition - _resizeStartMouse;
                float availableWidth = Mathf.Max(360f, Screen.width - (ScreenPadding * 2f));
                float availableHeight = Mathf.Max(320f, Screen.height - (ScreenPadding * 2f));
                float minWidth = Mathf.Min(MinWindowWidth, availableWidth);
                float minHeight = Mathf.Min(MinWindowHeight, availableHeight);
                float maxWidth = Mathf.Max(minWidth, Screen.width - (_windowRect.x + ScreenPadding));
                float maxHeight = Mathf.Max(minHeight, Screen.height - (_windowRect.y + ScreenPadding));
                _windowRect.width = Mathf.Clamp(_resizeStartSize.x + delta.x, minWidth, maxWidth);
                _windowRect.height = Mathf.Clamp(_resizeStartSize.y + delta.y, minHeight, maxHeight);
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseUp && _isResizing)
            {
                _isResizing = false;
                evt.Use();
            }
        }
    }
}

