using Drova_Modding_API.Access;
using Il2CppDrova;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    [RegisterTypeInIl2Cpp]
    internal class NpcWizardUI(IntPtr ptr) : MonoBehaviour(ptr)
    {
        #region Fields
        private static NpcWizardUI? _instance;
        private const string InitialStatusMessage = "Use cheat command 'npc_wizard' to open this window.";

        private bool _visible;
        private bool _initialPlayerPositionApplied;
        private readonly NpcWizardWindowLayout _windowLayout = new();
        private readonly NpcWizardDefinitionsBrowser _definitionsBrowser = new();
        private Vector2 _scroll;
        private bool _definitionsVisible;
        private readonly Dictionary<string, bool> _moduleExpandedByKey = new(StringComparer.OrdinalIgnoreCase);
        private readonly NpcWizardDialogueService _dialogueService = new();
        private readonly NpcWizardOverwriteState<PendingOverwriteAction> _overwriteState = new();
        private readonly NpcWizardState _state = new(
            NpcWizardDefinitionHelpers.CreateDefaultDefinitionAtPlayerPosition(),
            InitialStatusMessage);
        private bool _gameplayInputBlockedByWizard;
        private object? _pendingRespawnRoutine;
        private ExternalNpcPlacementSystem.ExternalNpcDefinition? _pendingRespawnDefinition;
        private bool _compactLayoutActiveForDialogueEditor;
        private bool _dialogueEditorEventsSubscribed;
        #endregion

        #region State Proxies
        private ExternalNpcPlacementSystem.ExternalNpcDefinition Definition
        {
            get => _state.Definition;
            set => _state.Definition = value;
        }

        private string Status
        {
            get => _state.Status;
            set => _state.Status = value;
        }

        private string PositionXInput
        {
            get => _state.PositionXInput;
            set => _state.PositionXInput = value;
        }

        private string PositionYInput
        {
            get => _state.PositionYInput;
            set => _state.PositionYInput = value;
        }
        #endregion

        #region Types
        private enum PendingOverwriteAction
        {
            SaveDefinition,
            SaveDefinitionAndSpawn,
            SaveDialogue
        }
        #endregion

        #region Unity Lifecycle
        internal void Awake()
        {
            _instance = this;
            ExternalNpcModuleRegistry.EnsureDefaults(Definition);
            TrySyncDefinitionPositionFromPlayer();
            SyncPositionInputs();
        }

        internal void OnDestroy()
        {
            UnsubscribeFromDialogueEditorEvents();
            ApplyDialogueEditorLayoutIfNeeded(false);
            _windowLayout.PersistNow();
            SetGameplayInputBlocked(false);
            CancelPendingRespawn();

            if (_instance == this)
                _instance = null;
        }

        internal void OnGUI()
        {
            if (!_visible)
                return;

            _windowLayout.EnsureMainWindowRectFitsScreen();
            _windowLayout.SetMainWindowRect(GUI.Window(487319, _windowLayout.MainWindowRect, new Action<int>(DrawWindow), "NPC Wizard"));

            if (_definitionsVisible)
            {
                _windowLayout.EnsureDefinitionsWindowRectFitsScreen();
                _windowLayout.SetDefinitionsWindowRect(GUI.Window(487320, _windowLayout.DefinitionsWindowRect, new Action<int>(DrawDefinitionsWindow), "NPC Definitions"));
            }
        }
        #endregion

        #region Visibility and Input
        [HideFromIl2Cpp]
        public static void Toggle()
        {
            if (_instance == null)
                return;

            _instance.SetWizardVisible(!_instance._visible);
            if (_instance._visible)
            {
                ExternalNpcModuleRegistry.EnsureDefaults(_instance.Definition);
                ReadableIdModuleSupport.WarmUpCache();
                _instance.TrySyncDefinitionPositionFromPlayer();
                _instance.SyncPositionInputs();
                _instance.RefreshDefinitionsCache();
                _instance.Status = $"Placement folder: {ExternalNpcPlacementSystem.GetNpcPlacementFolderPath()}  |  Wizard file: {ExternalNpcPlacementSystem.GetWizardFilePath()}";
            }
        }

        [HideFromIl2Cpp]
        private void SetWizardVisible(bool visible)
        {
            _visible = visible;
            if (_visible)
            {
                SubscribeToDialogueEditorEvents();
                ApplyDialogueEditorLayoutIfNeeded(_dialogueService.IsDialogueEditorOpen());
            }

            if (!_visible)
            {
                UnsubscribeFromDialogueEditorEvents();
                ApplyDialogueEditorLayoutIfNeeded(false);
                _windowLayout.PersistNow();
                _definitionsVisible = false;
                CancelPendingRespawn();
            }

            SetGameplayInputBlocked(_visible);
        }

        [HideFromIl2Cpp]
        private void SetGameplayInputBlocked(bool blocked, bool forceApply = false)
        {
            if (!forceApply && _gameplayInputBlockedByWizard == blocked)
                return;

            InputAccess.ToggleGameplayActionMaps(!blocked);
            _gameplayInputBlockedByWizard = blocked;
        }

        [HideFromIl2Cpp]
        private void ApplyDialogueEditorLayoutIfNeeded(bool dialogueEditorOpen)
        {
            if (dialogueEditorOpen)
            {
                if (_compactLayoutActiveForDialogueEditor)
                    return;

                _windowLayout.EnterCompactDialogueLayout();
                _compactLayoutActiveForDialogueEditor = true;
                return;
            }

            if (!_compactLayoutActiveForDialogueEditor)
                return;

            _windowLayout.ExitCompactDialogueLayout();
            _compactLayoutActiveForDialogueEditor = false;
        }

        [HideFromIl2Cpp]
        private void SubscribeToDialogueEditorEvents()
        {
            if (_dialogueEditorEventsSubscribed)
                return;

            _dialogueService.DialogueEditorOpened += HandleDialogueEditorOpened;
            _dialogueService.DialogueEditorClosed += HandleDialogueEditorClosed;
            _dialogueEditorEventsSubscribed = true;
        }

        [HideFromIl2Cpp]
        private void UnsubscribeFromDialogueEditorEvents()
        {
            if (!_dialogueEditorEventsSubscribed)
                return;

            _dialogueService.DialogueEditorOpened -= HandleDialogueEditorOpened;
            _dialogueService.DialogueEditorClosed -= HandleDialogueEditorClosed;
            _dialogueEditorEventsSubscribed = false;
        }

        [HideFromIl2Cpp]
        private void HandleDialogueEditorOpened()
        {
            if (!_visible)
                return;

            ApplyDialogueEditorLayoutIfNeeded(true);
        }

        [HideFromIl2Cpp]
        private void HandleDialogueEditorClosed()
        {
            if (!_visible)
                return;

            ApplyDialogueEditorLayoutIfNeeded(false);
            SetGameplayInputBlocked(true, forceApply: true);
        }
        #endregion

        #region Main Window
        [HideFromIl2Cpp]
        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(_windowLayout.MainWindowRect.height - 150f));

            DrawBaseFields();
            DrawModuleFields();

            GUILayout.EndScrollView();

            if (!string.IsNullOrWhiteSpace(Status))
                GUILayout.Label(Status);

            if (HasPendingOverwrite())
                DrawOverwriteConfirmationPrompt();
            else
                DrawActionButtons();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset Size", GUILayout.Width(110f)))
                _windowLayout.ResetMainWindowRectToScreen();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            Rect resizeHandleRect = new(_windowLayout.MainWindowRect.width - 24f, _windowLayout.MainWindowRect.height - 24f, 18f, 18f);
            GUI.Box(resizeHandleRect, "");
            _windowLayout.HandleMainResize(resizeHandleRect);

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        [HideFromIl2Cpp]
        private void DrawBaseFields()
        {
            GUILayout.Label("Base Definition");
            GUILayout.Label("Definition Id");
            Definition.Id = GUILayout.TextField(Definition.Id);

            GUILayout.Label("Display Name");
            Definition.Name = GUILayout.TextField(Definition.Name);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Position X", GUILayout.Width(140f));
            PositionXInput = GUILayout.TextField(PositionXInput);
            GUILayout.Label("Position Y", GUILayout.Width(140f));
            PositionYInput = GUILayout.TextField(PositionYInput);
            GUILayout.EndHorizontal();

            if (float.TryParse(PositionXInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x))
                Definition.PositionX = x;
            if (float.TryParse(PositionYInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                Definition.PositionY = y;

            Definition.Enabled = GUILayout.Toggle(Definition.Enabled, "Enabled for auto spawn on game start");
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

                if (!Definition.ModuleConfig.TryGetValue(module.Key, out string? payload))
                {
                    payload = module.CreateDefaultPayload();
                    Definition.ModuleConfig[module.Key] = payload;
                }

                if (expanded)
                {
                    payload ??= module.CreateDefaultPayload();
                    Definition.ModuleConfig[module.Key] = module.DrawWizardUiAndSerialize(payload);

                    if (string.Equals(module.Key, ExternalDialogueModule.ModuleKey, StringComparison.OrdinalIgnoreCase))
                        DrawDialogueModuleActions();
                }
                GUILayout.EndVertical();
            }
        }
        #endregion

        [HideFromIl2Cpp]
        private void DrawDialogueModuleActions()
        {
            bool isCurrentDefinitionSpawned = ExternalNpcPlacementSystem.IsDefinitionSpawned(Definition.Id);

            GUILayout.Space(4f);
            if (!isCurrentDefinitionSpawned)
                GUILayout.Label("Dialogue actions require a spawned NPC.");

            GUILayout.BeginHorizontal();
            GUI.enabled = isCurrentDefinitionSpawned && !HasPendingOverwrite();

            if (GUILayout.Button("Edit Graph", GUILayout.Width(120f)))
            {
                if (TryOpenDialogueEditorForCurrentDefinition())
                    Status = "Opened dialogue editor for spawned NPC.";
            }

            if (GUILayout.Button("Save To Store", GUILayout.Width(120f)))
            {
                if (TrySaveDialogueForCurrentDefinition())
                    Status = "Saved spawned NPC dialogue to dialogue store.";
            }

            if (GUILayout.Button("Load To Spawned", GUILayout.Width(130f)))
            {
                if (TryLoadDialogueForCurrentDefinition())
                    Status = "Loaded stored dialogue into spawned NPC.";
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
            bool isCurrentDefinitionSpawned = ExternalNpcPlacementSystem.IsDefinitionSpawned(Definition.Id);

            if (GUILayout.Button("New"))
            {
                CancelPendingRespawn();
                Definition = NpcWizardDefinitionHelpers.CreateDefaultDefinitionAtPlayerPosition();
                MarkCurrentDefinitionAsUnloaded();
                SyncPositionInputs();
                Status = "Created a fresh definition template.";
            }

            if (GUILayout.Button("Load By Id"))
            {
                CancelPendingRespawn();
                if (ExternalNpcPlacementSystem.TryGetDefinition(Definition.Id, out ExternalNpcPlacementSystem.ExternalNpcDefinition? found) && found != null)
                {
                    Definition = found;
                    ExternalNpcModuleRegistry.EnsureDefaults(Definition);
                    MarkCurrentDefinitionAsLoaded();
                    ClearDialogueLoadedMarker();
                    SyncPositionInputs();
                    Status = $"Loaded definition '{Definition.Id}'.";
                }
                else
                {
                    Status = $"Definition '{Definition.Id}' was not found.";
                }
            }

            if (GUILayout.Button("Save Definition"))
            {
                if (!ValidateForSave())
                    return;

                CancelPendingRespawn();
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
                CancelPendingRespawn();
                bool despawned = ExternalNpcPlacementSystem.DespawnDefinition(Definition.Id);
                Status = despawned
                    ? $"Despawned definition '{Definition.Id}'."
                    : $"Definition '{Definition.Id}' is not currently spawned.";
                RefreshDefinitionsCache();
            }
            GUI.enabled = true;

            if (GUILayout.Button(_definitionsVisible ? "Hide Definitions" : "Open Definitions"))
            {
                _definitionsVisible = !_definitionsVisible;
                if (_definitionsVisible)
                    _definitionsBrowser.Refresh();
            }

            if (GUILayout.Button("Close"))
            {
                SetWizardVisible(false);
            }

            GUILayout.EndHorizontal();
        }

        #region Definitions Browser
        [HideFromIl2Cpp]
        private void DrawDefinitionsWindow(int id)
        {
            bool keepOpen = _definitionsBrowser.DrawWindow(
                LoadDefinitionFromBrowser,
                status => Status = status);
            if (!keepOpen)
                _definitionsVisible = false;

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        [HideFromIl2Cpp]
        private void RefreshDefinitionsCache()
        {
            _definitionsBrowser.Refresh();
        }
        #endregion

        #region Validation
        [HideFromIl2Cpp]
        private bool ValidateForSave()
        {
            bool valid = NpcWizardDefinitionHelpers.ValidateForSave(Definition, out string status);
            if (!valid)
                Status = status;

            return valid;
        }
        #endregion

        #region Dialogue Actions
        [HideFromIl2Cpp]
        private bool TryOpenDialogueEditorForCurrentDefinition()
        {
            if (!TryGetCurrentDialogueId(out string dialogueId))
                return false;
            return _dialogueService.TryOpenDialogueEditor(
                Definition.Id,
                dialogueId,
                status => Status = status,
                MarkCurrentDialogueAsLoaded,
                ClearDialogueLoadedMarker);
        }

        [HideFromIl2Cpp]
        private bool TrySaveDialogueForCurrentDefinition()
        {
            if (HasPendingOverwrite())
            {
                Status = "Confirm or cancel the pending overwrite first.";
                return false;
            }

            return TrySaveDialogueForCurrentDefinitionInternal(overwriteConfirmed: false);
        }

        [HideFromIl2Cpp]
        private bool TrySaveDialogueForCurrentDefinitionInternal(bool overwriteConfirmed)
        {
            if (!TryGetCurrentDialogueId(out string dialogueId))
                return false;

            if (!overwriteConfirmed && RequiresDialogueOverwriteConfirmation(dialogueId))
            {
                QueueOverwriteConfirmation(
                    PendingOverwriteAction.SaveDialogue,
                    $"Dialogue '{dialogueId}' already exists in store and is not currently loaded. Overwrite existing dialogue configuration?");
                return false;
            }

            return NpcWizardDialogueService.TrySaveDialogue(
                Definition.Id,
                dialogueId,
                status => Status = status,
                MarkCurrentDialogueAsLoaded);
        }

        [HideFromIl2Cpp]
        private bool TryLoadDialogueForCurrentDefinition()
        {
            if (!TryGetCurrentDialogueId(out string dialogueId))
                return false;

            return NpcWizardDialogueService.TryLoadDialogue(
                Definition.Id,
                dialogueId,
                status => Status = status,
                MarkCurrentDialogueAsLoaded);
        }
        #endregion

        #region Overwrite Flow
        [HideFromIl2Cpp]
        private bool HasPendingOverwrite()
            => _overwriteState.HasPending;

        [HideFromIl2Cpp]
        private void DrawOverwriteConfirmationPrompt()
        {
            GUILayout.Space(6f);
            GUILayout.BeginVertical("box");
            GUILayout.Label(_overwriteState.Message);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Overwrite", GUILayout.Width(130f)))
                ExecutePendingOverwrite();

            if (GUILayout.Button("Cancel", GUILayout.Width(130f)))
            {
                ClearPendingOverwrite();
                Status = "Overwrite canceled.";
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void QueueOverwriteConfirmation(PendingOverwriteAction action, string message)
        {
            _overwriteState.Queue(action, message);
            Status = "Overwrite confirmation required.";
        }

        [HideFromIl2Cpp]
        private void ClearPendingOverwrite()
        {
            _overwriteState.Clear();
        }

        [HideFromIl2Cpp]
        private void ExecutePendingOverwrite()
        {
            if (!_overwriteState.TryConsume(out PendingOverwriteAction action))
                return;

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
                        Status = "Saved spawned NPC dialogue to dialogue store.";
                    break;
            }
        }

        [HideFromIl2Cpp]
        private void TrySaveDefinitionWithOptionalConfirmation(bool spawnAfterSave)
        {
            if (HasPendingOverwrite())
            {
                Status = "Confirm or cancel the pending overwrite first.";
                return;
            }

            if (RequiresDefinitionOverwriteConfirmation())
            {
                QueueOverwriteConfirmation(
                    spawnAfterSave ? PendingOverwriteAction.SaveDefinitionAndSpawn : PendingOverwriteAction.SaveDefinition,
                    $"Definition '{Definition.Id}' already exists and is not currently loaded. Overwrite existing configuration?");
                return;
            }

            SaveDefinitionInternal(spawnAfterSave);
        }
        #endregion

        #region Definition Save and Overwrite Checks
        [HideFromIl2Cpp]
        private void SaveDefinitionInternal(bool spawnAfterSave)
        {
            bool saved = ExternalNpcPlacementSystem.UpsertDefinition(Definition);
            if (!saved)
            {
                Status = "Failed to save definition.";
                return;
            }

            MarkCurrentDefinitionAsLoaded();
            if (!spawnAfterSave)
            {
                Status = $"Saved definition '{Definition.Id}'.";
                RefreshDefinitionsCache();
                return;
            }

            if (HasPendingRespawn())
            {
                QueueRespawnNextFrame(CloneDefinition(Definition));
                Status = "Save: ok, Respawn: updated.";
                RefreshDefinitionsCache();
                return;
            }

            bool wasAlreadySpawned = ExternalNpcPlacementSystem.IsDefinitionSpawned(Definition.Id);
            if (wasAlreadySpawned)
            {
                bool despawned = ExternalNpcPlacementSystem.DespawnDefinition(Definition.Id);
                if (!despawned)
                {
                    Status = $"Save: ok, Respawn: failed (could not despawn '{Definition.Id}').";
                    RefreshDefinitionsCache();
                    return;
                }

                QueueRespawnNextFrame(CloneDefinition(Definition));
                Status = "Save: ok, Respawn: queued.";
                RefreshDefinitionsCache();
                return;
            }

            bool spawned = ExternalNpcPlacementSystem.SpawnDefinition(Definition, skipIfAlreadySpawned: false, requireEnabled: false);
            Status = $"Save: ok, Spawn: {(spawned ? "spawned" : "failed")}.";
            RefreshDefinitionsCache();
        }

        [HideFromIl2Cpp]
        private void QueueRespawnNextFrame(ExternalNpcPlacementSystem.ExternalNpcDefinition definitionToSpawn)
        {
            CancelPendingRespawn();

            _pendingRespawnDefinition = definitionToSpawn;
            _pendingRespawnRoutine = MelonCoroutines.Start(RespawnNextFrameCoroutine());
        }

        [HideFromIl2Cpp]
        private bool HasPendingRespawn()
            => _pendingRespawnRoutine != null;

        [HideFromIl2Cpp]
        private void CancelPendingRespawn()
        {
            if (_pendingRespawnRoutine != null)
                MelonCoroutines.Stop(_pendingRespawnRoutine);

            _pendingRespawnRoutine = null;
            _pendingRespawnDefinition = null;
        }

        [HideFromIl2Cpp]
        private IEnumerator RespawnNextFrameCoroutine()
        {
            // Destroy() completes at end-of-frame, so respawn on the next frame.
            yield return null;

            ExternalNpcPlacementSystem.ExternalNpcDefinition? definitionToSpawn = _pendingRespawnDefinition;
            _pendingRespawnDefinition = null;

            if (definitionToSpawn == null)
            {
                _pendingRespawnRoutine = null;
                yield break;
            }

            bool spawned = ExternalNpcPlacementSystem.SpawnDefinition(definitionToSpawn, skipIfAlreadySpawned: false, requireEnabled: false);
            Status = $"Save: ok, Respawn: {(spawned ? "ok" : "failed")}.";
            RefreshDefinitionsCache();
            _pendingRespawnRoutine = null;
        }

        [HideFromIl2Cpp]
        private static ExternalNpcPlacementSystem.ExternalNpcDefinition CloneDefinition(ExternalNpcPlacementSystem.ExternalNpcDefinition source)
            => new()
            {
                Id = source.Id,
                Name = source.Name,
                PositionX = source.PositionX,
                PositionY = source.PositionY,
                Enabled = source.Enabled,
                ModuleConfig = new Dictionary<string, string>(source.ModuleConfig, StringComparer.OrdinalIgnoreCase)
            };

        [HideFromIl2Cpp]
        private bool RequiresDefinitionOverwriteConfirmation()
        {
            if (string.IsNullOrWhiteSpace(Definition.Id))
                return false;

            if (!ExternalNpcPlacementSystem.TryGetDefinition(Definition.Id, out ExternalNpcPlacementSystem.ExternalNpcDefinition? existingDefinition) || existingDefinition == null)
                return false;

            return !_state.IsDefinitionLoadedFromCurrentSession();
        }

        [HideFromIl2Cpp]
        private bool RequiresDialogueOverwriteConfirmation(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
                return false;

            if (!DialogueExistsInStore(dialogueId))
                return false;

            return !_state.IsDialogueLoadedForCurrentDefinition(dialogueId);
        }
        #endregion

        #region State Helpers
        [HideFromIl2Cpp]
        private static bool DialogueExistsInStore(string dialogueId)
            => NpcWizardDialogueService.DialogueExistsInStore(dialogueId);

        [HideFromIl2Cpp]
        private void MarkCurrentDefinitionAsLoaded()
            => _state.MarkCurrentDefinitionAsLoaded();

        [HideFromIl2Cpp]
        private void MarkCurrentDefinitionAsUnloaded()
            => _state.MarkCurrentDefinitionAsUnloaded();

        [HideFromIl2Cpp]
        private void MarkCurrentDialogueAsLoaded(string dialogueId)
            => _state.MarkCurrentDialogueAsLoaded(dialogueId);

        [HideFromIl2Cpp]
        private void ClearDialogueLoadedMarker()
            => _state.ClearDialogueLoadedMarker();

        [HideFromIl2Cpp]
        private bool TryGetCurrentDialogueId(out string dialogueId)
        {
            dialogueId = string.Empty;
            if (!Definition.ModuleConfig.TryGetValue(ExternalDialogueModule.ModuleKey, out string? payload))
            {
                Status = "Dialogue module payload was not found on this definition.";
                return false;
            }

            dialogueId = ExternalDialogueModule.GetDialogueId(payload);
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                Status = "Set a Dialogue Id in the Dialogue module first.";
                return false;
            }

            return true;
        }

        [HideFromIl2Cpp]
        private void SyncPositionInputs()
            => _state.SyncPositionInputs();

        [HideFromIl2Cpp]
        private void TrySyncDefinitionPositionFromPlayer()
        {
            if (_initialPlayerPositionApplied)
                return;

            if (!PlayerAccess.TryGetPlayer(out Actor player) || player == null)
                return;

            Vector3 playerPosition = player.transform.position;
            Definition.PositionX = playerPosition.x;
            Definition.PositionY = playerPosition.y;
            _initialPlayerPositionApplied = true;
        }

        [HideFromIl2Cpp]
        private void LoadDefinitionFromBrowser(ExternalNpcPlacementSystem.ExternalNpcDefinition definition)
        {
            Definition = definition;
            ExternalNpcModuleRegistry.EnsureDefaults(Definition);
            MarkCurrentDefinitionAsLoaded();
            ClearDialogueLoadedMarker();
            SyncPositionInputs();
            Status = $"Loaded definition '{Definition.Id}' from browser.";
        }
        #endregion

    }
}

