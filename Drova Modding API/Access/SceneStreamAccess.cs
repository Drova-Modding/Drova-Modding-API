using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Streamed-scene awareness. Drova streams chunk scenes
    /// (<c>Scene_Overworld_&lt;Layer&gt;_&lt;x&gt;_&lt;y&gt;</c>, area interiors, ...) continuously
    /// while the player travels, so world objects appear and disappear with their scene.
    ///
    /// Register for the scene names you care about and you get the <see cref="Scene"/> handle the
    /// moment it is loaded - without paying anything for the hundreds of loads you do not care
    /// about. PERFORMANCE: do not sweep the whole heap
    /// (<c>Resources.FindObjectsOfTypeAll</c>) or a full scene's renderers on every scene load -
    /// chunk scenes carry thousands of objects and each check is an interop call, so per-load
    /// sweeps tank the frame rate. Scope work to the scenes that provably contain your objects.
    /// </summary>
    public static class SceneStreamAccess
    {
        /// <summary>
        /// Raised for every scene load with the scene's name and handle, including chunk scenes.
        /// Handlers run on the main thread. Prefer <see cref="AddSceneListener"/> when you only
        /// care about specific scenes.
        /// </summary>
        public static event Action<string, Scene>? OnSceneLoaded;

        /// <summary>
        /// Raised for every scene unload with the scene's name. Objects of that scene (and any
        /// children you parented onto them) are destroyed with it.
        /// </summary>
        public static event Action<string>? OnSceneUnloaded;

        private static readonly Dictionary<string, List<Action<Scene>>> Listeners = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Call <paramref name="onLoaded"/> every time the scene named
        /// <paramref name="sceneName"/> finishes loading (case-insensitive). If the scene is
        /// already loaded when you register, the handler fires immediately as well.
        /// </summary>
        /// <param name="sceneName">Scene name as the runtime reports it (no path, no ".unity").</param>
        /// <param name="onLoaded">Handler, invoked on the main thread with the loaded scene.</param>
        public static void AddSceneListener(string sceneName, Action<Scene> onLoaded)
        {
            if (!Listeners.TryGetValue(sceneName, out List<Action<Scene>>? list))
            {
                list = [];
                Listeners[sceneName] = list;
            }
            list.Add(onLoaded);

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                InvokeSafe(onLoaded, scene);
            }
        }

        /// <summary>
        /// Remove a handler registered with <see cref="AddSceneListener"/>.
        /// </summary>
        /// <param name="sceneName">The scene name the handler was registered for.</param>
        /// <param name="onLoaded">The handler to remove.</param>
        public static void RemoveSceneListener(string sceneName, Action<Scene> onLoaded)
        {
            if (Listeners.TryGetValue(sceneName, out List<Action<Scene>>? list))
            {
                list.Remove(onLoaded);
                if (list.Count == 0)
                {
                    Listeners.Remove(sceneName);
                }
            }
        }

        /// <summary>
        /// Enumerate every component of type <typeparamref name="T"/> in one scene, including on
        /// inactive objects. This is the scoped alternative to
        /// <c>Resources.FindObjectsOfTypeAll</c>: it walks only the given scene's hierarchy and
        /// can never hand back prefab assets (parenting mod objects into a prefab corrupts every
        /// future instance of it).
        /// </summary>
        /// <typeparam name="T">Component type to collect.</typeparam>
        /// <param name="scene">A loaded scene.</param>
        /// <returns>All components of that type in the scene.</returns>
        public static List<T> GetComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> result = [];
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return result;
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    result.Add(component);
                }
            }
            return result;
        }

        /// <summary>
        /// Called by <see cref="Core.OnSceneWasLoaded"/> for every scene load.
        /// </summary>
        internal static void NotifySceneLoaded(string sceneName)
        {
            bool anyTargeted = Listeners.TryGetValue(sceneName, out List<Action<Scene>>? list);
            if (!anyTargeted && OnSceneLoaded == null)
            {
                return;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            try
            {
                OnSceneLoaded?.Invoke(sceneName, scene);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[SceneStreamAccess] OnSceneLoaded handler failed: " + e);
            }

            if (list != null)
            {
                // Copy: a handler may add/remove listeners while we iterate.
                foreach (Action<Scene> handler in list.ToArray())
                {
                    InvokeSafe(handler, scene);
                }
            }
        }

        /// <summary>
        /// Called by <see cref="Core.OnSceneWasUnloaded"/> for every scene unload.
        /// </summary>
        internal static void NotifySceneUnloaded(string sceneName)
        {
            try
            {
                OnSceneUnloaded?.Invoke(sceneName);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[SceneStreamAccess] OnSceneUnloaded handler failed: " + e);
            }
        }

        private static void InvokeSafe(Action<Scene> handler, Scene scene)
        {
            try
            {
                handler(scene);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[SceneStreamAccess] scene listener failed: " + e);
            }
        }
    }
}
