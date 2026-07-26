using Drova_Modding_API.Systems.Networking;
#if NETCOOP
using MelonLogger = MelonLoader.MelonLogger;
#endif

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Connection lifecycle of the networking transport, bridged out as plain C# events so the coop
    /// mod subscribes instead of touching the transport. All events fire on the Unity main thread.
    /// The raisers are wrapped so a throwing subscriber cannot tear down the transport's receive
    /// loop. When the API is built without coop support these events simply never fire.
    /// </summary>
    public static class NetworkEvents
    {
        // The compiler cannot see the raisers on a non-coop build, so it would flag the events as
        // never used; they are still part of the public surface a coop mod compiles against.
#pragma warning disable CS0067
        /// <summary>
        /// The local host started listening. Fires only in <see cref="NetRole.Host"/>.
        /// </summary>
        public static event Action? OnHostStarted;

        /// <summary>
        /// The local client finished connecting to its host. Fires only in <see cref="NetRole.Client"/>,
        /// before the matching <see cref="OnPeerConnected"/> for the host peer.
        /// </summary>
        public static event Action? OnConnectedToHost;

        /// <summary>
        /// A peer connected. On the host this fires once per joining client; on the client it fires
        /// for the host peer.
        /// </summary>
        public static event Action<INetPeer>? OnPeerConnected;

        /// <summary>
        /// A peer disconnected, with the folded-down reason. Do not use the peer past this callback.
        /// </summary>
        public static event Action<INetPeer, DisconnectReason>? OnPeerDisconnected;

        /// <summary>
        /// A connection attempt was refused, with the reason when the other side gave one. Fires before
        /// the matching <see cref="OnPeerDisconnected"/>, and in a relay session it is the only place the
        /// real cause (wrong code, wrong password, session full) shows up.
        /// </summary>
        public static event Action<ConnectionRejection>? OnConnectionRejected;

        /// <summary>
        /// A transport-level socket error occurred. The string is a human-readable description for
        /// logging; it is not a stable contract.
        /// </summary>
        public static event Action<string>? OnNetworkError;
#pragma warning restore CS0067

#if NETCOOP
        internal static void RaiseHostStarted()
        {
            try
            {
                OnHostStarted?.Invoke();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[NetworkEvents] OnHostStarted failed: " + e);
            }
        }

        internal static void RaiseConnectedToHost()
        {
            try
            {
                OnConnectedToHost?.Invoke();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[NetworkEvents] OnConnectedToHost failed: " + e);
            }
        }

        internal static void RaisePeerConnected(INetPeer peer)
        {
            try
            {
                OnPeerConnected?.Invoke(peer);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[NetworkEvents] OnPeerConnected failed: " + e);
            }
        }

        internal static void RaisePeerDisconnected(INetPeer peer, DisconnectReason reason)
        {
            try
            {
                OnPeerDisconnected?.Invoke(peer, reason);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[NetworkEvents] OnPeerDisconnected failed: " + e);
            }
        }

        internal static void RaiseConnectionRejected(ConnectionRejection rejection)
        {
            try
            {
                OnConnectionRejected?.Invoke(rejection);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[NetworkEvents] OnConnectionRejected failed: " + e);
            }
        }

        internal static void RaiseNetworkError(string description)
        {
            try
            {
                OnNetworkError?.Invoke(description);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[NetworkEvents] OnNetworkError failed: " + e);
            }
        }
#endif
    }
}
