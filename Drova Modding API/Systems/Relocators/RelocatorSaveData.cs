using System.Text.Json;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Data to save for relocation.
    /// </summary>
    public class RelocatorSaveData
    {
        /// <summary>
        /// Serializer options for relocator payloads, which use public fields.
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };

        /// <summary>
        /// Key for the relocator.
        /// </summary>
        public string? Key;
    }
}
