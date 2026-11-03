using System.Net;
using Drova_Modding_API.Systems.Networking;
using Drova_Modding_API.Systems.Networking.Impl;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// The co-op mod's entry point to the networking transport: sessions, messages, and connection events through
    /// <see cref="NetworkEvents"/>. It never syncs game state or draws UI.
    /// </summary>
    public static class NetworkAccess
    {
        /// <summary>
        /// The local peer's role. <see cref="NetRole.None"/> when no session is running.
        /// </summary>
        public static NetRole Role => NetworkSystem.Role;

        /// <summary>
        /// True when a session is running and at least one peer is connected.
        /// </summary>
        public static bool IsConnected => NetworkSystem.IsConnected;

        /// <summary>
        /// Start hosting on the given UDP port. When <paramref name="key"/> is set, only peers that
        /// present the same key are accepted. The key travels in the clear and a direct session is not encrypted.
        /// When the port cannot be opened, <see cref="NetworkEvents.OnNetworkError"/> fires and <see cref="Role"/> stays
        /// <see cref="NetRole.None"/>.
        /// </summary>
        /// <param name="port">The UDP port to listen on.</param>
        /// <param name="key">Optional connection key that clients must match.</param>
        public static void StartHost(int port, string? key = null)
        {
            NetworkSystem.StartHost(port, key);
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
            NetworkSystem.Connect(address, port, key, false);
        }

        /// <summary>
        /// Whether <see cref="StartHost"/> asks the router to open its port through UPnP. Read when the host starts. The
        /// port closes again when the session stops or the game quits, and after an hour at most if the game crashed.
        /// Off by default.
        /// </summary>
        public static bool UsePortMapping { get; set; }

        /// <summary>
        /// Joins a host whose router has no open port. Keeps trying for two minutes and finds this end's
        /// <see cref="PublicEndPoint"/>, which the host passes to <see cref="Punch"/>.
        /// </summary>
        /// <param name="address">The host's public IP address, its <see cref="PublicEndPoint"/>.</param>
        /// <param name="port">The host's public UDP port.</param>
        /// <param name="key">Optional connection key that must match the host's.</param>
        public static void ConnectWithPunch(string address, int port, string? key = null)
        {
            NetworkSystem.Connect(address, port, key, true);
        }

        /// <summary>
        /// This end's address and port as the internet sees them, after <see cref="FindPublicEndPoint"/>. Null before that,
        /// and outside a direct session.
        /// </summary>
        public static IPEndPoint? PublicEndPoint => NetworkSystem.PublicEndPoint;

        /// <summary>
        /// Asks a public STUN server (Google, Cloudflare) for this direct session's <see cref="PublicEndPoint"/>.
        /// <see cref="NetworkEvents.OnPublicEndPointFound"/> fires with it. Asked again every 20 s, which also keeps the
        /// router from forgetting the address.
        /// </summary>
        public static void FindPublicEndPoint()
        {
            NetworkSystem.FindPublicEndPoint();
        }

        /// <summary>
        /// Opens this end's router toward a joiner for two minutes, so the joiner's <see cref="ConnectWithPunch"/> gets in.
        /// Fails when both routers give every destination a new port.
        /// </summary>
        /// <param name="address">The joiner's public IP address, its <see cref="PublicEndPoint"/>.</param>
        /// <param name="port">The joiner's public UDP port.</param>
        public static void Punch(string address, int port)
        {
            NetworkSystem.Punch(address, port);
        }

        /// <summary>
        /// How many remote peers are connected. On a client that is 1 while in a session.
        /// </summary>
        public static int PeerCount => NetworkSystem.PeerCount;

        /// <summary>
        /// Host over Steam. Only the host's Steam friends are let in. <see cref="NetworkEvents.OnHostStarted"/> fires at once,
        /// or <see cref="NetworkEvents.OnNetworkError"/> when Steam is not running.
        /// </summary>
        public static void StartHostViaSteam()
        {
            NetworkSystem.StartHostViaSteam();
        }

        /// <summary>
        /// Join a Steam host. <see cref="NetworkEvents.OnConnectedToHost"/> fires once the host welcomed us, and
        /// <see cref="NetworkEvents.OnNetworkError"/> if Steam could not reach it.
        /// </summary>
        /// <param name="hostSteamId">The host's 64-bit Steam id, as <see cref="SteamAccess.LocalId"/> reads on the host.</param>
        public static void ConnectViaSteam(ulong hostSteamId)
        {
            NetworkSystem.ConnectViaSteam(hostSteamId);
        }

        /// <summary>
        /// What a direct host (<see cref="StartHost"/>) answers when someone on the local network runs
        /// <see cref="StartDiscovery"/>. Called on the main thread for each request. Keep it well under 1 KB, since it
        /// travels in one datagram. Null, the default, means the host does not answer.
        /// </summary>
        /// <param name="provider">Builds the payload, for example a name and a player count.</param>
        public static void SetDiscoveryInfo(Func<byte[]>? provider)
        {
            LanDiscovery.Provider = provider;
        }

        /// <summary>
        /// Look for direct hosts on the local network: broadcasts now and every 2 seconds until
        /// <see cref="StopDiscovery"/>. Independent of any session. Read the answers with <see cref="GetDiscoveredHosts"/>.
        /// </summary>
        /// <param name="port">The UDP port the hosts listen on.</param>
        public static void StartDiscovery(int port)
        {
            LanDiscovery.Start(port);
        }

        /// <summary>
        /// Stop looking for hosts and forget the ones found. Does nothing when discovery is not running.
        /// </summary>
        public static void StopDiscovery()
        {
            LanDiscovery.Stop();
        }

        /// <summary>
        /// Whether <see cref="StartDiscovery"/> is running.
        /// </summary>
        public static bool IsDiscovering => LanDiscovery.IsRunning;

        /// <summary>
        /// Fills a buffer with the hosts that answered recently. A host silent for three broadcasts is dropped. The
        /// buffer is cleared first.
        /// </summary>
        /// <param name="buffer">The list to fill.</param>
        public static void GetDiscoveredHosts(List<DiscoveredHost> buffer)
        {
            buffer.Clear();
            LanDiscovery.CopyHosts(buffer);
        }

        /// <summary>
        /// Stop the current session and close all connections.
        /// </summary>
        public static void Stop()
        {
            NetworkSystem.Stop();
        }

        /// <summary>
        /// Fills a buffer with the connected peers. The buffer is cleared first and never reallocated by
        /// this call, so keeping one list around and passing it in every frame does not allocate.
        /// </summary>
        /// <param name="buffer">The list to fill. Must not be null.</param>
        public static void GetPeers(List<INetPeer> buffer)
        {
            NetworkSystem.GetPeers(buffer);
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
            NetworkSystem.RegisterWithId(id, handler);
        }

        /// <summary>
        /// Drop the handler for a message type, freeing its id for a later registration. Does nothing
        /// when the type was never registered.
        /// </summary>
        /// <typeparam name="T">The message struct type.</typeparam>
        public static void Unregister<T>() where T : struct, INetMessage
        {
            NetworkSystem.Unregister<T>();
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
            NetworkSystem.Send(peer, message, delivery);
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
            NetworkSystem.SendToAll(message, delivery);
        }

        /// <summary>
        /// Register a handler for raw packets on a channel. Channels 1 to 62 are available for raw
        /// traffic. Channel 0 is reserved for typed messages and 63 for transport control traffic. The
        /// handler runs on the Unity main thread. Prefer <see cref="RegisterWithId{T}"/> unless you bring your own serialization.
        /// </summary>
        /// <param name="channel">The raw channel, 1 to 62.</param>
        /// <param name="handler">Invoked with the sending peer and the raw payload.</param>
        public static void RegisterChannel(byte channel, RawHandler handler)
        {
            NetworkSystem.RegisterChannel(channel, handler);
        }

        /// <summary>
        /// Drop the handler for a raw channel. Does nothing when the channel had none.
        /// </summary>
        /// <param name="channel">The raw channel to clear.</param>
        public static void UnregisterChannel(byte channel)
        {
            NetworkSystem.UnregisterChannel(channel);
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
            NetworkSystem.SendRaw(peer, channel, data, delivery);
        }

        /// <summary>
        /// Send raw bytes to every connected peer on a channel.
        /// </summary>
        /// <param name="channel">The raw channel, 1 or greater.</param>
        /// <param name="data">The payload to send.</param>
        /// <param name="delivery">The delivery guarantee.</param>
        public static void SendRawToAll(byte channel, ReadOnlySpan<byte> data, Delivery delivery = Delivery.Reliable)
        {
            NetworkSystem.SendRawToAll(channel, data, delivery);
        }

    }
}
