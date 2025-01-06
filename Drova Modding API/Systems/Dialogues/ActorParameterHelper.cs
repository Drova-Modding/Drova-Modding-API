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
            return new ActorParameter("Player");
        }
    }
}
