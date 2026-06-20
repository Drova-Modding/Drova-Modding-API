using Drova_Modding_API.Systems.Dialogues.Store;
using Il2CppNodeCanvas.DialogueTrees;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Applies a stored custom dialogue tree to an NPCs dialogue controller.
    /// </summary>
    public sealed class DialogueModule(string dialogueId) : INpcModule
    {
        
        /// <inheritdoc/>
        public int Priority => 200;

        /// <inheritdoc/>
        public void Apply(ModuleContext context)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
                return;

            DS_DialogueTreeController dialogueTreeController = context.GetComponentInChildren<DS_DialogueTreeController>();
            if (dialogueTreeController == null)
                return;

            if (!DialogueStore.TryLoadDialogue(dialogueId, out DialogueTree? loadedDialogue) || loadedDialogue == null)
                return;

            loadedDialogue.Serialize(null);
            dialogueTreeController.graph = loadedDialogue;
        }
    }
}



