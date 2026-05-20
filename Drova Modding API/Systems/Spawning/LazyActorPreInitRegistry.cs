using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Registry that allows running callbacks on a lazy-controlled <see cref="Actor"/>
    /// <b>before</b> <c>Actor.StartInitAsync</c> is called (i.e. before the actor is fully
    /// initialized). This is the hook point that replaces the unusable
    /// <c>LazyActor.ActorSpawnEvent</c>, whose <c>SpawnArgs</c> non-blittable struct causes
    /// Il2CppInterop to throw when converting managed delegates.
    /// </summary>
    public static class LazyActorPreInitRegistry
    {
        // Keyed by the LazyActor's native Il2Cpp pointer so that GC / equality issues
        // with managed wrappers are avoided.
        private static readonly Dictionary<IntPtr, List<Action<Actor>>> Callbacks = [];

        /// <summary>
        /// Registers a callback that will be invoked every time a lazy-controlled actor
        /// owned by <paramref name="lazyActor"/> is about to be initialized (right before
        /// <c>StartInitAsync</c>). Re-registering the same LazyActor appends another callback.
        /// </summary>
        public static void Register(LazyActor lazyActor, Action<Actor> callback)
        {
            IntPtr key = lazyActor.Pointer;
            if (!Callbacks.TryGetValue(key, out var list))
            {
                list = [];
                Callbacks[key] = list;

                // Auto-clean up when the LazyActor scene object is destroyed.
                lazyActor.LazyActorDestroyedEvent.AddEventListener(
                    new Action<LazyActor>(Unregister));
            }

            list.Add(callback);
        }

        /// <summary>
        /// Removes all callbacks registered for <paramref name="lazyActor"/>.
        /// </summary>
        public static void Unregister(LazyActor lazyActor)
        {
            if (lazyActor != null)
                Callbacks.Remove(lazyActor.Pointer);
        }

        /// <summary>
        /// Invokes all callbacks registered for a given LazyActor pointer.
        /// Called from the Harmony patch below.
        /// </summary>
        internal static void Invoke(IntPtr lazyActorPtr, Actor actor)
        {
            if (!Callbacks.TryGetValue(lazyActorPtr, out var list))
                return;

            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    list[i](actor);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[LazyActorPreInitRegistry] Callback threw an exception for actor '{actor?.name}': {ex}");
                }
            }
        }
    }
}