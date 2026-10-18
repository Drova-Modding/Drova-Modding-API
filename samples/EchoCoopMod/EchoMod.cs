using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Systems.Networking;
using MelonLoader;

[assembly: MelonInfo(typeof(EchoCoopMod.EchoMod), "Echo Coop Sample", "1.0.0", "Drova Modding", null)]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: MelonAdditionalDependencies("Drova_Modding_API")]

namespace EchoCoopMod
{
    /// <summary>
    /// The smallest mod that uses the API's networking transport: the client sends an
    /// <see cref="EchoMessage"/> once it is connected, the host sends the same sequence straight
    /// back, and both sides log what they saw.
    ///
    /// This project references <c>Drova_Modding_API.dll</c> and <c>MelonLoader.dll</c> and nothing
    /// else - it has no LiteNetLib reference of its own, which is the point of the split surface.
    /// </summary>
    public class EchoMod : MelonMod
    {
        /// <summary>
        /// The message id both ends agree on. Ids are the wire contract, so they are declared as
        /// constants rather than handed out by registration order.
        /// </summary>
        private const ushort EchoMessageId = 1;

        private MelonPreferences_Entry<string> _mode = null!;
        private MelonPreferences_Entry<string> _address = null!;
        private MelonPreferences_Entry<int> _port = null!;
        private int _sequence;

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            MelonPreferences_Category category = MelonPreferences.CreateCategory("EchoCoopSample");
            _mode = category.CreateEntry("Mode", "off", description: "off, host or client");
            _address = category.CreateEntry("Address", "127.0.0.1");
            _port = category.CreateEntry("Port", 9050);

            NetworkAccess.RegisterWithId<EchoMessage>(EchoMessageId, OnEcho);

            NetworkEvents.OnHostStarted += () => LoggerInstance.Msg("Hosting, waiting for a client.");
            NetworkEvents.OnPeerConnected += peer => LoggerInstance.Msg($"Peer {peer.Id} connected.");
            NetworkEvents.OnPeerDisconnected += (peer, reason) => LoggerInstance.Msg($"Peer {peer.Id} left: {reason}");
            NetworkEvents.OnNetworkError += description => LoggerInstance.Error("Transport error: " + description);
            NetworkEvents.OnConnectedToHost += SendFirstEcho;
        }

        /// <summary>
        /// Starts the session once the world is up. Doing it here rather than in
        /// <see cref="OnInitializeMelon"/> keeps the socket tied to an actual play session.
        /// </summary>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            if (sceneName != SceneNames.GameplayMain) return;
            if (NetworkAccess.Role != NetRole.None) return;

            switch (_mode.Value)
            {
                case "host":
                    NetworkAccess.StartHost(_port.Value);
                    break;
                case "client":
                    NetworkAccess.Connect(_address.Value, _port.Value);
                    break;
            }
        }

        /// <inheritdoc/>
        public override void OnDeinitializeMelon()
        {
            base.OnDeinitializeMelon();

            if (NetworkAccess.Role == NetRole.None) return;

            NetworkAccess.Stop();
        }

        private void SendFirstEcho()
        {
            _sequence++;
            EchoMessage message = new() { Sequence = _sequence };
            NetworkAccess.SendToAll(message);
            LoggerInstance.Msg($"Sent echo #{message.Sequence}.");
        }

        /// <summary>
        /// Runs on the Unity main thread for both roles. Only the host answers, otherwise the two
        /// ends would bounce the message forever.
        /// </summary>
        private void OnEcho(INetPeer peer, EchoMessage message)
        {
            if (NetworkAccess.Role == NetRole.Host)
            {
                LoggerInstance.Msg($"Host got echo #{message.Sequence} from peer {peer.Id}, sending it back.");
                NetworkAccess.Send(peer, message);
                return;
            }

            LoggerInstance.Msg($"Client got echo #{message.Sequence} back, round trip {peer.Ping} ms.");
        }
    }
}
