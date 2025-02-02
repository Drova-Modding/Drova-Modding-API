namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    internal static class DialogueUtils
    {
        public const char SEPERATOR = '|';
        public const string EMOTION = "Neutral";
        public const string STYLE = "Default";

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
    }
}
