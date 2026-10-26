using System.Security.Cryptography;
using System.Text;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// The JSON content under <c>Mods/Modding_API</c>: quests, placements, items, map layouts, dialogues. Machines sharing a
    /// world need the same files, because guids, items and placements are derived from them.
    /// </summary>
    public static class ModContentAccess
    {
        // Folders whose files define world content. Others under Modding_API hold per-player settings or tool output.
        private static readonly string[] ContentFolders =
        [
            "Quests", "CreaturePlacement", "ItemPlacement", "NpcPlacement", "Items", "MapEditor", "NpcDialogues", "GlobalVars",
            "Routines", "BehaviourTrees"
        ];

        /// <summary>
        /// A hash of every <c>*.json</c> in the content folders, by relative path and content. Read from disk on every call.
        /// </summary>
        public static string GetContentHash()
        {
            string root = Utils.SavePath;
            List<string> files = ContentFolders
                .Select(folder => Path.Combine(root, folder))
                .Where(Directory.Exists)
                .SelectMany(folder => Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories))
                .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();

            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (string file in files)
            {
                hash.AppendData(Encoding.UTF8.GetBytes(file.ToLowerInvariant() + "\n"));
                hash.AppendData(File.ReadAllBytes(Path.Combine(root, file)));
            }

            return Convert.ToHexString(hash.GetHashAndReset())[..16];
        }
    }
}
