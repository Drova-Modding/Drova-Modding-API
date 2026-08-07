namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// The single coordinator behind <see cref="Access.NetworkAccess"/>, holding the one
    /// <see cref="LiteTransport"/> for the process. <c>Core</c> drives its lifecycle:
    /// <see cref="Initialize"/> at melon init, <see cref="Poll"/> each frame, and
    /// <see cref="Shutdown"/> on teardown. Everything here runs on the Unity main thread.
    /// </summary>
    internal static class NetworkSystem
    {
        private static LiteTransport? _transport;

        /// <summary>
        /// The live transport, for the debug stats view to read the counters and per-peer numbers that
        /// <see cref="Access.NetworkAccess"/> deliberately does not expose. Null until
        /// <see cref="Initialize"/> has run.
        /// </summary>
        internal static LiteTransport? Transport => _transport;

        internal static NetRole Role => _transport?.Role ?? NetRole.None;

        internal static bool IsConnected => _transport?.IsConnected ?? false;

        internal static int PeerCount => _transport?.PeerCount ?? 0;

        internal static string? SessionCode => _transport?.SessionCode;

        internal static bool IsEncrypted => _transport?.IsEncrypted ?? false;

        internal static void Initialize()
        {
            _transport ??= new LiteTransport();
        }

        internal static void Poll()
        {
            _transport?.Poll();
        }

        internal static void Shutdown()
        {
            _transport?.Stop();
            // Handlers are process-global; drop them so a later session does not inherit stale ones.
            Dispatcher.Reset();
        }

        internal static void StartHost(int port, string? key)
        {
            Initialize();
            _transport!.StartHost(port, key);
        }

        internal static void Connect(string address, int port, string? key)
        {
            Initialize();
            _transport!.Connect(address, port, key);
        }

        internal static void StartHostViaRelay(string address, int port, string? sessionCode, string? password)
        {
            Initialize();
            _transport!.StartHostViaRelay(address, port, sessionCode, password);
        }

        internal static void ConnectViaRelay(string address, int port, string sessionCode, string? password)
        {
            Initialize();
            _transport!.ConnectViaRelay(address, port, sessionCode, password);
        }

        internal static void Stop()
        {
            _transport?.Stop();
        }

        internal static void GetPeers(List<INetPeer> buffer)
        {
            buffer.Clear();
            _transport?.CopyPeers(buffer);
        }

        internal static void RegisterWithId<T>(ushort id, Action<INetPeer, T> handler) where T : struct, INetMessage
        {
            Dispatcher.RegisterWithId(id, handler);
        }

        internal static void Unregister<T>() where T : struct, INetMessage
        {
            Dispatcher.Unregister<T>();
        }

        internal static void Send<T>(INetPeer peer, in T message, Delivery delivery) where T : struct, INetMessage
        {
            Initialize();
            _transport!.SendTyped(peer, message, delivery);
        }

        internal static void SendToAll<T>(in T message, Delivery delivery) where T : struct, INetMessage
        {
            _transport?.SendTypedToAll(message, delivery);
        }

        internal static void RegisterChannel(byte channel, RawHandler handler)
        {
            Dispatcher.RegisterRawChannel(channel, handler);
        }

        internal static void UnregisterChannel(byte channel)
        {
            Dispatcher.UnregisterRawChannel(channel);
        }

        internal static void SendRaw(INetPeer peer, byte channel, ReadOnlySpan<byte> data, Delivery delivery)
        {
            _transport?.SendRaw(peer, channel, data, delivery);
        }

        internal static void SendRawToAll(byte channel, ReadOnlySpan<byte> data, Delivery delivery)
        {
            _transport?.SendRawToAll(channel, data, delivery);
        }
    }
}
