using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Provides a context for modules to resolve and cache components on an NPC.
    /// This ensures efficient component lookups across multiple modules.
    /// </summary>
    public class ModuleContext
    {
        private readonly GameObject _npc;
        private readonly Dictionary<Type, Component> _componentCache = [];

        /// <summary>
        /// Creates a new ModuleContext for the given NPC GameObject.
        /// </summary>
        /// <param name="npc">The NPC GameObject to resolve components from</param>
        public ModuleContext(GameObject npc)
        {
            _npc = npc;
        }

        /// <summary>
        /// Gets a component of the specified type, using cache if available.
        /// </summary>
        /// <typeparam name="T">The component type to retrieve</typeparam>
        /// <returns>The component, or null if not found</returns>
        public T GetComponent<T>() where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return (cached as T)!;

            var component = _npc.GetComponent<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }

        /// <summary>
        /// Gets a component of the specified type in children, using cache if available.
        /// </summary>
        /// <typeparam name="T">The component type to retrieve</typeparam>
        /// <returns>The component, or null if not found</returns>
        public T GetComponentInChildren<T>() where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return (cached as T)!;

            var component = _npc.GetComponentInChildren<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }
    }
}