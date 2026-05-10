using Il2CppNodeCanvas.DialogueTrees;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Relocates dialogue trees.
    /// </summary>
    public class DialogueTreeRelocator : IObjectRelocator<DialogueTree>
    {
        /// <inheritdoc/>
        public string Key => "DialogueTree";

        /// <inheritdoc/>
        public class SaveData : RelocatorSaveData
        {
            /// <summary>
            /// The dialogue key <see cref="Il2CppNodeCanvas.Framework.Graph.Key"/>.
            /// </summary>
            public string DialogueKey;
        }

        private readonly DialogueTree[] _dialogueTrees;

        /// <summary>
        /// Creates a new dialogue tree relocator.
        /// </summary>
        public DialogueTreeRelocator()
        {
            _dialogueTrees = Resources.FindObjectsOfTypeAll<DialogueTree>();
        }

        /// <inheritdoc/>
        public DialogueTree DeserializeObjectFromJson(string json)
        {
            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
            return _dialogueTrees.Where(e => e.Key == saveData.DialogueKey).FirstOrDefault();
        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(DialogueTree objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData() { Key = Key, DialogueKey = objectToSerialize.Key });
        }
    }
}
