using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using System.Text.Json;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Locates global variable lists.
    /// </summary>
    public class GVarListRelocator : IObjectRelocator<GVarList>
    {
        /// <inheritdoc/>
        public string Key => "GVarList";

        /// <summary>
        /// Data to save.
        /// </summary>
        public class SaveData : RelocatorSaveData
        {
            /// <summary>
            /// The global variable list key. <see cref="GVarList.Guid"/>
            /// </summary>
            public string GVarListGuid;
        }

        /// <inheritdoc/>
        public GVarList DeserializeObjectFromJson(string json)
        {
            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
            return ProviderAccess.GVarDatabase.GetGVarListByGuid(saveData.GVarListGuid);
        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(GVarList objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData() { Key = Key, GVarListGuid = objectToSerialize.Guid });

        }
    }
}
