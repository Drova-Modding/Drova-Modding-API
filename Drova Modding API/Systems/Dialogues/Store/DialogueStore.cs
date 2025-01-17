using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework.Internal;
using Il2CppSirenix.Serialization;

namespace Drova_Modding_API.Systems.Dialogues.Store
{
    /// <summary>
    /// A store for dialogues.
    /// </summary>
    public class DialogueStore
    {
        [Serializable]
        private class SaveData
        {
            public byte[] StoredData;
            public byte[] ObjectReferences;
            public string Key;
            public bool IsBuiltInDialog;
        }

        private readonly Dictionary<string, SaveData> _dialogues = new();

        /// <summary>
        /// Saves a dialogue.
        /// </summary>
        public void SaveDialogue(DialogueTree dialogue)
        {
            Il2CppSystem.Collections.Generic.List<UnityEngine.Object> data;
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte> lol = SerializationUtility.SerializeValue<GraphSource>(dialogue.graphSource.Pack(dialogue), DataFormat.JSON, out data, null);

            // Write lol into a json file
            string savePath = Path.Combine(Utils.SavePath, "dialogue.json");
            File.WriteAllBytes(savePath, lol);
        }
    }
}
