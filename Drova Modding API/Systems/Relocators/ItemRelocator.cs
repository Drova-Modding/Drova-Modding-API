using Drova_Modding_API.Access;
using Il2CppDrova.Items;
using System.Text.Json;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Relocates items.
    /// </summary>
    public class ItemRelocator : IObjectRelocator<Item>
    {
        /// <inheritdoc/>
        public string Key => "Item";

        /// <summary>
        /// Data to save.
        /// </summary>
        public class SaveData : RelocatorSaveData
        {
            /// <summary>
            /// The item guid <see cref="Item.Guid"/>
            /// </summary>
            public string ItemGuid;
        }

        /// <inheritdoc/>
        public Item DeserializeObjectFromJson(string json)
        {
            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
            return ProviderAccess.ItemDatabase.GetItemByGuid(saveData.ItemGuid);
        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(Item objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData() { Key = Key, ItemGuid = objectToSerialize.Guid });
        }
    }
}
