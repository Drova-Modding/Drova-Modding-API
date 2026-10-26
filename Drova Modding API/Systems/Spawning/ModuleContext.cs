using Il2CppDrova.Utilities.LazyLoading;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Provides a context for modules to resolve and cache components on an NPC.
    /// </summary>
    public class ModuleContext(GameObject? npc, LazyActor? lazyActor)
    {
        private readonly Dictionary<Type, Component> _componentCache = [];

        /// <summary>
        /// The LazyActor of this npc if applicable
        /// </summary>
        public LazyActor? LazyActor { get; } = lazyActor;

        /// <summary>
        /// Gets a component of the specified type, using cache if available.
        /// </summary>
        /// <returns>The component, or null if not found</returns>
        public T GetComponent<T>() where T : Component
        {
            if (npc == null)
                return null!;

            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return (cached as T)!;

            var component = npc.GetComponent<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }

        /// <summary>
        /// Gets a component of the specified type in children, using cache if available.
        /// </summary>
        /// <returns>The component, or null if not found</returns>
        public T GetComponentInChildren<T>() where T : Component
        {
            if (npc == null)
                return null!;

            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return (cached as T)!;

            var component = npc.GetComponentInChildren<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }
    }
}