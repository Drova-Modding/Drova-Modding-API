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

        /// <summary>
        /// Get the display name of an actor parameter, falling back to its key name.
        /// </summary>
        /// <param name="actorParameter">The actor parameter.</param>
        /// <returns>The display name, or the key name, or an empty string.</returns>
        internal static string GetActorParameterName(ActorParameter actorParameter)
        {
            if (!string.IsNullOrWhiteSpace(actorParameter.name))
            {
                return actorParameter.name;
            }

            return actorParameter._keyName ?? string.Empty;
        }

        /// <summary>
        /// Get the stable id of an actor parameter, falling back to its internal id.
        /// </summary>
        /// <param name="actorParameter">The actor parameter.</param>
        /// <returns>The id, or the internal id, or an empty string.</returns>
        internal static string GetActorParameterId(ActorParameter actorParameter)
        {
            if (!string.IsNullOrWhiteSpace(actorParameter.ID))
            {
                return actorParameter.ID;
            }

            return actorParameter._id ?? string.Empty;
        }
    }
}
