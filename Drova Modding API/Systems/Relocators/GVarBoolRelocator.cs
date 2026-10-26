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
            public string? GvarListGuid;
            /// <summary>
            /// The global variable bool key. <see cref="AGVarBase.GetGVarId"/>
            /// </summary>
            public string? GVarBoolGuid;
        }

        /// <inheritdoc/>
        public GBool? DeserializeObjectFromJson(string json)
        {
            SaveData? saveData = JsonSerializer.Deserialize<SaveData>(json, RelocatorSaveData.JsonOptions);
            SubDatabase_GVars? database = ProviderAccess.GVarDatabase;
            if (saveData == null || database == null) return null;

            GVarList? gvarList = database.GetGVarListByGuid(saveData.GvarListGuid);
            AGVarBase? gvar = gvarList != null ? gvarList.GetGVarById(saveData.GVarBoolGuid) : null;
            return gvar != null ? gvar.TryCast<GBool>() : null;
        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(GBool objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData() { Key = Key, GvarListGuid = objectToSerialize.GetParent().Guid, GVarBoolGuid = objectToSerialize.GetGVarId() }, RelocatorSaveData.JsonOptions);
        }
    }
}
