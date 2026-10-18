namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Relocates unity objects.
    /// <typeparamref name="T"/> is the type of object to relocate.
    /// </summary>
    public interface IObjectRelocator<T>
    {
        /// <summary>
        /// Serializes the object to a JSON string.
        /// </summary>
        string SerializeObjectToJson(T objectToSerialize);

        /// <summary>
        /// Deserializes the object from a JSON string.
        /// </summary>
        /// <returns>The object, or null when it is not found.</returns>
        T? DeserializeObjectFromJson(string json);

        /// <summary>
        /// The key for the relocator.
        /// </summary>
        string Key { get; }
    }
}
