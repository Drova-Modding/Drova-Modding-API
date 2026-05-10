using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using System.Text.Json;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Relocates GVars of type bool.
    /// </summary>
    public class GVarBoolRelocator : IObjectRelocator<GBool>
    {
        /// <inheritdoc/>
        public string Key => "GBool";

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
            public string GVarBoolGuid;
        }

        /// <inheritdoc/>
        public GBool DeserializeObjectFromJson(string json)
        {
            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
            GVarList gvarList = ProviderAccess.GVarDatabase.GetGVarListByGuid(saveData.GvarListGuid);
            return gvarList.GetGVarById(saveData.GVarBoolGuid).TryCast<GBool>();

        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(GBool objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData() { Key = Key, GvarListGuid = objectToSerialize.GetParent().Guid, GVarBoolGuid = objectToSerialize.GetGVarId() });
        }
    }
}
