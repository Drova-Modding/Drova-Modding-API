using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using System.Text.Json;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Relocates global variable integers.
    /// </summary>
    public class GVarIntRelocator : IObjectRelocator<GInt>
    {
        /// <inheritdoc/>
        public string Key => "Gint";

        /// <summary>
        /// Data to save.
        /// </summary>
        public class SaveData : RelocatorSaveData
        {
            /// <summary>
            /// The global variable list key. <see cref="GVarList.Guid"/>
            /// </summary>
            public string GvarListGuid;
            /// <summary>
            /// The global variable integer key. <see cref="AGVarBase.GetGVarId"/>
            /// </summary>
            public string GVarIntGuid;
        }

        /// <inheritdoc/>

        public GInt DeserializeObjectFromJson(string json)
        {

            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
            GVarList gvarList = ProviderAccess.GVarDatabase.GetGVarListByGuid(saveData.GvarListGuid);
            return gvarList.GetGVarById(saveData.GVarIntGuid).TryCast<GInt>();
        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(GInt objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData() { Key = Key, GvarListGuid = objectToSerialize.GetParent().Guid, GVarIntGuid = objectToSerialize.GetGVarId() });

        }
    }
}
