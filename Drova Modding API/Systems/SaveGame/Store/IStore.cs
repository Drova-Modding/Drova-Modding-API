namespace Drova_Modding_API.Systems.SaveGame.Store
{
    /// <summary>
    /// A store for a specific type.
    /// </summary>
    /// <typeparam name="T">The specific type to store</typeparam>
    public interface IStore<T> : IStorable
    {
       

        /// <summary>
        /// Adds an item to the store.
        /// </summary>
        /// <param name="item"></param>
        void Add(T item);

        /// <summary>
        /// Adds a range of items to the store.
        /// </summary>
        /// <param name="items"></param>
        void AddRange(T[] items);

        /// <summary>
        /// Removes an item from the store.
        /// </summary>
        /// <param name="item"></param>
        void Remove(T item);

        /// <summary>
        /// Clears the store.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets an item from the store.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T Get(int index);
        /// <summary>
        /// Gets all items from the store.
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetAll();
    }
}
