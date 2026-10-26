using System.Diagnostics.CodeAnalysis;
using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;

namespace Drova_Modding_API.Systems.Dialogues.Store
{
    /// <summary>
    /// A store for dialogues.
    /// </summary>
    public static class DialogueStore
    {
        private const string DialogueFolderName = "NpcDialogues";

        private static bool _isInitialized;

        private static readonly HashSet<int> PatchedTrees = [];

        /// <summary>
        /// Gets the absolute path where custom dialogue files are stored.
        /// </summary>
        public static string GetDialogueFolderPath()
            => Path.Combine(Utils.SavePath, DialogueFolderName);

        /// <summary>
        /// Gets the absolute path for one dialogue id.
        /// </summary>
        public static string GetDialogueFilePath(string dialogueId)
            => GraphStore.GetFilePath(GetDialogueFolderPath(), dialogueId);

        /// <summary>
        /// Saves a dialogue.
        /// </summary>
        public static bool SaveDialogue(string dialogueId, DialogueTree dialogue)
            => GraphStore.Save(GetDialogueFolderPath(), dialogueId, dialogue);

        /// <summary>
        /// Patches the loaded <see cref="DialogueTree"/> with the bytes stored under <paramref name="dialogueId"/>.
        /// The live object is mutated in place, so existing NPC references see the change.
        /// </summary>
        /// <returns><c>true</c> if a matching in-memory instance was found and patched.</returns>
        public static bool TryPatchInMemoryDialogue(string dialogueId)
            => GraphStore.TryPatchInMemory<DialogueTree>(GetDialogueFolderPath(), dialogueId);

        /// <summary>
        /// Tries to load a dialogue by id from the custom dialogue store.
        /// </summary>
        public static bool TryLoadDialogue(string dialogueId, [NotNullWhen(true)] out DialogueTree? dialogue)
            => GraphStore.TryLoad(GetDialogueFolderPath(), dialogueId, out dialogue);

        /// <summary>
        /// Patches every stored dialogue into its loaded <see cref="DialogueTree"/>. Call once when the main menu has loaded.
        /// </summary>
        public static void PatchAllSavedDialogues()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            int patched = GraphStore.PatchAll<DialogueTree>(GetDialogueFolderPath(), PatchedTrees);
            MelonLogger.Msg($"PatchAllSavedDialogues: patched {patched} dialogue tree(s).");
        }

        /// <summary>
        /// Patches a dialogue tree that was not in memory at the main menu, on its first use.
        /// Most NPC trees load with their NPC, after <see cref="PatchAllSavedDialogues"/> ran.
        /// </summary>
        internal static void PatchOnFirstUse(DialogueTree tree)
            => GraphStore.PatchIfStored(GetDialogueFolderPath(), tree, PatchedTrees);
    }
}
