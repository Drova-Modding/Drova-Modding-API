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
        public static bool IsTelporting(this Actor actor)
        {
            return actor.IsPlayer && SceneGameHandler._isTeleportingPlayer;
        }
    }
}
