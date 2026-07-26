using Drova_Modding_API.Systems.Networking;
#if NETCOOP
using Drova_Modding_API.Systems.Networking.Impl;
#endif

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// The coop mod's entry point to the optional networking transport. This is transport plumbing
    /// only: it starts and stops a session, sends and receives messages, and raises connection
    /// events through <see cref="NetworkEvents"/>. It never syncs game state and never draws UI -
    /// that is the coop mod's job.
    ///
    /// The LiteNetLib-backed implementation only exists when the API is built with
    /// <c>-p:Coop=true</c>. Check <see cref="IsSupported"/> before use; the action methods throw
    /// <see cref="NotSupportedException"/> on a build without coop support so a lean API build plus a
    /// coop mod fails loudly instead of silently doing nothing.
    /// </summary>
    public static class NetworkAccess
    {
        /// <summary>
        /// True when this API build includes the networking transport. When false, every action
        /// method throws; a coop mod should surface "install the coop-enabled API build" instead of
        /// calling them.
        /// </summary>
        public static bool IsSupported =>
#if NETCOOP
            true;
#else
            false;
#endif

        /// <summary>
        /// The local peer's role. <see cref="NetRole.None"/> when no session is running or the build
        /// has no coop support.
        /// </summary>
        public static NetRole Role =>
#if NETCOOP
            NetworkSystem.Role;
#else
            NetRole.None;
#endif

        /// <summary>
        /// True when a session is running and at least one peer is connected.
        /// </summary>
        public static bool IsConnected =>
#if NETCOOP
            NetworkSystem.IsConnected;
#else
            false;
#endif

        /// <summary>
        /// Start hosting on the given UDP port. When <paramref name="key"/> is set, only peers that
        /// present the same key are accepted.
        /// </summary>
        /// <param name="port">The UDP port to listen on.</param>
        /// <param name="key">Optional connection key that clients must match.</param>
        public static void StartHost(int port, string? key = null)
        {
#if NETCOOP
            NetworkSystem.StartHost(port, key);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Connect to a host at the given address and UDP port. When the host set a connection key,
        /// pass the same <paramref name="key"/> here.
        /// </summary>
        /// <param name="address">The host's IP address or hostname.</param>
        /// <param name="port">The host's UDP port.</param>
        /// <param name="key">Optional connection key that must match the host's.</param>
        public static void Connect(string address, int port, string? key = null)
        {
#if NETCOOP
            NetworkSystem.Connect(address, port, key);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// How many remote peers are connected. On a client that is 1 while in a session.
        /// </summary>
        public static int PeerCount =>
#if NETCOOP
            NetworkSystem.PeerCount;
#else
            0;
#endif

        /// <summary>
        /// The relay session code in use, or null when there is no relay session. A host that let the relay
        /// pick a code has it here by the time <see cref="NetworkEvents.OnHostStarted"/> fires, which is the
        /// code to show the players who should join.
        /// </summary>
        public static string? SessionCode =>
#if NETCOOP
            NetworkSystem.SessionCode;
#else
            null;
#endif

        /// <summary>
        /// Whether messages are encrypted end to end between the players. True only in a relay session with
        /// a password: the password is the key material, so an open session cannot be encrypted. When this
        /// is true the relay cannot read the traffic, and it cannot forge a message from another player
        /// either. Every message costs 28 bytes more on the wire.
        /// </summary>
        public static bool IsEncrypted =>
#if NETCOOP
            NetworkSystem.IsEncrypted;
#else
            false;
#endif

        /// <summary>
        /// Claim a session code on a relay and host through it, for when the host is not reachable from
        /// the internet. Unlike <see cref="StartHost"/> this cannot succeed immediately:
        /// <see cref="NetworkEvents.OnHostStarted"/> fires once the relay accepted the code, and
        /// <see cref="NetworkEvents.OnConnectionRejected"/> if it refused.
        /// </summary>
        /// <param name="relayAddress">The relay's address or hostname.</param>
        /// <param name="relayPort">The relay's UDP port.</param>
        /// <param name="sessionCode">4 to 16 letters and digits, shared with the players joining. Null or
        /// empty asks the relay to pick one, which is the better option: a relay-assigned code has real
        /// entropy and cannot be claimed by whoever guesses it first. Read it back from
        /// <see cref="SessionCode"/>.</param>
        /// <param name="password">Optional session password clients must match. Setting one also turns on
        /// end-to-end encryption, see <see cref="IsEncrypted"/>.</param>
        public static void StartHostViaRelay(string relayAddress, int relayPort, string? sessionCode = null, string? password = null)
        {
#if NETCOOP
            NetworkSystem.StartHostViaRelay(relayAddress, relayPort, sessionCode, password);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Join a session held on a relay. <see cref="NetworkEvents.OnConnectedToHost"/> fires once the
        /// relay put us in the session, <see cref="NetworkEvents.OnConnectionRejected"/> if it refused.
        /// </summary>
        /// <param name="relayAddress">The relay's address or hostname.</param>
        /// <param name="relayPort">The relay's UDP port.</param>
        /// <param name="sessionCode">The code the host claimed or was assigned.</param>
        /// <param name="password">The session password, when it has one. It must match the host's exactly or
        /// every message will fail to decrypt, since the password is also the encryption key material.</param>
        public static void ConnectViaRelay(string relayAddress, int relayPort, string sessionCode, string? password = null)
        {
#if NETCOOP
            NetworkSystem.ConnectViaRelay(relayAddress, relayPort, sessionCode, password);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Stop the current session and close all connections.
        /// </summary>
        public static void Stop()
        {
#if NETCOOP
            NetworkSystem.Stop();
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Fills a buffer with the connected peers. The buffer is cleared first and never reallocated by
        /// this call, so keeping one list around and passing it in every frame does not allocate.
        /// </summary>
        /// <param name="buffer">The list to fill. Must not be null.</param>
        public static void GetPeers(List<INetPeer> buffer)
        {
#if NETCOOP
            NetworkSystem.GetPeers(buffer);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Register a handler for a typed message under a stable id. Host and client must agree on
        /// the same <paramref name="id"/> for the same message type; registration order does not
        /// matter. The handler runs on the Unity main thread.
        /// </summary>
        /// <typeparam name="T">The message struct type.</typeparam>
        /// <param name="id">The stable id shared by both ends for this message type.</param>
        /// <param name="handler">Invoked with the sending peer and the decoded message.</param>
        public static void RegisterWithId<T>(ushort id, Action<INetPeer, T> handler) where T : struct, INetMessage
        {
#if NETCOOP
            NetworkSystem.RegisterWithId(id, handler);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Drop the handler for a message type, freeing its id for a later registration. Does nothing
        /// when the type was never registered.
        /// </summary>
        /// <typeparam name="T">The message struct type.</typeparam>
        public static void Unregister<T>() where T : struct, INetMessage
        {
#if NETCOOP
            NetworkSystem.Unregister<T>();
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Send a typed message to a single peer. The message type must have been registered with
        /// <see cref="RegisterWithId{T}"/>.
        /// </summary>
        /// <typeparam name="T">The message struct type.</typeparam>
        /// <param name="peer">The peer to send to.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="delivery">The delivery guarantee.</param>
        public static void Send<T>(INetPeer peer, in T message, Delivery delivery = Delivery.Reliable) where T : struct, INetMessage
        {
#if NETCOOP
            NetworkSystem.Send(peer, message, delivery);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Send a typed message to every connected peer. The payload is serialised once. The message
        /// type must have been registered with <see cref="RegisterWithId{T}"/>.
        /// </summary>
        /// <typeparam name="T">The message struct type.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="delivery">The delivery guarantee.</param>
        public static void SendToAll<T>(in T message, Delivery delivery = Delivery.Reliable) where T : struct, INetMessage
        {
#if NETCOOP
            NetworkSystem.SendToAll(message, delivery);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Register a handler for raw packets on a channel. Channels 1 to 62 are available for raw
        /// traffic; channel 0 is reserved for typed messages and 63 for relay control traffic. The
        /// handler runs on the Unity main thread. Use this only when bringing your own serialisation;
        /// prefer <see cref="RegisterWithId{T}"/> otherwise.
        /// </summary>
        /// <param name="channel">The raw channel, 1 to 62.</param>
        /// <param name="handler">Invoked with the sending peer and the raw payload.</param>
        public static void RegisterChannel(byte channel, RawHandler handler)
        {
#if NETCOOP
            NetworkSystem.RegisterChannel(channel, handler);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Drop the handler for a raw channel. Does nothing when the channel had none.
        /// </summary>
        /// <param name="channel">The raw channel to clear.</param>
        public static void UnregisterChannel(byte channel)
        {
#if NETCOOP
            NetworkSystem.UnregisterChannel(channel);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Send raw bytes to a single peer on a channel.
        /// </summary>
        /// <param name="peer">The peer to send to.</param>
        /// <param name="channel">The raw channel, 1 or greater.</param>
        /// <param name="data">The payload to send.</param>
        /// <param name="delivery">The delivery guarantee.</param>
        public static void SendRaw(INetPeer peer, byte channel, ReadOnlySpan<byte> data, Delivery delivery = Delivery.Reliable)
        {
#if NETCOOP
            NetworkSystem.SendRaw(peer, channel, data, delivery);
#else
            throw Unsupported();
#endif
        }

        /// <summary>
        /// Send raw bytes to every connected peer on a channel.
        /// </summary>
        /// <param name="channel">The raw channel, 1 or greater.</param>
        /// <param name="data">The payload to send.</param>
        /// <param name="delivery">The delivery guarantee.</param>
        public static void SendRawToAll(byte channel, ReadOnlySpan<byte> data, Delivery delivery = Delivery.Reliable)
        {
#if NETCOOP
            NetworkSystem.SendRawToAll(channel, data, delivery);
#else
            throw Unsupported();
#endif
        }

#if !NETCOOP
        private static NotSupportedException Unsupported()
        {
            return new NotSupportedException("Drova Modding API was built without coop support. Rebuild with -p:Coop=true to enable networking.");
        }
#endif
    }
}
