using MelonLoader;
using System.Collections.Concurrent;

namespace Drova_Modding_API.Systems
{
    /// <summary>
    /// Marshals work onto the Unity main thread. Needed because Drova deserializes dialogue
    /// graphs on a UniTask thread-pool thread (<c>DS_DialogueTreeController.StartGraphAsync</c>),
    /// so Harmony patches on <c>Graph.SelfDeserialize</c> can run off the main thread. Unity APIs
    /// (AssetBundle loading, AudioClip creation) called there fail natively with
    /// "Graphics device is null" because IL2CPP release builds strip the managed thread guards.
    /// The queue is drained once per frame from <c>Core.OnUpdate</c>.
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static int _mainThreadId = -1;
        private static readonly ConcurrentQueue<Action> _queue = new();

        /// <summary>
        /// How long <see cref="WaitForTasks"/> pumps before giving up, so a task that never
        /// completes cannot freeze the game permanently.
        /// </summary>
        private const int WaitTimeoutMs = 10_000;

        internal static void Initialize()
        {
            _mainThreadId = Environment.CurrentManagedThreadId;
        }

        /// <summary>
        /// True when called from the Unity main thread. Always false before <c>Core</c> initialized.
        /// </summary>
        public static bool IsMainThread => Environment.CurrentManagedThreadId == _mainThreadId;

        /// <summary>
        /// Queue an action to run on the main thread during the next frame's update.
        /// </summary>
        /// <param name="action">The action to run on the main thread</param>
        public static void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }

        /// <summary>
        /// Run a function on the main thread and get its result as a task. Executes inline when
        /// already on the main thread; otherwise it is queued for the next frame.
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="func">The function to run on the main thread</param>
        /// <returns>Task that completes once the function ran</returns>
        public static Task<T> RunOnMainThread<T>(Func<T> func)
        {
            if (IsMainThread)
            {
                try
                {
                    return Task.FromResult(func());
                }
                catch (Exception e)
                {
                    return Task.FromException<T>(e);
                }
            }
            TaskCompletionSource<T> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Block on the main thread until all tasks completed, while still draining the dispatcher
        /// queue. Use this instead of <see cref="Task.WaitAll(Task[])"/> when the awaited tasks may
        /// themselves need the main thread (e.g. AudioClip creation), which would otherwise deadlock.
        /// </summary>
        /// <param name="tasks">The tasks to wait for</param>
        public static void WaitForTasks(params Task[] tasks)
        {
            if (!IsMainThread)
            {
                // Off the main thread there is nothing to pump; a plain wait is safe here.
                Task.WaitAll(tasks);
                return;
            }
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < WaitTimeoutMs)
            {
                Drain();
                bool allDone = true;
                foreach (Task task in tasks)
                {
                    if (!task.IsCompleted)
                    {
                        allDone = false;
                        break;
                    }
                }
                if (allDone) return;
                Thread.Sleep(1);
            }
            MelonLogger.Warning($"MainThreadDispatcher.WaitForTasks timed out after {WaitTimeoutMs}ms; continuing without the remaining results.");
        }

        internal static void Drain()
        {
            while (_queue.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"MainThreadDispatcher action failed: {e}");
                }
            }
        }
    }
}
