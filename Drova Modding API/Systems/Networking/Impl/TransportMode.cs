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
        /// One transport connection to a relay that forwards for everyone. Peers are virtual ids behind
        /// that single connection.
        /// </summary>
        Relay,
    }
}
