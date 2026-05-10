using MelonLoader;

namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    internal static class DialogueUtils
    {
        public const char SEPERATOR = '|';
        public const string EMOTION = "Neutral";
        public const string STYLE = "Default";
        private const string MAPPING_FILE_NAME = "mapping.txt";

        internal static string MapActorNameToNumber(Dictionary<string, int> actorMapping, string actorName)
        {
            return actorName;
            //if (actorMapping.ContainsKey(actorName))
            //{
            //    return actorMapping[actorName].ToString();
            //}
            //else
            //{
            //    MelonLogger.Error("No mapping found for actor: " + actorName);
            //    return "0";
            //}
        }

        public static void ReadActorMappings(Dictionary<string, int> actorMapping)
        {
            string pathToMapping = Path.Combine(Utils.SavePath, MAPPING_FILE_NAME);
            if (File.Exists(pathToMapping))
            {
                string[] lines = File.ReadAllLines(pathToMapping);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] split = line.Split(SEPERATOR);
                    if (split.Length != 2)
                    {
                        MelonLogger.Error("Invalid mapping line: " + line);
                        continue;
                    }
                    actorMapping.Add(split[0], int.Parse(split[1]));
                }
            }
            else
            {
                MelonLogger.Error("No mapping file found at: " + pathToMapping);
            }
        }
    }
}
