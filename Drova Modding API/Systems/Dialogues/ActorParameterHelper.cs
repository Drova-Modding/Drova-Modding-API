using static Il2CppNodeCanvas.DialogueTrees.DialogueTree;


namespace Drova_Modding_API.Systems.Dialogues
{
    /// <summary>
    /// Helper class for actor parameters.
    /// </summary>
    public static class ActorParameterHelper
    {
        /// <summary>
        /// Get the player actor parameters.
        /// </summary>
        /// <returns></returns>
        public static ActorParameter GetPlayerActorParameters()
        {
            return new ActorParameter
            {
                _keyName = "Player",
                _id = "1956844c-1ff1-42d4-8420-8718a1b27966",
            };
        }
    }
}
