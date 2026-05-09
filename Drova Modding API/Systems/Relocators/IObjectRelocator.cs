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
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        string SerializeObjectToJson(T objectToSerialize);

        /// <summary>
        /// Deserializes the object from a JSON string.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        T DeserializeObjectFromJson(string json);

        /// <summary>
        /// The key for the relocator.
        /// </summary>
        string Key { get; }
    }
}
