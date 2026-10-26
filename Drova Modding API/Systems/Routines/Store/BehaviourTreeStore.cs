using System.Diagnostics.CodeAnalysis;
using Drova_Modding_API.Systems.Dialogues.Store;
using Il2CppCustomFramework;
using Il2CppDrova;
using Il2CppNodeCanvas.BehaviourTrees;
using MelonLoader;

namespace Drova_Modding_API.Systems.Routines.Store
{
    /// <summary>
    /// A store for the behaviour trees that drive NPC routines, like <see cref="DialogueStore"/>. A saved tree named
    /// after a game asset overwrites it in memory; any other is an own graph assigned through <see cref="RoutineGraphAccess"/>.
    /// </summary>
    public static class BehaviourTreeStore
    {
        private const string FolderName = "BehaviourTrees";

        /// <summary>
        /// Asset instances already patched, so a repeated pass only touches assets loaded since the last one.
        /// </summary>
        private static readonly HashSet<int> PatchedInstances = [];

        /// <summary>
        /// Gets the absolute path where behaviour tree files are stored.
        /// </summary>
        public static string GetFolderPath()
            => Path.Combine(Utils.SavePath, FolderName);

        /// <summary>
        /// Gets the absolute path for one behaviour tree id.
        /// </summary>
        public static string GetFilePath(string treeId)
            => GraphStore.GetFilePath(GetFolderPath(), treeId);

        /// <summary>
        /// Every behaviour tree id in the store.
        /// </summary>
        public static IEnumerable<string> GetSavedIds()
            => Directory.Exists(GetFolderPath())
                ? Directory.GetFiles(GetFolderPath(), "*.json")
                    .Where(file => !file.EndsWith(".graph.dev.json", StringComparison.OrdinalIgnoreCase))
                    .Select(Path.GetFileNameWithoutExtension)!
                : [];

        /// <summary>
        /// Saves a behaviour tree under <paramref name="treeId"/>.
        /// </summary>
        public static bool SaveBehaviourTree(string treeId, BehaviourTree tree)
            => GraphStore.Save(GetFolderPath(), treeId, tree);

        /// <summary>
        /// Overwrites the live behaviour tree asset named <paramref name="treeId"/> with the stored version and
        /// restarts the routines running it (see <see cref="RoutineGraphAccess.RefreshRunningRoutines"/>).
        /// </summary>
        /// <returns><c>true</c> if a matching in-memory asset was found and patched.</returns>
        public static bool TryPatchInMemoryBehaviourTree(string treeId)
        {
            if (!GraphStore.TryPatchInMemory<BehaviourTree>(GetFolderPath(), treeId)) return false;

            RoutineGraphAccess.RefreshRunningRoutines(treeId);
            return true;
        }

        /// <summary>
        /// Creates a new behaviour tree from the stored version of <paramref name="treeId"/>.
        /// </summary>
        public static bool TryLoadBehaviourTree(string treeId, [NotNullWhen(true)] out BehaviourTree? tree)
            => GraphStore.TryLoad(GetFolderPath(), treeId, out tree);

        /// <summary>
        /// Patches every stored tree onto its live asset. Assets patched by an earlier pass are skipped.
        /// </summary>
        public static void PatchAllSavedBehaviourTrees()
        {
            LoadAIDatabase();
            int patched = GraphStore.PatchAll<BehaviourTree>(GetFolderPath(), PatchedInstances);
            if (patched > 0)
            {
                MelonLogger.Msg($"PatchAllSavedBehaviourTrees: patched {patched} behaviour tree(s).");
            }
        }

        /// <summary>
        /// Patches every stored tree again, including already patched assets. Runs whenever the main menu loads.
        /// </summary>
        internal static void RepatchAllSavedBehaviourTrees()
        {
            PatchedInstances.Clear();
            PatchAllSavedBehaviourTrees();
        }

        /// <summary>
        /// Loads the lazy AIDatabase singleton so its routine graphs can be patched before the first NPC uses one.
        /// </summary>
        private static void LoadAIDatabase()
        {
            try
            {
                _ = AResourceScriptableSingleton<AIDatabase>.Instance;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("BehaviourTreeStore: could not load the AIDatabase: " + e.Message);
            }
        }
    }
}
