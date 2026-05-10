using Il2CppDrova;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    /// <summary>
    /// A class to generate guids for dynamic instances.
    /// </summary>
    public static class DynamicInstancesToGuid
    {
        /// <summary>
        /// Generates guids for all dynamic instances.
        /// </summary>
        public static void GenerateGuids()
        {
            var allGuids = UnityEngine.Object.FindObjectsByType<GuidComponent>(UnityEngine.FindObjectsSortMode.InstanceID);
            var allGenericInstances = allGuids.Where(g =>
            {
                if (g.name.Contains("Bandit"))
                {
                    return true;
                }
                return false;
            });
            var guidFromInstances = allGenericInstances.Select(x => new DataContainer(x._guidString, x.transform.parent.name, x.name)).ToList();
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < guidFromInstances.Count; i++)
            {
                var guid = guidFromInstances[i];
                stringBuilder.Append(guid.Guid).Append('|').Append(guid.Location).AppendLine();
            }
            File.WriteAllText(Path.Combine(Utils.SavePath, "GenericInstancesToGuids.txt"), stringBuilder.ToString());
        }

        private readonly struct DataContainer(string guid, string location, string name)
        {
            public readonly string Guid { get; } = guid;
            public readonly string Name { get; } = name;
            public readonly string Location { get; } = location;
        }
    }
}
