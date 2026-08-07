#if DEBUG
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Networking.Impl;
using Il2CppCommandTerminal;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LiteNetLib;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Networking.Diagnostics
{
    /// <summary>
    /// An in-game window over the coop transport's numbers: round trip, throughput, everything that was
    /// dropped and why, and which message id is spending the bandwidth. Driven by the
    /// <c>api_netstats</c> cheat command and only present in a coop-enabled Debug build, since it reads
    /// transport internals no mod is given.
    ///
    /// Deliberately does not take gameplay input while open, unlike the gvar inspector: a network problem
    /// is something you have to be moving to reproduce, and a view you must close to reproduce the bug
    /// shows you the wrong numbers.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    internal class NetworkStatsView(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private const string CommandName = "api_netstats";
        private const int WindowId = 923460;
        private const float DefaultWindowWidth = 700f;
        private const float DefaultWindowHeight = 620f;
        private const float SampleIntervalSeconds = 0.25f;
        private const int HistoryLength = 120;
        private const float GraphHeight = 40f;
        private const float LabelWidth = 130f;
        private const float MinimumRoundTripScaleMs = 50f;
        private const float MinimumRateScaleBytes = 4096f;

        private static GameObject? _host;
        private static NetworkStatsView? _instance;

        private readonly List<INetPeer> _peers = [];
        private readonly float[] _roundTripHistory = new float[HistoryLength];
        private readonly float[] _sendRateHistory = new float[HistoryLength];
        private readonly float[] _receiveRateHistory = new float[HistoryLength];

        private Rect _windowRect = new(24f, 24f, DefaultWindowWidth, DefaultWindowHeight);
        private Vector2 _scroll;
        private bool _isVisible;
        private bool _hasBaseline;
        private int _historyHead;
        private int _historyCount;
        private long _countingSinceMs;
        private float _lastSampleTime;
        private float _nextSampleTime;
        private long _lastBytesSent;
        private long _lastBytesReceived;
        private long _lastPacketsSent;
        private long _lastPacketsReceived;
        private float _sendBytesPerSecond;
        private float _receiveBytesPerSecond;
        private float _sendPacketsPerSecond;
        private float _receivePacketsPerSecond;

        /// <summary>
        /// Creates the view on its own object that survives scene loads, so the numbers keep accruing
        /// across a load screen and the window is reachable from the main menu, where a session is
        /// usually started, and registers the cheat command that drives it. The registration is queued
        /// by <see cref="CheatMenuAccess"/> until cheat mode exists, so calling this at melon init is
        /// safe.
        /// </summary>
        [HideFromIl2Cpp]
        internal static void Initialize()
        {
            if (_host != null) return;

            _host = new GameObject("ModdingAPI_NetworkStats");
            DontDestroyOnLoad(_host);
            _host.AddComponent<NetworkStatsView>();

            CheatMenuAccess.RegisterCheat(
                CommandName,
                (Action<Il2CppReferenceArray<CommandArg>>)OnNetStatsCommand,
                0,
                1,
                CommandName + " [on|off|toggle|reset|log]",
                "Show coop networking stats (ping, throughput, dropped packets)");
        }

        internal void Awake()
        {
            _instance = this;
        }

        internal void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        internal void Update()
        {
            // Sampled whether or not the window is open, so opening it after something went wrong still
            // shows the half minute that led there.
            if (Time.unscaledTime < _nextSampleTime) return;

            _nextSampleTime = Time.unscaledTime + SampleIntervalSeconds;
            Sample();
        }

        internal void OnGUI()
        {
            if (!_isVisible) return;

            _windowRect = GUI.Window(WindowId, _windowRect, new Action<int>(DrawWindow), "Coop Network Stats (" + CommandName + ")");
        }

        /// <summary>
        /// Backs <c>api_netstats [on|off|toggle|reset|log]</c>. No argument toggles the window;
        /// <c>reset</c> and <c>log</c> do their work whether or not it is open, which is what you want
        /// when the console is already in front of the thing you are measuring.
        /// </summary>
        [HideFromIl2Cpp]
        private static void OnNetStatsCommand(Il2CppReferenceArray<CommandArg> args)
        {
            try
            {
                if (_instance == null)
                {
                    MelonLogger.Warning(CommandName + ": the stats view is not running.");
                    return;
                }

                string mode = args.Length > 0 ? args[0].String ?? string.Empty : string.Empty;
                switch (mode.ToLowerInvariant())
                {
                    case "":
                    case "toggle":
                        _instance._isVisible = !_instance._isVisible;
                        break;
                    case "on":
                        _instance._isVisible = true;
                        break;
                    case "off":
                        _instance._isVisible = false;
                        break;
                    case "reset":
                        _instance.ResetCounters();
                        break;
                    case "log":
                        _instance.LogSnapshot(NetworkSystem.Transport);
                        break;
                    default:
                        MelonLogger.Warning(CommandName + ": expected on, off, toggle, reset or log.");
                        break;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error(CommandName + " failed: " + e);
            }
        }

        [HideFromIl2Cpp]
        private void Sample()
        {
            float now = Time.unscaledTime;
            float elapsed = now - _lastSampleTime;
            _lastSampleTime = now;

            // A new session, or the reset button, restarts the counters; the graph must not draw a cliff
            // between two runs as if it were one.
            long countingSince = NetworkDiagnostics.CountingSinceMs;
            if (countingSince != _countingSinceMs)
            {
                _countingSinceMs = countingSince;
                _historyHead = 0;
                _historyCount = 0;
                _hasBaseline = false;
            }

            NetManager? manager = NetworkSystem.Transport?.Manager;
            if (manager == null)
            {
                _hasBaseline = false;
                return;
            }

            NetStatistics statistics = manager.Statistics;
            long bytesSent = statistics.BytesSent;
            long bytesReceived = statistics.BytesReceived;
            long packetsSent = statistics.PacketsSent;
            long packetsReceived = statistics.PacketsReceived;

            if (_hasBaseline && elapsed > 0f)
            {
                _sendBytesPerSecond = (bytesSent - _lastBytesSent) / elapsed;
                _receiveBytesPerSecond = (bytesReceived - _lastBytesReceived) / elapsed;
                _sendPacketsPerSecond = (packetsSent - _lastPacketsSent) / elapsed;
                _receivePacketsPerSecond = (packetsReceived - _lastPacketsReceived) / elapsed;

                _roundTripHistory[_historyHead] = WorstRoundTripMs();
                _sendRateHistory[_historyHead] = _sendBytesPerSecond;
                _receiveRateHistory[_historyHead] = _receiveBytesPerSecond;
                _historyHead = (_historyHead + 1) % HistoryLength;
                if (_historyCount < HistoryLength) _historyCount++;
            }

            _lastBytesSent = bytesSent;
            _lastBytesReceived = bytesReceived;
            _lastPacketsSent = packetsSent;
            _lastPacketsReceived = packetsReceived;
            _hasBaseline = true;
        }

        [HideFromIl2Cpp]
        private void DrawWindow(int id)
        {
            LiteTransport? transport = NetworkSystem.Transport;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset counters", GUILayout.Width(130f)))
            {
                ResetCounters();
            }
            if (GUILayout.Button("Log snapshot", GUILayout.Width(130f)))
            {
                LogSnapshot(transport);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80f)))
            {
                _isVisible = false;
            }
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll);

            DrawSession(transport);
            DrawTraffic(transport);
            DrawGraph("round trip", _roundTripHistory, MinimumRoundTripScaleMs, false, new Color(0.4f, 0.85f, 1f));
            DrawGraph("outgoing", _sendRateHistory, MinimumRateScaleBytes, true, new Color(1f, 0.75f, 0.35f));
            DrawGraph("incoming", _receiveRateHistory, MinimumRateScaleBytes, true, new Color(0.55f, 1f, 0.55f));
            DrawDropped();
            DrawPeers(transport);
            DrawMessageTypes();

            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        [HideFromIl2Cpp]
        private static void DrawSession(LiteTransport? transport)
        {
            GUILayout.Label("Session");

            if (transport == null || transport.Role == NetRole.None)
            {
                GUILayout.Label("  no session running");
                return;
            }

            bool relay = transport.Mode == TransportMode.Relay;
            Field("role", transport.Role + (relay ? " via relay" : " direct"));
            Field("connection", transport.IsConnected
                ? "connected, " + transport.PeerCount + " peer(s)"
                : "not connected yet, " + transport.PeerCount + " peer(s) known");

            if (relay)
            {
                Field("session code", transport.SessionCode ?? "waiting for the relay to assign one");
                Field("encryption", transport.IsEncrypted
                    ? "on, end to end"
                    : "off - an open session has no key material");
                Field("local id", transport.LocalVirtualId.ToString());
            }

            Field("counting for", FormatDuration(Environment.TickCount64 - NetworkDiagnostics.CountingSinceMs));
        }

        [HideFromIl2Cpp]
        private void DrawTraffic(LiteTransport? transport)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Traffic");

            NetManager? manager = transport?.Manager;
            if (manager == null)
            {
                GUILayout.Label("  no transport running");
                return;
            }

            NetStatistics statistics = manager.Statistics;
            Field("sent", $"{statistics.PacketsSent:N0} packets, {FormatBytes(statistics.BytesSent)}, " +
                          $"{FormatRate(_sendBytesPerSecond)} ({_sendPacketsPerSecond:F0}/s)");
            Field("received", $"{statistics.PacketsReceived:N0} packets, {FormatBytes(statistics.BytesReceived)}, " +
                              $"{FormatRate(_receiveBytesPerSecond)} ({_receivePacketsPerSecond:F0}/s)");

            // Reliable packets LiteNetLib had to send again, which is the only loss measurable from this
            // end. Unreliable traffic that never arrived leaves no trace here by definition.
            Field("resent", $"{statistics.PacketLoss:N0} packets ({Percent(statistics.PacketLoss, statistics.PacketsSent)})");
        }

        [HideFromIl2Cpp]
        private static void DrawDropped()
        {
            GUILayout.Space(6f);
            GUILayout.Label("Dropped");

            Field("undecryptable", NetworkDiagnostics.UndecryptablePackets.ToString("N0"));
            Field("NaN or infinity", NetworkDiagnostics.RefusedMessages.ToString("N0"));
            Field("unknown id", NetworkDiagnostics.UnknownMessageIds.ToString("N0"));
            Field("malformed", NetworkDiagnostics.MalformedPackets.ToString("N0"));
            Field("receive failures", NetworkDiagnostics.ReceiveFailures.ToString("N0"));
        }

        [HideFromIl2Cpp]
        private void DrawPeers(LiteTransport? transport)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Peers");

            if (transport == null || transport.Role == NetRole.None)
            {
                GUILayout.Label("  no session running");
                return;
            }

            // Refilled in the layout pass only. LiteNetLib maintains its peer list on its own thread, so a
            // peer leaving between layout and repaint would change how many controls this method emits
            // halfway through a frame, and IMGUI answers that with a mismatched layout group rather than
            // with one fewer row.
            if (Event.current.type == EventType.Layout)
            {
                NetworkSystem.GetPeers(_peers);
            }

            // Checked before the peer count, because a relay host waiting for the first player still has a
            // link worth looking at - and it is exactly the link they are waiting on.
            if (transport.Mode == TransportMode.Relay)
            {
                DrawRelayPeers(transport);
                return;
            }

            if (_peers.Count == 0)
            {
                GUILayout.Label("  nobody connected");
                return;
            }

            GUILayout.BeginHorizontal();
            Cell("id", 40f);
            Cell("rtt", 70f);
            Cell("one-way", 70f);
            Cell("mtu", 60f);
            Cell("last in", 70f);
            Cell("out", 120f);
            Cell("in", 120f);
            Cell("resent", 60f);
            GUILayout.EndHorizontal();

            foreach (INetPeer peer in _peers)
            {
                if (peer is not PeerWrapper wrapper) continue;

                NetPeer native = wrapper.Native;
                NetStatistics statistics = native.Statistics;

                GUILayout.BeginHorizontal();
                Cell(native.Id.ToString(), 40f);
                Cell(native.RoundTripTime + " ms", 70f);
                Cell(native.Ping + " ms", 70f);
                Cell(native.Mtu.ToString(), 60f);
                Cell(native.TimeSinceLastPacket.ToString("F0") + " ms", 70f);
                Cell(FormatBytes(statistics.BytesSent), 120f);
                Cell(FormatBytes(statistics.BytesReceived), 120f);
                Cell(statistics.PacketLoss.ToString("N0"), 60f);
                GUILayout.EndHorizontal();
            }
        }

        [HideFromIl2Cpp]
        private void DrawRelayPeers(LiteTransport transport)
        {
            NetPeer? connection = transport.RelayConnection;
            if (connection == null)
            {
                GUILayout.Label("  no link to the relay");
            }
            else
            {
                NetStatistics statistics = connection.Statistics;
                Field("relay link", $"rtt {connection.RoundTripTime} ms, mtu {connection.Mtu}, " +
                                    $"last packet {connection.TimeSinceLastPacket:F0} ms ago");
                Field("relay traffic", $"{FormatBytes(statistics.BytesSent)} out, {FormatBytes(statistics.BytesReceived)} in, " +
                                       $"{statistics.PacketLoss:N0} resent");
            }

            if (_peers.Count == 0)
            {
                GUILayout.Label("  no players in the session yet");
                return;
            }

            // Everyone is behind that one connection, so nothing per-player can be measured from here -
            // not their share of the bytes and not the second leg of their latency.
            foreach (INetPeer peer in _peers)
            {
                Field("player " + peer.Id, "at least " + peer.Ping + " ms one-way, measured to the relay");
            }
        }

        [HideFromIl2Cpp]
        private static void DrawMessageTypes()
        {
            GUILayout.Space(6f);
            GUILayout.Label("Typed messages");

            GUILayout.BeginHorizontal();
            Cell("id", 40f);
            Cell("sent", 90f);
            Cell("bytes out", 110f);
            Cell("received", 90f);
            Cell("bytes in", 110f);
            GUILayout.EndHorizontal();

            bool any = false;
            for (int id = 0; id < Dispatcher.MaxMessageTypes; id++)
            {
                long sent = NetworkDiagnostics.SentMessages(id);
                long received = NetworkDiagnostics.ReceivedMessages(id);
                if (sent == 0 && received == 0) continue;

                any = true;
                GUILayout.BeginHorizontal();
                Cell(id.ToString(), 40f);
                Cell(sent.ToString("N0"), 90f);
                Cell(FormatBytes(NetworkDiagnostics.SentBytes(id)), 110f);
                Cell(received.ToString("N0"), 90f);
                Cell(FormatBytes(NetworkDiagnostics.ReceivedBytes(id)), 110f);
                GUILayout.EndHorizontal();
            }

            if (!any)
            {
                GUILayout.Label("  nothing sent or received yet");
            }

            GUILayout.Label("  one SendToAll counts once, not once per peer");
        }

        /// <summary>
        /// Draws the history as bars, oldest on the left. The scale is the tallest sample or the given
        /// floor, whichever is larger, so an idle session does not magnify its own noise into a mountain.
        /// </summary>
        [HideFromIl2Cpp]
        private void DrawGraph(string label, float[] history, float minimumScale, bool asRate, Color color)
        {
            float peak = minimumScale;
            for (int i = 0; i < _historyCount; i++)
            {
                if (history[i] > peak) peak = history[i];
            }

            string peakText = asRate ? FormatRate(peak) : peak.ToString("F0") + " ms";
            GUILayout.Label($"{label} - peak {peakText}");
            Rect area = GUILayoutUtility.GetRect(0f, GraphHeight, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) return;

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = color;

            float barWidth = area.width / HistoryLength;
            for (int i = 0; i < _historyCount; i++)
            {
                int index = (_historyHead - _historyCount + i + HistoryLength) % HistoryLength;
                float height = Mathf.Clamp01(history[index] / peak) * area.height;
                GUI.DrawTexture(new Rect(area.x + (i * barWidth), area.yMax - height, Mathf.Max(1f, barWidth - 1f), height),
                    Texture2D.whiteTexture);
            }

            GUI.color = previousColor;
        }

        [HideFromIl2Cpp]
        private void ResetCounters()
        {
            NetworkDiagnostics.Reset();

            NetManager? manager = NetworkSystem.Transport?.Manager;
            if (manager != null)
            {
                manager.Statistics.Reset();
                foreach (NetPeer peer in manager)
                {
                    peer.Statistics.Reset();
                }
            }

            // The next sample would otherwise read a delta against the totals from before the reset and
            // report one enormous spike.
            _hasBaseline = false;
        }

        /// <summary>
        /// Writes the whole view to the MelonLoader log, which is what actually ends up attached to a bug
        /// report - a screenshot of a window that updates four times a second usually is not.
        /// </summary>
        [HideFromIl2Cpp]
        private void LogSnapshot(LiteTransport? transport)
        {
            // Written even with no session running: the counters outlive the session that filled them, and
            // the moment somebody wants them written down is usually just after it ended badly.
            if (transport == null || transport.Role == NetRole.None)
            {
                MelonLogger.Msg("[NetStats] no session running; what follows is the last one, " +
                                $"counted over {FormatDuration(Environment.TickCount64 - NetworkDiagnostics.CountingSinceMs)}");
            }
            else
            {
                MelonLogger.Msg($"[NetStats] role {transport.Role}, {(transport.Mode == TransportMode.Relay ? "relay" : "direct")}, " +
                                $"{transport.PeerCount} peer(s), counting for {FormatDuration(Environment.TickCount64 - NetworkDiagnostics.CountingSinceMs)}");
            }

            NetManager? manager = transport?.Manager;
            if (manager != null)
            {
                NetStatistics statistics = manager.Statistics;
                MelonLogger.Msg($"[NetStats] out {statistics.PacketsSent:N0} pkt / {FormatBytes(statistics.BytesSent)} at {FormatRate(_sendBytesPerSecond)}, " +
                                $"in {statistics.PacketsReceived:N0} pkt / {FormatBytes(statistics.BytesReceived)} at {FormatRate(_receiveBytesPerSecond)}, " +
                                $"resent {statistics.PacketLoss:N0} ({Percent(statistics.PacketLoss, statistics.PacketsSent)})");
            }

            MelonLogger.Msg($"[NetStats] dropped: undecryptable {NetworkDiagnostics.UndecryptablePackets}, " +
                            $"NaN/infinity {NetworkDiagnostics.RefusedMessages}, unknown id {NetworkDiagnostics.UnknownMessageIds}, " +
                            $"malformed {NetworkDiagnostics.MalformedPackets}, receive failures {NetworkDiagnostics.ReceiveFailures}");

            for (int id = 0; id < Dispatcher.MaxMessageTypes; id++)
            {
                long sent = NetworkDiagnostics.SentMessages(id);
                long received = NetworkDiagnostics.ReceivedMessages(id);
                if (sent == 0 && received == 0) continue;

                MelonLogger.Msg($"[NetStats] message {id}: sent {sent:N0} ({FormatBytes(NetworkDiagnostics.SentBytes(id))}), " +
                                $"received {received:N0} ({FormatBytes(NetworkDiagnostics.ReceivedBytes(id))})");
            }
        }

        /// <summary>
        /// The worst round trip in the session, because an average hides the one player whose connection
        /// is the reason somebody opened this window.
        /// </summary>
        [HideFromIl2Cpp]
        private static int WorstRoundTripMs()
        {
            LiteTransport? transport = NetworkSystem.Transport;
            if (transport == null) return 0;

            if (transport.Mode == TransportMode.Relay)
            {
                return transport.RelayConnection?.RoundTripTime ?? 0;
            }

            NetManager? manager = transport.Manager;
            if (manager == null) return 0;

            int worst = 0;
            foreach (NetPeer peer in manager)
            {
                if (peer.RoundTripTime > worst) worst = peer.RoundTripTime;
            }

            return worst;
        }

        [HideFromIl2Cpp]
        private static void Field(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("  " + label, GUILayout.Width(LabelWidth));
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }

        [HideFromIl2Cpp]
        private static void Cell(string text, float width)
        {
            GUILayout.Label(text, GUILayout.Width(width));
        }

        [HideFromIl2Cpp]
        private static string Percent(long part, long whole)
        {
            return whole <= 0 ? "0.00 %" : (part * 100f / whole).ToString("F2") + " %";
        }

        [HideFromIl2Cpp]
        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L) return (bytes / (1024f * 1024f)).ToString("F2") + " MB";
            if (bytes >= 1024L) return (bytes / 1024f).ToString("F1") + " KB";

            return bytes + " B";
        }

        [HideFromIl2Cpp]
        private static string FormatRate(float bytesPerSecond)
        {
            if (bytesPerSecond >= 1024f * 1024f) return (bytesPerSecond / (1024f * 1024f)).ToString("F2") + " MB/s";
            if (bytesPerSecond >= 1024f) return (bytesPerSecond / 1024f).ToString("F1") + " KB/s";

            return bytesPerSecond.ToString("F0") + " B/s";
        }

        [HideFromIl2Cpp]
        private static string FormatDuration(long milliseconds)
        {
            long totalSeconds = Math.Max(0L, milliseconds) / 1000L;

            return $"{totalSeconds / 60L}m {totalSeconds % 60L:D2}s";
        }
    }
}
#endif
