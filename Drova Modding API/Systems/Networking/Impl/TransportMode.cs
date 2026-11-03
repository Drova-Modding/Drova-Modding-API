namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// How the transport reaches the other players. The coop mod picks it implicitly by which start or
    /// connect method it calls, and never sees this type.
    /// </summary>
    internal enum TransportMode
    {
        /// <summary>
        /// One transport connection per remote player. Needs the host reachable from the internet.
        /// </summary>
        Direct,

        /// <summary>
        /// Steam peer-to-peer messages. Peers are Steam ids, see <see cref="SteamLink"/>.
        /// </summary>
        Steam,
    }
}
