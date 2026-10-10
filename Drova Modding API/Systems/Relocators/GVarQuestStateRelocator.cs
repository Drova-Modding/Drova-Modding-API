using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using System.Text.Json;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Relocator for <see cref="GQuestState"/>
    /// </summary>
    public class GVarQuestStateRelocator : IObjectRelocator<GQuestState>
    {
        /// <inheritdoc/>
        public string Key => "GQuestState";

        /// <summary>
        /// Data to save for relocation.
        /// </summary>
        public class SaveData : RelocatorSaveData
        {
            /// <summary>
            /// Guid of the <see cref="GVarList.Guid"/>.
            /// </summary>
            public string? GVarListGuid;
        }

        /// <inheritdoc/>
        public GQuestState? DeserializeObjectFromJson(string json)
        {
            SaveData? data = JsonSerializer.Deserialize<SaveData>(json, RelocatorSaveData.JsonOptions);
            SubDatabase_GVars? database = ProviderAccess.GVarDatabase;
            if (data == null || database == null) return null;

            GVarList? gVarList = database.GetGVarListByGuid(data.GVarListGuid);
            return gVarList != null ? gVarList.GetQuestState() : null;
        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(GQuestState objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData()
            {
                Key = Key,
                GVarListGuid = objectToSerialize.GetParent().Guid
            }, RelocatorSaveData.JsonOptions);
        }
    }
}
