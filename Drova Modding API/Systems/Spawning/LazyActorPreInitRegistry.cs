using Il2CppDrova;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Runs callbacks on a lazy-controlled <see cref="Actor"/> right before <c>Actor.StartInitAsync</c>.
    /// Replaces <c>LazyActor.ActorSpawnEvent</c>, whose non-blittable <c>SpawnArgs</c> Il2CppInterop cannot marshal.
    /// </summary>
    public static class LazyActorPreInitRegistry
    {
        private static readonly Dictionary<IntPtr, List<Action<Actor, LazyActor>>> Callbacks = [];

        /// <summary>
        /// Registers a callback that will be invoked every time a lazy-controlled actor
        /// owned by <paramref name="lazyActor"/> is about to be initialized (right before
        /// <c>StartInitAsync</c>). Re-registering the same LazyActor appends another callback.
        /// </summary>
        public static void Register(LazyActor lazyActor, Action<Actor, LazyActor> callback)
        {
            IntPtr key = lazyActor.Pointer;
            if (!Callbacks.TryGetValue(key, out var list))
            {
                list = [];
                Callbacks[key] = list;

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
        internal static void Invoke(LazyActor lazyActor, Actor actor)
        {
            if (!Callbacks.TryGetValue(lazyActor.Pointer, out var list))
                return;

            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    list[i](actor, lazyActor);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[LazyActorPreInitRegistry] Callback threw an exception for actor '{(actor == null ? null : actor.name)}': {ex}");
                }
            }
        }
    }
}