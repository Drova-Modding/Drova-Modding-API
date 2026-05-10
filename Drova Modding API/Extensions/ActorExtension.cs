using Il2CppDrova;

namespace Drova_Modding_API.Extensions
{
    /// <summary>
    /// Extensions for the Actor class.
    /// </summary>
    public static class ActorExtension
    {

        /// <summary>
        /// Checks if the player is teleporting.
        /// </summary>
        public static bool IsPlayerTeleporting(this Actor actor)
        {
            return actor.IsPlayer && SceneGameHandler._isTeleportingPlayer;
        }

        /// <summary>
        /// Checks if the actor is in a Dialogue
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        public static bool IsInDialogue(this Actor actor)
        {
            return actor._dialogueModule.IsInDialogue();
        }
    }
}
