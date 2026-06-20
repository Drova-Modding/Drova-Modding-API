using Drova_Modding_API.Systems.Dialogues;
using Drova_Modding_API.Systems.Dialogues.Editor;
using Drova_Modding_API.Systems.Dialogues.Store;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning;

/// <summary>
/// Encapsulates dialogue-related wizard actions (open, load, save) so the UI layer
/// stays focused on input and rendering concerns.
/// </summary>
internal sealed class NpcWizardDialogueService
{
    private const string WizardGraphEditorName = "ModdingAPI_WizardGraphEditor";

    private GraphEditorManager? _graphEditorManager;
    private DialogueEditorLifecycleWatcher? _lifecycleWatcher;

    internal event Action? DialogueEditorOpened;
    internal event Action? DialogueEditorClosed;

    /// <summary>
    /// Ensures the spawned NPC has a dialogue graph and opens the graph editor for it.
    /// </summary>
    internal bool TryOpenDialogueEditor(
        string definitionId,
        string dialogueId,
        Action<string> setStatus,
        Action<string> markCurrentDialogueAsLoaded,
        Action clearDialogueLoadedMarker)
    {
        if (!ExternalNpcPlacementSystem.TryGetSpawnedActor(definitionId, out Actor? actor) || actor == null)
        {
            setStatus("Spawn the current definition first, then open dialogue editor.");
            return false;
        }

        DS_DialogueTreeController controller = actor.GetComponentInChildren<DS_DialogueTreeController>();
        if (controller == null)
        {
            setStatus("Selected NPC has no dialogue controller.");
            return false;
        }

        if (!TryEnsureDialogueGraphForActor(actor, controller, dialogueId, setStatus, markCurrentDialogueAsLoaded, clearDialogueLoadedMarker))
            return false;

        EnsureWizardGraphEditor();
        if (_graphEditorManager == null)
        {
            setStatus("Failed to initialize graph editor.");
            return false;
        }

        _graphEditorManager.gameObject.SetActive(true);
        _graphEditorManager.Init(actor);
        DialogueEditorOpened?.Invoke();
        return true;
    }

    /// <summary>
    /// Saves the currently attached dialogue graph from the spawned NPC into the store.
    /// </summary>
    internal static bool TrySaveDialogue(
        string definitionId,
        string dialogueId,
        Action<string> setStatus,
        Action<string> markCurrentDialogueAsLoaded)
    {
        if (!ExternalNpcPlacementSystem.TryGetSpawnedActor(definitionId, out Actor? actor) || actor == null)
        {
            setStatus("Spawn the current definition first, then save dialogue.");
            return false;
        }

        DS_DialogueTreeController controller = actor.GetComponentInChildren<DS_DialogueTreeController>();
        if (controller == null || controller.graph == null)
        {
            setStatus("Selected NPC has no dialogue graph to save.");
            return false;
        }

        DialogueTree dialogueTree = controller.graph.TryCast<DialogueTree>();
        if (dialogueTree == null)
        {
            setStatus("Current NPC graph is not a DialogueTree.");
            return false;
        }

        bool saved = DialogueStore.SaveDialogue(dialogueId, dialogueTree);
        if (!saved)
        {
            setStatus($"Failed to save dialogue '{dialogueId}'.");
            return false;
        }

        markCurrentDialogueAsLoaded(dialogueId);
        return true;
    }

    /// <summary>
    /// Loads a stored dialogue graph and assigns it to the spawned NPC.
    /// </summary>
    internal static bool TryLoadDialogue(
        string definitionId,
        string dialogueId,
        Action<string> setStatus,
        Action<string> markCurrentDialogueAsLoaded)
    {
        if (!ExternalNpcPlacementSystem.TryGetSpawnedActor(definitionId, out Actor? actor) || actor == null)
        {
            setStatus("Spawn the current definition first, then load dialogue.");
            return false;
        }

        DS_DialogueTreeController controller = actor.GetComponentInChildren<DS_DialogueTreeController>();
        if (controller == null)
        {
            setStatus("Selected NPC has no dialogue controller.");
            return false;
        }

        if (!DialogueStore.TryLoadDialogue(dialogueId, out DialogueTree? loadedDialogue) || loadedDialogue == null)
        {
            setStatus($"No stored dialogue found for '{dialogueId}'.");
            return false;
        }

        loadedDialogue.Serialize(null);
        controller.graph = loadedDialogue;
        markCurrentDialogueAsLoaded(dialogueId);
        return true;
    }

