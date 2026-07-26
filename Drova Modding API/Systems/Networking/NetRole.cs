namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// The local peer's role in the current session. <see cref="None"/> whenever no session is
    /// running or the API was built without coop support.
    /// </summary>
    public enum NetRole
    {
        /// <summary>
        /// No session is running.
        /// </summary>
        None,

        /// <summary>
        /// This peer is hosting; remote peers connect to it.
        /// </summary>
        Host,

        /// <summary>
        /// This peer connected to a remote host.
        /// </summary>
        Client,
    }
}
