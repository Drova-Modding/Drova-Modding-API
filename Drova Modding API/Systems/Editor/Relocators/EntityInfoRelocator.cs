using Il2CppDrova;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <inheritdoc/>
    public class EntityInfoRelocator : IObjectRelocator<EntityInfo>
    {
        /// <inheritdoc/>
        public string Key => "EntityInfo";

        /// <summary>
        /// Data to save.
        /// </summary>
        public class SaveData : RelocatorSaveData
        {
            /// <summary>
            /// The entity info guid
            /// </summary>
            public string Guid;
        }

        private readonly EntityInfo[] _entityInfos;

        /// <summary>
        /// Creates a new entity info relocator.
        /// </summary>
        public EntityInfoRelocator()
        {
            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>();
        }

        /// <inheritdoc/>
        public string SerializeObjectToJson(EntityInfo objectToSerialize)
        {
            return JsonSerializer.Serialize(new SaveData() { Key = Key, Guid = objectToSerialize._guid });
        }

        /// <inheritdoc/>
        public EntityInfo DeserializeObjectFromJson(string json)
        {
            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
            return _entityInfos.Where(e => e.GUID == saveData.Guid).FirstOrDefault();
        }
    }
}