    internal static bool DialogueExistsInStore(string dialogueId)
        => File.Exists(DialogueStore.GetDialogueFilePath(dialogueId));

    internal bool IsDialogueEditorOpen()
        => _graphEditorManager != null
           && _graphEditorManager.gameObject != null
           && _graphEditorManager.gameObject.activeSelf;

    private static bool TryEnsureDialogueGraphForActor(
        Actor actor,
        DS_DialogueTreeController controller,
        string dialogueId,
        Action<string> setStatus,
        Action<string> markCurrentDialogueAsLoaded,
        Action clearDialogueLoadedMarker)
    {
        if (controller.graph != null)
            return true;

        if (DialogueStore.TryLoadDialogue(dialogueId, out DialogueTree? loadedDialogue) && loadedDialogue != null)
        {
            loadedDialogue.Serialize(null);
            controller.graph = loadedDialogue;
            markCurrentDialogueAsLoaded(dialogueId);
            setStatus($"Loaded stored dialogue '{dialogueId}'.");
            return true;
        }

        DialogueTree createdDialogue = CreateStarterDialogueTree(actor, dialogueId);
        if (createdDialogue == null)
        {
            setStatus("Failed to create starter dialogue graph.");
            return false;
        }

        createdDialogue.Serialize(null);
        controller.graph = createdDialogue;
        clearDialogueLoadedMarker();
        setStatus($"Created new starter dialogue '{dialogueId}'.");
        return true;
    }

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
        DS_Statement statementEnd = new()
        {
            useGlobalLoca = true,
            GlobalLocaPath = "Modding_API/NpcWizard",
            locaKey = "StarterLine"
        };
        endNode.availableChoices.Add(new DS_MultipleChoiceNode.Choice
        {
            statement = statementEnd,
            isEndNode = true,
            UID = Il2CppSystem.Guid.NewGuid().ToString()
        });
        endNode.TryGenerateUID();

        tree.ConnectNodes(startNode, endNode);
        tree.primeNode = startNode;
        tree.name = dialogueId;
        return tree;
    }

    private void EnsureWizardGraphEditor()
    {
        if (_graphEditorManager != null)
        {
            EnsureLifecycleWatcher(_graphEditorManager.gameObject);
            return;
        }

        GameObject graphEditorRoot = GameObject.Find(WizardGraphEditorName);
        if (graphEditorRoot == null)
        {
            graphEditorRoot = new GameObject(WizardGraphEditorName);
            UnityEngine.Object.DontDestroyOnLoad(graphEditorRoot);
        }

        _graphEditorManager = graphEditorRoot.GetComponent<GraphEditorManager>();
        if (_graphEditorManager == null)
            _graphEditorManager = graphEditorRoot.AddComponent<GraphEditorManager>();

        EnsureLifecycleWatcher(graphEditorRoot);
    }

    private void EnsureLifecycleWatcher(GameObject graphEditorRoot)
    {
        DialogueEditorLifecycleWatcher watcher = graphEditorRoot.GetComponent<DialogueEditorLifecycleWatcher>();
        if (watcher == null)
            watcher = graphEditorRoot.AddComponent<DialogueEditorLifecycleWatcher>();

        if (!ReferenceEquals(_lifecycleWatcher, watcher))
        {
            if (_lifecycleWatcher != null)
                _lifecycleWatcher.Closed -= HandleEditorClosed;

            _lifecycleWatcher = watcher;
            _lifecycleWatcher!.Closed += HandleEditorClosed;
        }
    }

    private void HandleEditorClosed()
    {
        DialogueEditorClosed?.Invoke();
    }
}