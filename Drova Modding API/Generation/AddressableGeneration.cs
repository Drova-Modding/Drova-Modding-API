using MelonLoader;
using System.Text;
using System.Text.Json;
using UnityEngine;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.ResourceManagement.Util;
using static UnityEngine.AddressableAssets.ResourceLocators.ContentCatalogData;

namespace Drova_Modding_API.Generation
{
    /// <summary>
    /// This class is used to generate a file with all addressable assets in the game.
    /// </summary>
    public static class AddressableGeneration
    {

        struct Bucket
        {
            public int dataOffset;
            public int[] entries;
        }

        [Serializable]
        internal class ContentCatalogData
        {
            public string[] m_ProviderIds { get; set; }

            public string[] m_InternalIds { get; set; }
            public string m_KeyDataString { get; set; }

            public string m_BucketDataString { get; set; }

            public string m_EntryDataString { get; set; }

            public string m_ExtraDataString { get; set; }

            public SerializedType[] m_resourceTypes { get; set; }

        }

        internal static object ReadObjectFromByteArray(byte[] keyData, int dataIndex)
        {
            try
            {
                SerializationUtilities.ObjectType keyType = (SerializationUtilities.ObjectType)keyData[dataIndex];
                dataIndex++;
                switch (keyType)
                {
                    case SerializationUtilities.ObjectType.UnicodeString:
                        {
                            int dataLength = BitConverter.ToInt32(keyData, dataIndex);
                            return Encoding.Unicode.GetString(keyData, dataIndex + 4, dataLength);
                        }
                    case SerializationUtilities.ObjectType.AsciiString:
                        {
                            int dataLength = BitConverter.ToInt32(keyData, dataIndex);
                            return Encoding.ASCII.GetString(keyData, dataIndex + 4, dataLength);
                        }
                    case SerializationUtilities.ObjectType.UInt16: return BitConverter.ToUInt16(keyData, dataIndex);
                    case SerializationUtilities.ObjectType.UInt32: return BitConverter.ToUInt32(keyData, dataIndex);
                    case SerializationUtilities.ObjectType.Int32: return BitConverter.ToInt32(keyData, dataIndex);
                    case SerializationUtilities.ObjectType.Hash128: return Hash128.Parse(Encoding.ASCII.GetString(keyData, dataIndex + 1, keyData[dataIndex]));
                    //case SerializationUtilities.ObjectType.Type: return Type.GetTypeFromCLSID(new Guid(Encoding.ASCII.GetString(keyData, dataIndex + 1, keyData[dataIndex])));
                    case SerializationUtilities.ObjectType.JsonObject:
                        {
                            int assemblyNameLength = keyData[dataIndex];
                            dataIndex++;
                            string assemblyName = Encoding.ASCII.GetString(keyData, dataIndex, assemblyNameLength);
                            dataIndex += assemblyNameLength;

                            int classNameLength = keyData[dataIndex];
                            dataIndex++;
                            string className = Encoding.ASCII.GetString(keyData, dataIndex, classNameLength);
                            dataIndex += classNameLength;
                            int jsonLength = BitConverter.ToInt32(keyData, dataIndex);
                            dataIndex += 4;
                            string jsonText = Encoding.Unicode.GetString(keyData, dataIndex, jsonLength);

                            return JsonUtility.FromJson<string>(jsonText);
                        }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("err", ex);
            }

            return null;
        }

        /// <summary>
        /// Generates a file with all addressable assets in the game.
        /// You can find the file in the Mods/test.txt.
        /// It uses the catalog.json file from the game.
        /// </summary>

        public static void CreateAddressableFile(MelonAssembly assembly)
        {
            string jsonPath = Path.Combine(assembly.Location, "..", "..", "Drova_Data", "StreamingAssets", "aa", "catalog.json");
            string json = File.ReadAllText(jsonPath);
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            ContentCatalogData data = JsonSerializer.Deserialize<ContentCatalogData>(json, options);

            byte[] bucketData = Convert.FromBase64String(data.m_BucketDataString);
            byte[] entryData = Convert.FromBase64String(data.m_EntryDataString);
            byte[] keyData = Convert.FromBase64String(data.m_KeyDataString);
            int bucketCount = BitConverter.ToInt32(bucketData, 0);
            MelonLogger.Msg("bucketcount " + bucketCount);
            Bucket[] buckets = new Bucket[bucketCount];
            int bi = 4;
            for (int i = 0; i < bucketCount; i++)
            {
                int index = SerializationUtilities.ReadInt32FromByteArray(bucketData, bi);
                bi += 4;
                int entryCount = SerializationUtilities.ReadInt32FromByteArray(bucketData, bi);
                bi += 4;
                int[] entryArray = new int[entryCount];
                for (int c = 0; c < entryCount; c++)
                {
                    //entryArray[c] = SerializationUtilities.ReadInt32FromByteArray(bucketData, bi);
                    bi += 4;
                }

                buckets[i] = new Bucket { entries = entryArray, dataOffset = index };
            }
            MelonLogger.Msg("Buckets created");
            int keyCount = BitConverter.ToInt32(keyData, 0);
            object[] keys = new object[keyCount];
            for (int i = 0; i < buckets.Length; i++)
                keys[i] = ReadObjectFromByteArray(keyData, buckets[i].dataOffset);
            MelonLogger.Msg("Keys created");
            const int kBytesPerInt32 = 4;
            const int k_EntryDataItemPerEntry = 7;
            int count = SerializationUtilities.ReadInt32FromByteArray(entryData, 0);
            string addressablesPath = Path.Combine(assembly.Location, "..", "addressables.txt");
            using (FileStream stream = File.Create(addressablesPath))
            {                     //var locations = new IResourceLocation[count];
                StringBuilder sb = new();
                for (int i = 0; i < count; i++)
                {
                    int index = kBytesPerInt32 + (i * kBytesPerInt32 * k_EntryDataItemPerEntry);
                    int internalId = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                    index += kBytesPerInt32;
                    int providerIndex = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                    index += kBytesPerInt32;
                    int dependencyKeyIndex = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                    index += kBytesPerInt32;
                    int depHash = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                    index += kBytesPerInt32;
                    int dataIndex = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                    index += kBytesPerInt32;
                    int primaryKey = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                    index += kBytesPerInt32;
                    int resourceType = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                    //object data = dataIndex < 0 ? null : SerializationUtilities.ReadObjectFromByteArray(extraData, dataIndex);
                    sb.Append("Name/InternalId: " + data.m_InternalIds[internalId] + " Guid: " + keys[primaryKey].ToString() + " ressourceType: " + data.m_resourceTypes[resourceType].ClassName + "\n");

                }
                stream.Write(Encoding.ASCII.GetBytes(sb.ToString()));
            }

            MelonLogger.Msg("Addressable File Created at " + addressablesPath);

        }
    }
}
