using Drova_Modding_API.Systems.Dialogues.Store;
using Il2CppNodeCanvas.DialogueTrees;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Applies a stored custom dialogue tree to an NPC's dialogue controller.
    /// </summary>
    internal sealed class DialoguePresetModule(string dialogueId) : INpcModule
    {
        private readonly DialogueStore _dialogueStore = new();

        public int Priority => 200;

        public void Apply(ModuleContext context)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
                return;

            DS_DialogueTreeController dialogueTreeController = context.GetComponentInChildren<DS_DialogueTreeController>();
            if (dialogueTreeController == null)
                return;

            if (!_dialogueStore.TryLoadDialogue(dialogueId, out DialogueTree? loadedDialogue) || loadedDialogue == null)
                return;

            loadedDialogue.Serialize(null);
            dialogueTreeController.graph = loadedDialogue;
        }

    }
}



