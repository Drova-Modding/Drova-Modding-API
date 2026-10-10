using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Il2CppDrova;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.HitSystem;
using Il2CppDrova.InteractionSystem;
using Il2CppDrova.LoadingScreenHandles;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.Saveables;
using Il2CppDrova.Utilities.LazyLoading;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Testing
{
    /// <summary>
    /// Lets a test runner drive this game instance: load a save, walk, teleport, read and set global
    /// variables, kill creatures, ask what the world holds. Inert unless the game was started with
    /// <c>--agent-port N</c>; pair it with <c>--profile X</c> (<see cref="LaunchProfile"/>) so several
    /// instances keep their own saves. The runner is <c>tools/Drova.TestRunner</c> in this repository.
    ///
    /// Listens on 127.0.0.1 only. One request per line, one answer per line: <c>ok &lt;text&gt;</c> or
    /// <c>err &lt;text&gt;</c>. Requests arrive on a socket thread and run on the Unity main thread.
    ///
    /// Built in: <c>ping, state, run, load, gvar, setgvar, tp, tpnear, near, walk, walked, dead, kill,
    /// counters, inside, ai, find, journal, give, items, inventory, equipped, chest, hit, use, takeall, camera, interactables, colliders, talk, time,
    /// scenes, dialogue, choose, next, skip, bye, log</c>. A mod adds its own with <see cref="RegisterCommand"/>, adds fields to
    /// <c>state</c> with <see cref="RegisterState"/>, and makes its console output readable by <c>run</c>
    /// with <see cref="Report"/>.
    /// </summary>
    public static partial class TestAgent
    {
        private static readonly ConcurrentQueue<(string Line, TaskCompletionSource<string> Answer)> _requests = new();
        private static readonly Dictionary<string, Func<string, string>> Commands = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Func<string>> StateProviders = new();

        private static TcpListener? _listener;
        private static List<string>? _capture;
        private static bool _inGameplay;

        /// <summary>
        /// Since when the world has been finished and the player in it, or null. The gameplay scene loads
        /// long before that, and a save load passes through scene changes, so "the scene exists" is not
        /// "the world is up".
        /// </summary>
        private static float? _worldSince;

        /// <summary>Since when the main menu has been up with no loading screen over it, or null.</summary>
        private static float? _menuSince;
        private static bool _menuLoaded;

        /// <summary>
        /// A load asked for, repeated until the world is really up: the game builds its main menu twice
        /// while starting, passing through gameplay in between, and silently drops a load asked across it.
        /// </summary>
        private static (int Hero, int Slot, float LastTry)? _pendingLoad;
        private static AudioClip? _lastClip;

        /// <summary>The last spoken line the dialogue window was asked to show, and how to move past it.</summary>
        private static SubtitlesRequestInfo? _line;

        /// <summary>The answers the dialogue window was last asked to offer, and the window, which finishes a pick.</summary>
        private static (MultipleChoiceRequestInfo Info, DS_DialogueUGUI Window)? _choices;

        /// <summary>
        /// The dialogue node that asked for <see cref="_line"/> and for <see cref="_choices"/>. The tree continues
        /// from whatever node is current, not from the one that asked: continuing a line the game's own UI already
        /// continued (autoplay) moves the dialogue one extra step - answering the next question with its first
        /// answer, or ending the conversation when a stale answer index lands on a node with fewer exits.
        /// </summary>
        private static IntPtr _lineNode;
        private static IntPtr _choicesNode;

        /// <summary>
        /// A press of the dialogue's skip input, waiting for the window to read it. Lines are moved on through the
        /// window's own input check rather than by calling their continue callback: an auto-advancing line would
        /// otherwise be continued twice - once here, once by the window - and the second step lands on whatever
        /// node is current by then.
        /// </summary>
        private static bool _pressSkip;

        /// <summary>How long the player has been out of any dialogue while a skip runs.</summary>
        private static float _skipIdle;

        /// <summary>When <see cref="_line"/> arrived, in unscaled time.</summary>
        private static float _lineSince;

        private static bool _minigamesOff;

        /// <summary>Continuing every spoken line as it appears, until answers are offered or the dialogue ends.</summary>
        private static bool _skipping;

        /// <summary><c>skip first</c>: answers every question with its first answer too, until the dialogue ends.</summary>
        private static bool _skipAnswering;

        private static (Vector2 Direction, float Seconds, float Elapsed, Vector2 LastPos, float SinceCheck)? _walk;
        private static Vector2 _walkStart;
        private static (List<Vector2> Points, int Next, float Stop)? _route;
        private static float _walkedDistance;

        /// <summary>Whether this instance was started with an agent port.</summary>
        public static bool IsActive => _listener != null;

        /// <summary>
        /// Adds a command. The handler gets the rest of the request line after the command word and
        /// returns the answer text; throw to answer <c>err</c> with the exception's message. Runs on the
        /// main thread. Registering is harmless when the agent is off.
        /// </summary>
        public static void RegisterCommand(string name, Func<string, string> handler)
        {
            Commands[name] = handler;
        }

        /// <summary>Adds <c>key=value</c> pairs (space-separated, no spaces in values) to the <c>state</c> answer.</summary>
        public static void RegisterState(Func<string> contributor)
        {
            StateProviders.Add(contributor);
        }

        /// <summary>
        /// Records one line of console output while a <c>run</c> request is executing, so the runner can read
        /// what a command said. A no-op otherwise.
        /// </summary>
        public static void Report(string line)
        {
            _capture?.Add(line);
        }

        internal static void Initialize(HarmonyLib.Harmony harmony)
        {
            LaunchProfile.Initialize(harmony);

            string? value = LaunchProfile.ReadArgument("--agent-port");
            if (value == null || !int.TryParse(value, out int port) || port <= 0 || port > 65535) return;

            // A scripted walk goes in where a held key would: the movement input's own update, a virtual
            // call. Setting the direction from outside, switching the input controller off, or a cutscene
            // style SetTarget all left the player standing - measured.
            Hooking.TryPrefix(harmony, typeof(Il2CppDrova.InputModules.PlayerInput_Movement), "UpdateInput",
                typeof(TestAgent), nameof(MovementInputPrefix));

            // What the dialogue window is asked to show, read where it is handed over: both are the window's
            // event handlers, reached through delegates.
            Hooking.TryPrefix(harmony, typeof(DS_DialogueUGUI), "OnSubtitlesRequest", typeof(TestAgent), nameof(SubtitlesPrefix));
            Hooking.TryPrefix(harmony, typeof(DS_DialogueUGUI), "OnMultipleChoiceRequest", typeof(TestAgent), nameof(ChoicesPrefix));
            Hooking.TryPrefix(harmony, typeof(Il2CppDrova.GUI.Dialogue.GUI_DialogueInputHandler), "CheckShouldSkipDialog",
                typeof(TestAgent), nameof(SkipInputPrefix));

            // The camera leads ahead of the player, up to its full reach: toward the mouse (in an unfocused test
            // instance, wherever the desktop cursor is) or, in the controller modes, along a remembered offset.
            // A camera ~600 units off the player leaves trigger volumes near the player unloaded (they load by
            // camera distance) - the host walked through a dead mine exit. The camera stays on the player instead. After
            // LateUpdate: the private movement methods are inlined into it, and a patch on them never fires - measured.
            Hooking.TryPostfix(harmony, typeof(SmoothCameraTarget), "LateUpdate", typeof(TestAgent), nameof(CameraLeadPostfix));

            // `press <bind> <hold>` holds: the combat input releases every button not physically down.
            Hooking.TryPrefix(harmony, typeof(Il2CppDrova.ActorActions.AA_ABase), "TryReleaseAction", typeof(TestAgent), nameof(HeldReleasePrefix));
            Hooking.TryPrefix(harmony, typeof(Il2CppDrova.ActorActions.AA_APhasicAction), "TryReleaseAction", typeof(TestAgent), nameof(HeldReleasePrefix));

            // `look`: aim the player at a point - the facing, and the mouse point spells and shots aim at.
            Hooking.TryPrefix(harmony, typeof(Il2CppDrova.InputModules.PlayerInput_LookDir), "UpdateInput", typeof(TestAgent), nameof(LookInputPrefix));
            Hooking.TryPostfix(harmony, typeof(CameraGameHandler), "UpdateGameHandler", typeof(TestAgent), nameof(MousePointPostfix));

            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();

            Thread thread = new(Listen) { IsBackground = true, Name = "TestAgent" };
            thread.Start();

            MelonLogger.Msg($"[Agent] test agent listening on 127.0.0.1:{port}.");
        }

        internal static void OnSceneLoaded(string sceneName)
        {
            // Latched on the two scenes that mean something; area scenes stream in continuously.
            if (sceneName == SceneNames.GameplayMain) _inGameplay = true;
            else if (sceneName == SceneNames.MainMenu)
            {
                _inGameplay = false;
                _menuLoaded = true;
                _menuSince = null;
                MelonLogger.Msg("[Agent] main menu scene loaded.");
            }
        }

        internal static void Update()
        {
            if (_listener == null) return;

            _worldSince = IsWorldUp() ? _worldSince ?? Time.realtimeSinceStartup : null;
            // A frame stall is the menu still being built (it is built twice at startup): idle starts over.
            _menuSince = IsMenuUp() && Time.unscaledDeltaTime < 0.5f ? _menuSince ?? Time.realtimeSinceStartup : null;

            // Two instances side by side: Unity stops ticking a window that lost focus, and a game that
            // does not tick answers nothing - measured as a single 101-second frame. Asserted every frame
            // because the game writes the flag back from its own settings after start.
            Application.runInBackground = true;

            // The game's own "deactivate minigames" option, so work spots and locks resolve without a minigame
            // a script cannot play. Written into this instance's profile config only.
            // Only once the world is up: before the game's managers exist, ConfigGameHandler.TryGet throws inside the
            // game - which, asked every frame, filled each start-up log with hundreds of errors.
            if (!_minigamesOff && _worldSince != null && ConfigGameHandler.TryGet(out ConfigGameHandler config) && config != null && config.GameplayConfig != null)
            {
                config.GameplayConfig.DeactivateMinigames.SetValue(true);
                _minigamesOff = true;
            }

            HoldRealInput();
            TickWalk();
            TickSkip();
            TickActions();

            if (_pendingLoad is { } load)
            {
                if (_worldSince != null) _pendingLoad = null;
                else if (_menuSince != null && Time.realtimeSinceStartup - load.LastTry > 15f) Load(load.Hero, load.Slot);
            }

            while (_requests.TryDequeue(out (string Line, TaskCompletionSource<string> Answer) request))
            {
                string answer;

                try
                {
                    answer = "ok " + Execute(request.Line);
                }
                catch (Exception e)
                {
                    answer = "err " + e.Message;
                }

                request.Answer.TrySetResult(answer.Replace('\n', ' ').Replace('\r', ' '));
            }
        }

        private static void Listen()
        {
            while (_listener != null)
            {
                try
                {
                    // A thread per connection: a runner holds its connection for the whole scenario, and a second
                    // one (a person poking at a running test) used to wait behind it until the run ended.
                    TcpClient accepted = _listener.AcceptTcpClient();
                    new Thread(() => Serve(accepted)) { IsBackground = true, Name = "TestAgent client" }.Start();
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("[Agent] accepting a runner failed: " + e.Message);
                }
            }
        }

        private static void Serve(TcpClient accepted)
        {
            try
            {
                using TcpClient client = accepted;
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.UTF8);
                using StreamWriter writer = new(stream, new UTF8Encoding(false));
                writer.AutoFlush = true;
                writer.NewLine = "\n";

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    TaskCompletionSource<string> answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    _requests.Enqueue((line.Trim(), answer));

                    // A request the main thread never reaches (a frozen game) answers rather than hangs.
                    writer.WriteLine(answer.Task.Wait(TimeSpan.FromSeconds(60)) ? answer.Task.Result : "err timed out");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Agent] a runner connection ended: " + e.Message);
            }
        }

        private static string Execute(string line)
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) throw new InvalidOperationException("empty request");

            string rest = line.Length > parts[0].Length ? line.Substring(parts[0].Length).Trim() : string.Empty;

            if (Commands.TryGetValue(parts[0], out Func<string, string> registered)) return registered(rest);

            switch (parts[0].ToLowerInvariant())
            {
                case "ping": return "pong";
                case "state": return State();
                case "run": return Run(rest);
                case "load": return Load(Int(parts, 1), Int(parts, 2));
                case "save": return Save(Int(parts, 1));
                case "gvar": return Describe(Find(parts));
                case "setgvar": return Set(Find(parts), Arg(parts, 3));

                case "tp":
                    // Also "tp x,y", so a position read from another instance's state can be passed on.
                    if (parts.Length == 2 && parts[1].Contains(','))
                    {
                        string[] xy = parts[1].Split(',');
                        return Teleport(Parse(xy[0]), Parse(xy[1]));
                    }

                    return Teleport(Float(parts, 1), Float(parts, 2));

                case "tpnear": return TeleportNear(Arg(parts, 1), Float(parts, 2), parts.Length > 3 ? Float(parts, 3) : 0f);
                case "near": return Near(Arg(parts, 1), Float(parts, 2));
                case "walk": return Walk(Float(parts, 1), Float(parts, 2), Float(parts, 3));
                case "walked": return Format(_walkedDistance);
                case "realinput": _realInput = Arg(parts, 1) == "on"; HoldRealInput(); return "real input " + (_realInput ? "on" : "off");
                case "walkto": return WalkTo(Arg(parts, 1), parts.Length > 2 ? Float(parts, 2) : 30f, parts.Length > 3 ? Float(parts, 3) : 40f);
                case "dead": return Dead(Arg(parts, 1));
                case "kill": return Kill(Arg(parts, 1));
                case "counters": return Counters(parts.Length > 1 ? parts[1] : string.Empty);
                case "inside": return Inside(Arg(parts, 1));
                case "tpinto": return TeleportInto(Arg(parts, 1));
                case "ai": return Brain(Arg(parts, 1));
                case "port": return PortToRoutine(Arg(parts, 1));
                case "place": return Place(Arg(parts, 1), parts.Length > 2 ? Float(parts, 2) : 150f);
                case "perceive": return Perceive(Arg(parts, 1), parts.Length > 2 ? Float(parts, 2) : 300f);
                case "find": return FindActors(Arg(parts, 1));
                case "journal": return JournalEntries(parts.Length > 1 ? parts[1] : string.Empty);
                case "give": return Give(Arg(parts, 1), parts.Length > 2 ? Int(parts, 2) : 1);
                case "items": return Items(Arg(parts, 1));
                case "inventory": return Inventory(parts.Length > 1 ? parts[1] : string.Empty);
                case "equipped": return Equipped();
                case "press": return Press(Arg(parts, 1), parts.Length > 2 ? Float(parts, 2) : 0.05f, parts.Length > 3 ? Float(parts, 3) : 0f);
                case "stance": return Stance(Arg(parts, 1));
                case "useitem": return UseItem(Arg(parts, 1), parts.Length > 2 ? Float(parts, 2) : 0.05f);
                case "equip": return Equip(Arg(parts, 1));
                case "unequip": return Unequip();
                case "itemsearch": return ItemSearch(Arg(parts, 1));
                case "health": return ActorHealth(Arg(parts, 1), parts.Length > 2 ? parts[2] : string.Empty);
                case "effects": return Effects(Arg(parts, 1));
                case "talents": return Talents(Arg(parts, 1), parts.Length > 2 ? parts[2] : string.Empty);
                case "talent": return Talent(Arg(parts, 1), Arg(parts, 2));
                case "effect": return Effect(Arg(parts, 1), Arg(parts, 2));
                case "objects": return Objects(Arg(parts, 1), parts.Length > 2 && parts[2] == "list");
                case "actors": return Actors(parts.Length > 1 ? Float(parts, 1) : 600f);
                case "look": return Look(Arg(parts, 1));
                case "actlog": return ActLog(Arg(parts, 1), parts.Length > 2 ? parts[2] : "me");
                case "chest": return Chest(Arg(parts, 1));
                case "takeall": return TakeAll();
                case "popup": return Popup(parts.Length > 1 ? parts[1] : string.Empty);
                case "click": return Click(Arg(parts, 1));
                case "weather": return Weather();
                case "merchant": return Merchant(parts.Length > 1 ? parts[1] : string.Empty, parts.Length > 2 ? parts[2] : string.Empty, parts.Length > 3 ? Int(parts, 3) : 1);
                case "foglets": return Foglets(parts.Length > 1 ? Float(parts, 1) : 1500f);
                case "worldevent": return WorldEvent(parts);
                case "pause": return GUIGameHandler.TryGet(out GUIGameHandler gui) && gui != null ? Opened(gui) : "no gui handler";
                case "camera": return CameraTarget(parts.Length > 1 && parts[1] == "reset");
                case "hit": return Hit(Arg(parts, 1), Int(parts, 2), parts.Length > 3 ? parts[3] : "pure", parts.Length > 4 ? parts[4] : null);
                case "use": return Use(Arg(parts, 1), parts.Length > 2 ? Float(parts, 2) : 300f);
                case "colliders": return Colliders(Arg(parts, 1), parts.Length > 2 ? Float(parts, 2) : 30f);
                case "interactables": return Interactables(parts.Length > 1 ? parts[1] : string.Empty, parts.Length > 2 ? Float(parts, 2) : 500f);
                case "talk": return Talk(Arg(parts, 1), parts.Length > 2 ? parts[2] : null);
                case "time": return GameTime();
                case "scenes": return Scenes(parts.Length > 1 ? parts[1] : string.Empty, parts.Length > 2 && parts[2] == "rearm");
                case "dialogue": return Dialogue();
                case "choose": return Choose(rest);
                case "next": return Next();
                case "bye": return Bye();

                case "skip":
                    _skipping = true;
                    _skipIdle = 0f;
                    _skipAnswering = parts.Length > 1 && parts[1] == "first";
                    return _skipAnswering
                        ? "skipping lines and taking the first answer until the dialogue ends"
                        : "skipping lines until answers are offered or the dialogue ends";

                case "log":
                    MelonLogger.Msg("[Agent] " + rest);
                    return "logged";

                // "shot <file.png>": what this instance shows, written at the end of the frame.
                case "shot":
                    ScreenCapture.CaptureScreenshot(rest);
                    return "shot " + rest;

                // "critstop on|off": the game's own no-crits cheat, for blows of a known size (a crit downs a lightly armored
                // player outright).
                case "critstop":
                    Il2CppDrova.Cheat.Cheat_CritStop.CritStop = rest == "on";
                    return "critstop " + (Il2CppDrova.Cheat.Cheat_CritStop.CritStop ? "on" : "off");

                // "netstats": what went out this session - "out=packets:bytes" on the wire (headers included), then
                // "id:count:bytes" of message payload per typed message id that sent any.
                case "netstats":
                {
                    StringBuilder stats = new();
                    LiteNetLib.NetStatistics? wire = Networking.Impl.NetworkSystem.Transport?.Manager?.Statistics;
                    if (wire != null) stats.Append("out=").Append(wire.PacketsSent).Append(':').Append(wire.BytesSent).Append(' ');
                    for (int id = 0; id < Networking.Impl.Dispatcher.MaxMessageTypes; id++)
                    {
                        long count = Networking.Diagnostics.NetworkDiagnostics.SentMessages(id);
                        if (count > 0) stats.Append(id).Append(':').Append(count).Append(':').Append(Networking.Diagnostics.NetworkDiagnostics.SentBytes(id)).Append(' ');
                    }

                    return stats.Length == 0 ? "none" : stats.ToString().TrimEnd();
                }

                // "audioclip <actor> <clip>": a dialogue clip by name from the default provider's bundles, as a co-op peer
                // looks it up; "clipalive" whether the last clip it handed out still exists; "unloadassets" Unity's sweep of
                // unused assets, which destroys a clip nothing in the game holds (it finishes over the next frames).
                case "audioclip":
                    _lastClip = Audio.AudioManager.FindDialogueClip(Arg(parts, 1), Arg(parts, 2));
                    return _lastClip == null ? "none" : _lastClip.name + " length=" + Format(_lastClip.length);
                case "clipalive":
                    return _lastClip is null ? "none" : _lastClip != null ? "alive" : "dead";
                case "unloadassets":
                    Resources.UnloadUnusedAssets();
                    return "unloading";

                default:
                    throw new InvalidOperationException("unknown command " + parts[0]);
            }
        }

        /// <summary>
        /// One line of key=value pairs the runner waits on. <c>ready</c>: the main menu has stood finished for
        /// three seconds, when a load is taken. <c>world</c>: the world is up and has been for two seconds.
        /// </summary>
        private static string State()
        {
            string position = PlayerAccess.TryGetPlayer(out Actor? player)
                ? Format(player.GetFeetPosition().x) + "," + Format(player.GetFeetPosition().y)
                : "-";

            // A load taken between the two menu builds came up half-loaded: the save loader failed on the quickbar
            // and ten thousand errors followed. Hence the menu scene, no stall and a longer quiet.
            bool ready = _menuSince != null && Time.realtimeSinceStartup - _menuSince > 6f;
            bool world = _worldSince != null && Time.realtimeSinceStartup - _worldSince > 2f;

            StringBuilder text = new($"ready={(ready ? 1 : 0)} world={(world ? 1 : 0)} pos={position} " +
                $"profile={LaunchProfile.Name ?? "-"} saves={(LaunchProfile.Separate ? "separate" : "shared")} " +
                $"gameplay={(_inGameplay ? 1 : 0)} loadingscreen={(SceneGameHandler.IsLoadingScreenActive ? 1 : 0)} " +
                $"pendingload={(_pendingLoad != null ? 1 : 0)} timescale={Format(Time.timeScale)} " +
                $"focused={(Application.isFocused ? 1 : 0)} cam={Format(CameraGameHandler.CamPos2D.x)},{Format(CameraGameHandler.CamPos2D.y)} " +
                $"teleporting={(SceneGameHandler.IsTeleportingPlayer ? 1 : 0)}");

            // What the logged body played, without timings: a scenario compares a teammate's copy against it.
            if (_logged != null) text.Append(" actlog=").Append(ActLogBrief());

            foreach (Func<string> contributor in StateProviders)
            {
                text.Append(' ').Append(contributor());
            }

            return text.ToString();
        }

        private static bool IsMenuUp()
        {
            return _menuLoaded && !_inGameplay && !SceneGameHandler.IsLoadingScreenActive
                && SavegameGameHandler.TryGet(out SavegameGameHandler handler) && handler != null;
        }

        /// <summary>The same test <see cref="PlayerAccess.OnPlayerFound"/> waits on, polled.</summary>
        private static bool IsWorldUp()
        {
            if (!_inGameplay || SceneGameHandler.IsLoadingScreenActive) return false;

            LoadingScreenHandler? loading = LoadingScreenHandler.Instance;
            if (loading == null || !loading.IsWorldReady()) return false;

            return PlayerAccess.TryGetPlayer(out Actor? player) && player._isInitialized;
        }

        private static string Run(string command)
        {
            if (command.Length == 0) throw new InvalidOperationException("run needs a command");

            List<string> said = new();
            _capture = said;

            try
            {
                CheatMenuAccess.FireCommand(command);
            }
            finally
            {
                _capture = null;
            }

            return string.Join(" | ", said);
        }

        private static string Load(int hero, int slot)
        {
            if (!SavegameGameHandler.TryGet(out SavegameGameHandler handler) || handler == null)
            {
                throw new InvalidOperationException("no savegame handler yet");
            }

            _pendingLoad = (hero, slot, Time.realtimeSinceStartup);

            bool started = handler.TryLoadSavegameSync(new SavegameIdentifier(hero, slot), true);
            return started ? $"loading hero {hero} slot {slot}" : throw new InvalidOperationException("the game refused the load");
        }

        /// <summary>
        /// <c>save &lt;slot&gt;</c>: saves the running game into that slot of the current hero - a fixture save for a
        /// later scenario, taken after its preconditions were set and reloaded so routines and scenery rebuild from them.
        /// Only in a profile of its own: never into the player's real savegames.
        /// </summary>
        private static string Save(int slot)
        {
            if (!LaunchProfile.Separate) throw new InvalidOperationException("saves are not separate - refusing to write the real savegames");
            if (!SavegameGameHandler.TryGet(out SavegameGameHandler handler) || handler == null) throw new InvalidOperationException("no savegame handler yet");

            int hero = handler._currentSavegame.MetaInfo.GetIdentifier().HeroIndex;
            handler.SaveCurrentAt(new SavegameIdentifier(hero, slot), SaveOptions.Default, true);
            return $"saving hero {hero} slot {slot}";
        }

        private static AGVarBase Find(string[] parts)
        {
            string listName = Arg(parts, 1);
            string varName = Arg(parts, 2);

            SubDatabase_GVars database = ProviderAccess.GVarDatabase ?? throw new InvalidOperationException("no gvar database yet");

            for (int index = 0; index < database.AllGVars.Count; index++)
            {
                GVarList list = database.AllGVars[index];
                if (list == null || list.name != listName) continue;

                return list.GetGVarByName(varName) ?? throw new InvalidOperationException($"{listName} has no {varName}");
            }

            throw new InvalidOperationException("no list " + listName);
        }

        private static string Describe(AGVarBase gvar)
        {
            if (gvar.TryCast<AGVar<bool>>() is { } asBool) return asBool.GetValue() ? "1" : "0";
            if (gvar.TryCast<AGVar<int>>() is { } asInt) return asInt.GetValue().ToString(CultureInfo.InvariantCulture);
            if (gvar.TryCast<AGVar<float>>() is { } asFloat) return Format(asFloat.GetValue());
            if (gvar.TryCast<AGVar<QuestState>>() is { } asQuest) return ((int)asQuest.GetValue()).ToString(CultureInfo.InvariantCulture);

            throw new InvalidOperationException("not a bool, int, float or quest state");
        }

        /// <summary>Sets through the game's own SetValue, so listeners and other mods see a story write.</summary>
        private static string Set(AGVarBase gvar, string value)
        {
            if (gvar.TryCast<AGVar<bool>>() is { } asBool) asBool.SetValue(value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase), GVarContext.Empty);
            else if (gvar.TryCast<AGVar<int>>() is { } asInt) asInt.SetValue(int.Parse(value, CultureInfo.InvariantCulture), GVarContext.Empty);
            else if (gvar.TryCast<AGVar<float>>() is { } asFloat) asFloat.SetValue(Parse(value), GVarContext.Empty);
            else if (gvar.TryCast<AGVar<QuestState>>() is { } asQuest) asQuest.SetValue((QuestState)int.Parse(value, CultureInfo.InvariantCulture), GVarContext.Empty);
            else throw new InvalidOperationException("not a bool, int, float or quest state");

            return Describe(gvar);
        }

        private static string Teleport(float x, float y)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            // A player cannot walk away from a conversation, but a teleport can: the scene's NPCs then unload while the
            // conversation still speaks through them, and the dialogue window breaks on the next line - for good.
            // So a running conversation is ended first, the way the game ends one - for a jump far enough to unload the
            // scene; a step next to the speaker keeps it.
            if ((new Vector2(x, y) - player.GetFeetPosition()).magnitude > 1000f
                && Il2CppDrova.DialogueNew.DialogueSystemGameHandler.TryGet(out Il2CppDrova.DialogueNew.DialogueSystemGameHandler dialogues)
                && dialogues != null && dialogues.ActivePlayerDialogue != null && dialogues.ActivePlayerDialogue.isRunning)
            {
                Bye();
            }

            player.TeleportTo(new Vector2(x, y), portWithGroup: false, updateCam: true);
            return Format(x) + "," + Format(y);
        }

        /// <summary><c>tpnear &lt;guid&gt; &lt;dx&gt; [dy]</c>: this player to that actor, offset by dx, dy.</summary>
        private static string TeleportNear(string guid, float dx, float dy)
        {
            Vector2 at = ActorPosition(guid) + new Vector2(dx, dy);
            return Teleport(at.x, at.y);
        }

        private static EntityGameHandler Entities()
        {
            return EntityGameHandler.TryGet(out EntityGameHandler handler) && handler != null
                ? handler
                : throw new InvalidOperationException("no entity handler yet");
        }

        /// <summary>Where an actor stands: by its lazy-actor guid the live body if loaded, the placement otherwise; else by name.</summary>
        private static Vector2 ActorPosition(string guid)
        {
            Actor actor = Entities().GetActorFromGuid(guid);
            if (actor != null) return actor.GetFeetPosition();

            LazyActor lazy = Entities().GetLazyActorFromGuid(guid);
            if (lazy != null) return lazy.GetFeetPosition();

            // Not an authored guid: a name, like every other command takes ("tpnear DrovaCoop.Remote" - a teammate's body).
            return ResolveActor(guid).GetFeetPosition();
        }

        /// <summary><c>near &lt;guid&gt; &lt;range&gt;</c>: 1 when that actor is within range of this player, the distance, and whether it is loaded here.</summary>
        private static string Near(string guid, float range)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            float distance = (ActorPosition(guid) - player.GetFeetPosition()).magnitude;

            // Loaded means ready to act on: a streamed-in body is findable a few frames before it has finished
            // initializing, and starting a dialogue on it then throws inside the game.
            Actor? body = Entities().GetActorFromGuid(guid);
            bool loaded = body != null && body.IsInitialized;
            return (distance <= range ? "1" : "0") + " dist=" + Format(distance) + (loaded ? " loaded" : " lazy");
        }

        /// <summary>
        /// <c>walk &lt;dx&gt; &lt;dy&gt; &lt;seconds&gt;</c>: walks this player in that direction for that long at the game's
        /// own speed, turning 45 degrees whenever it stops making progress. Answers at once; check how far it
        /// really got with <c>walked</c> - a walk into a fence goes nowhere without complaint.
        /// </summary>
        private static string Walk(float dx, float dy, float seconds)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");
            if (seconds <= 0f) throw new InvalidOperationException("seconds must be positive");

            _route = null;
            _walk = (new Vector2(dx, dy).normalized, seconds, 0f, player.GetFeetPosition(), 0f);
            _walkStart = player.GetFeetPosition();
            _walkedDistance = 0f;
            return $"walking {Format(dx)},{Format(dy)} over {Format(seconds)}s";
        }

        /// <summary>
        /// <c>walkto &lt;x,y|guid&gt; [seconds] [stop]</c>: walks this player there along the path the NPCs' own A* graph
        /// finds, until within <c>stop</c> units or out of time. Unlike a teleport it crosses every trigger volume on
        /// the way and moves like a player to a teammate's machine. The graph only covers the area around the
        /// player, so a far target is refused: teleport closer first.
        /// </summary>
        private static string WalkTo(string target, float seconds, float stop)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");
            if (seconds <= 0f) throw new InvalidOperationException("seconds must be positive");

            Vector2 goal;
            string[] xy = target.Split(',');
            if (xy.Length == 2 && float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                goal = new Vector2(x, y);
            }
            else
            {
                Actor body = Entities().GetActorFromGuid(target) ?? throw new InvalidOperationException("no loaded actor " + target);
                goal = body.GetFeetPosition();
            }

            Vector2 from = player.GetFeetPosition();
            Il2CppPathfinding.ABPath path = Il2CppPathfinding.ABPath.Construct(new Vector3(from.x, from.y, 0f), new Vector3(goal.x, goal.y, 0f));
            Il2Cpp.AstarPath.StartPath(path);
            Il2Cpp.AstarPath.BlockUntilCalculated(path);
            if (path.error || path.vectorPath == null || path.vectorPath.Count == 0)
                throw new InvalidOperationException("no path: " + path.errorLog);

            List<Vector2> points = new();
            float length = 0f;
            Vector2 previous = from;
            foreach (Vector3 point in path.vectorPath)
            {
                Vector2 flat = new(point.x, point.y);
                length += (flat - previous).magnitude;
                previous = flat;
                points.Add(flat);
            }

            _route = (points, 0, stop);
            _walk = ((points[0] - from).normalized, seconds, 0f, from, 0f);
            _walkStart = from;
            _walkedDistance = 0f;
            return $"walking a {points.Count}-point path, {Format(length)} long";
        }

        /// <summary>Points the walk at the route's next waypoint; true once the route's end is reached.</summary>
        private static bool SteerRoute(Vector2 here, ref Vector2 direction)
        {
            if (_route is not { } route) return false;

            int next = route.Next;
            while (next < route.Points.Count - 1 && (route.Points[next] - here).sqrMagnitude < 24f * 24f) next++;
            _route = (route.Points, next, route.Stop);

            Vector2 end = route.Points[^1];
            if ((end - here).sqrMagnitude <= route.Stop * route.Stop) return true;

            Vector2 toward = route.Points[next] - here;
            if (toward.sqrMagnitude > 0.01f) direction = toward.normalized;
            return false;
        }

        private static void TickWalk()
        {
            if (_walk is not { } walk || !PlayerAccess.TryGetPlayer(out Actor? walker)) return;

            float elapsed = walk.Elapsed + Time.deltaTime;
            float sinceCheck = walk.SinceCheck + Time.deltaTime;
            Vector2 direction = walk.Direction;
            Vector2 lastPos = walk.LastPos;
            bool done = elapsed >= walk.Seconds || SteerRoute(walker.GetFeetPosition(), ref direction);

            // A route steers itself; the stuck-turn is for a straight walk into a fence.
            if (_route == null && sinceCheck >= 0.5f)
            {
                Vector2 here = walker.GetFeetPosition();
                if ((here - lastPos).sqrMagnitude < 15f * 15f) direction = Rotate(direction, 45f);

                lastPos = here;
                sinceCheck = 0f;
            }

            _walk = done ? null : (direction, walk.Seconds, elapsed, lastPos, sinceCheck);
            _walkedDistance = (walker.GetFeetPosition() - _walkStart).magnitude;

            if (done)
            {
                _route = null;
                walker.GetMovementModule()?.SetDirection(Vector2.zero);
            }
        }

        /// <summary>Whether the desktop's keyboard and mouse reach the game. Off while a script drives it.</summary>
        private static bool _realInput;

        /// <summary>
        /// Keeps the keyboard and mouse switched off at the device while a script drives the game. A launched
        /// instance takes the desktop's focus, and somebody playing in another window then swung, drank and
        /// sheathed on the scripted player - indistinguishable from a sync fault. Off at the device rather
        /// than by skipping the game's input modules: those also let go of every button that is not held,
        /// each frame, and skipping them changed how attacks and walks behave. Asserted every frame, because
        /// the game can switch controllers back on.
        /// </summary>
        private static void HoldRealInput()
        {
            if (!Il2CppRewired.ReInput.isReady) return;

            Il2CppRewired.ReInput.ControllerHelper controllers = Il2CppRewired.ReInput.controllers;
            if (controllers.Keyboard != null && controllers.Keyboard.enabled != _realInput) controllers.Keyboard.enabled = _realInput;
            if (controllers.Mouse != null && controllers.Mouse.enabled != _realInput) controllers.Mouse.enabled = _realInput;
        }

        private static bool MovementInputPrefix(Il2CppDrova.InputModules.PlayerInput_Movement __instance)
        {
            if (_walk is not { } walk) return true;

            Actor.MovementModule movement = __instance._movement;
            if (movement == null) return true;

            movement.SetIntendedMode(Actor.EMoveMode.Walk);
            movement.SetDirection(walk.Direction);
            return false;
        }

        /// <summary><c>dead &lt;guid&gt;</c>: 1 when that creature is dead here, 0 when alive, and whether a loaded body or the lazy placeholder said so.</summary>
        private static string Dead(string guid)
        {
            Actor actor = Entities().GetActorFromGuid(guid);
            if (actor != null)
            {
                Health health = actor.GetHealth() ?? throw new InvalidOperationException("no health: " + guid);
                return (health.IsDead ? "1" : "0") + " loaded";
            }

            LazyActor lazy = Entities().GetLazyActorFromGuid(guid) ?? throw new InvalidOperationException("no actor " + guid);
            return (lazy.IsDead() ? "1" : "0") + " lazy";
        }

        /// <summary><c>kill &lt;guid&gt;</c>: kills that creature here, the ordinary way a death happens, if it is loaded.</summary>
        private static string Kill(string guid)
        {
            Actor actor = Entities().GetActorFromGuid(guid) ?? throw new InvalidOperationException("not loaded here: " + guid);
            Health health = actor.GetHealth() ?? throw new InvalidOperationException("no health: " + guid);
            if (health.IsDead) return "already dead";

            health.SetHealth(0, null);
            return health.IsDead ? "killed" : "the game kept it alive";
        }

        /// <summary><c>counters [fragment]</c>: the kill-counting quest observers in the loaded scenes, as "name:killed/total:guid,guid,...;".</summary>
        private static string Counters(string fragment)
        {
            StringBuilder text = new();

            foreach (GlobalObserverBhvr_CountKilledActors counter in Resources.FindObjectsOfTypeAll<GlobalObserverBhvr_CountKilledActors>())
            {
                if (counter == null || !counter.gameObject.scene.IsValid()) continue;

                string name = counter.gameObject.name;
                if (fragment.Length > 0 && !name.Contains(fragment, StringComparison.OrdinalIgnoreCase)) continue;

                Il2CppSystem.Collections.Generic.List<LazyActor> actors = counter._lazyActors;
                List<string> guids = new();
                for (int index = 0; actors != null && index < actors.Count; index++)
                {
                    if (actors[index] != null) guids.Add(actors[index].GUID);
                }

                text.Append($"{name}:{counter._killedActors}/{guids.Count}:{string.Join(",", guids)};");
            }

            return text.Length == 0 ? "none" : text.ToString();
        }

        /// <summary>
        /// <c>inside &lt;name fragment&gt;</c>: 1 when this player is in that trigger volume by the game's own
        /// flag, then the collider overlap, whether the volume is active, and its full name.
        /// </summary>
        /// <summary>
        /// <c>tpinto &lt;volume name fragment&gt;</c>: teleports the player to a point inside that trigger volume's shape - the
        /// middle of its bounds if that is inside, else the first point of a grid over the bounds that is. A volume's own
        /// position is only its anchor; its polygon can lie well off it.
        /// </summary>
        private static string TeleportInto(string fragment)
        {
            HitReceiveHandler handler = Resources.FindObjectsOfTypeAll<HitReceiveHandler>()
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene.IsValid()
                    && candidate.gameObject.name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("no volume named like " + fragment);

            Collider2D receive = handler.GetReceiveCollider() ?? throw new InvalidOperationException("the volume has no collider");
            Bounds bounds = receive.bounds;
            Vector2 at = bounds.center;

            for (int step = 1; !receive.OverlapPoint(at) && step <= 64; step++)
            {
                int row = step / 8;
                at = new Vector2(bounds.min.x + bounds.size.x * (step % 8 + 0.5f) / 8f, bounds.min.y + bounds.size.y * (row + 0.5f) / 8f);
            }

            if (!receive.OverlapPoint(at)) throw new InvalidOperationException("found no point inside " + handler.gameObject.name);
            return Teleport(at.x, at.y);
        }

        private static string Inside(string fragment)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            // By name fragment, inactive ones included: GameObject.Find sees neither.
            HitReceiveHandler? handler = null;
            foreach (HitReceiveHandler candidate in Resources.FindObjectsOfTypeAll<HitReceiveHandler>())
            {
                if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;
                if (!candidate.gameObject.name.Contains(fragment, StringComparison.OrdinalIgnoreCase)) continue;

                handler = candidate;
                break;
            }

            if (handler == null) throw new InvalidOperationException("no volume named like " + fragment);

            Collider2D receive = handler.GetReceiveCollider();
            bool overlaps = receive != null && receive.OverlapPoint(player.GetFeetPosition());

            // What a player-only volume polls: whether it counts as loaded (camera-gated) and which actor it was
            // handed as "the player" when it started - a volume with no player never fires for anybody.
            string bound = !handler._hasPlayerActor ? "none"
                : handler._playerActor != null && handler._playerActor.Pointer == player.Pointer ? "local" : "other";

            return $"{(handler._isInCollider ? 1 : 0)} overlap={(overlaps ? 1 : 0)} active={(handler.isActiveAndEnabled ? 1 : 0)} " +
                $"loaded={(handler.IsLoaded ? 1 : 0)} player={bound} name={handler.gameObject.name}";
        }

        /// <summary><c>ai &lt;guid&gt;</c>: whether that actor's AI runs here and which routine place it is on.</summary>
        /// <summary>
        /// <c>port &lt;guid&gt;</c>: sends an NPC to its current routine place the way the game's story observers do
        /// (<c>LazyActor.ForceActorToPortToNextRoutine</c>). A fixture that sets story flags gets the right routines,
        /// but the bodies stay where the older routine left them until something ports them.
        /// </summary>
        /// <summary>
        /// <c>perceive &lt;fragment|*&gt; [range]</c>: what a player's perception does to hidden objects near them - a wall
        /// switch, a pedestal with a relic - once the investigation timer has run out: they become investigable, and
        /// usable. Answers the ones switched, by parent/name.
        /// </summary>
        private static string Perceive(string fragment, float range)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Il2CppDrova.Perception.PPT_PerceptionHandler handler = player.GetComponentInChildren<Il2CppDrova.Perception.PPT_PerceptionHandler>(true)
                ?? throw new InvalidOperationException("the player has no perception handler");

            Vector2 origin = player.GetFeetPosition();
            List<string> switched = new();
            foreach (Il2CppDrova.Perception.PPT_PerceptionTriggerHandler trigger in UnityEngine.Object.FindObjectsOfType<Il2CppDrova.Perception.PPT_PerceptionTriggerHandler>())
            {
                if (trigger == null || trigger.CurrentPerceptionMode == Il2CppDrova.Perception.PPT_PerceptionTriggerHandler.PerceptionMode.Investigateable) continue;

                string name = (trigger.transform.parent != null ? trigger.transform.parent.name + "/" : "") + trigger.name;
                if (fragment != "*" && name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0) continue;

                Vector2 at = trigger.TriggerPosition != null ? trigger.TriggerPosition.position : trigger.transform.position;
                float distance = (at - origin).magnitude;
                if (distance > range) continue;

                var args = new Il2CppDrova.Perception.PPT_PerceptionTriggerHandler.PerceptionArgs(Il2CppDrova.Perception.PPT_PerceptionHandler.PerceptionCircle.Inner, handler, distance);
                args.PerceptionMode = Il2CppDrova.Perception.PPT_PerceptionTriggerHandler.PerceptionMode.Investigateable;
                trigger.SetNewPerceptionMode(args);
                switched.Add(name);
            }

            return switched.Count == 0 ? "none" : string.Join(";", switched);
        }

        /// <summary>
        /// <c>place &lt;item&gt; [range]</c>: puts an item on the nearest item place (a riddle plinth) the way its chooser
        /// window's "put" does - the puzzle checks it and, if right, takes it from the player. The window itself is
        /// closed. Answers the place and the item count left.
        /// </summary>
        private static string Place(string readableId, float range)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Il2CppDrova.Items.Item item = ResolveItem(readableId);
            Vector2 origin = player.GetFeetPosition();

            Interact_Bhvr_ItemPlace? nearest = null;
            float best = range;
            foreach (Interact_Bhvr_ItemPlace place in UnityEngine.Object.FindObjectsOfType<Interact_Bhvr_ItemPlace>())
            {
                if (place == null) continue;
                float distance = ((Vector2)place.transform.position - origin).magnitude;
                if (distance > best) continue;

                best = distance;
                nearest = place;
            }

            if (nearest == null) throw new InvalidOperationException("no item place within " + range);

            nearest._chooserGUI?.Close();
            nearest._chooserGUI = null;
            nearest.SetItem(item);
            // The shipping build raises (item, place); ManagedDrova.dll still shows the item alone.
            nearest.OnItemChangedEvent.InvokeEvent(new Il2CppSystem.ValueTuple<Il2CppDrova.Items.Item, Interact_Bhvr_ItemPlace>(item, nearest));

            return $"placed on {(nearest.transform.parent != null ? nearest.transform.parent.name : nearest.name)}; {Items(readableId)} left";
        }

        private static string PortToRoutine(string guid)
        {
            LazyActor lazy = Entities().GetLazyActorFromGuid(guid)
                ?? throw new InvalidOperationException("no lazy actor " + guid);

            lazy.ForceActorToPortToNextRoutine();
            return "porting " + lazy.name;
        }

        private static string Brain(string guid)
        {
            Actor actor = Entities().GetActorFromGuid(guid);
            if (actor == null && Entities().GetLazyActorFromGuid(guid) != null) return "lazy";
            actor ??= ResolveActor(guid);

            bool active = actor.AIModule != null && actor.AIModule.IsAIActiveAndValid();
            Il2CppDrova.Routine.IRoutinePlace? place = actor._routineModule?.GetActiveRoutinePlace();
            string key = actor._routineModule?.GetActiveRoutinePlaceKey() ?? "-";

            Component? component = place?.Cast<Il2CppSystem.Object>().TryCast<Component>();
            string routine = component == null ? "-" : (component.transform.parent != null ? component.transform.parent.name + "/" : "") + component.name;
            Vector2 feet = actor.GetFeetPosition();

            return $"active={(active ? 1 : 0)} place={(place == null ? "-" : place.Cast<Il2CppSystem.Object>().GetIl2CppType().Name)} key={key} routine={routine} pos={Format(feet.x)},{Format(feet.y)} " +
                $"ko={(actor.CustomStats.IsKnockout ? 1 : 0)} brawl={(actor.CustomStats.IsInBrawl ? 1 : 0)} dead={(actor.GetHealth() != null && actor.GetHealth().IsDead ? 1 : 0)} " +
                $"talking={(actor.DialogueModule != null && actor.DialogueModule.IsInDialogue() ? 1 : 0)} ready={(actor.DialogueModule != null && actor.DialogueModule.IsReadyForPlayerDialogue() ? 1 : 0)} " +
                $"action={(actor.GetActionModule() is { HasCurrentAction: true } actions && actions.CurrentAction != null ? actions.CurrentAction.name.Replace(' ', '_') : "-")} " +
                $"torch={(actor.TorchTimer != null && actor.TorchTimer.IsUsingTorch ? 1 : 0)} mode={actor.GetInteractionModule()?.CurrentMode.ToString() ?? "-"} " +
                $"home={(actor.CustomStats != null ? Format(actor.CustomStats.DefaultPosition.x) + "," + Format(actor.CustomStats.DefaultPosition.y) : "-")} " +
                $"homereq={(actor.CustomStats != null && actor.CustomStats.RequestSetNewDefaultPos ? 1 : 0)}{(actor.CustomStats != null && actor.CustomStats.RequestDefaultRoutinePosChange ? 1 : 0)} " +
                $"target={Target(actor)}";
        }

        private static void SubtitlesPrefix(SubtitlesRequestInfo info)
        {
            // A line continued from here keeps its window routine running, which continues it again once done - and
            // the tree continues whatever node is current by then: a question was skipped past unanswered, and the
            // routine's leftover text broke on a window already gone. A line moves the tree on once, whoever is first.
            Il2CppSystem.Action? original = info.Continue;
            if (original != null)
            {
                bool continued = false;
                info.Continue = (Action)(() =>
                {
                    if (continued) return;
                    continued = true;
                    original.Invoke();
                });
            }

            _line = info;
            _lineNode = CurrentNode();
            _lineSince = Time.unscaledTime;
        }

        private static void ChoicesPrefix(DS_DialogueUGUI __instance, MultipleChoiceRequestInfo info)
        {
            _choices = (info, __instance);
            _choicesNode = CurrentNode();

            // A press still waiting would be read by the answer window as picking the highlighted answer.
            _pressSkip = false;

            // Some answer windows count down and then take the last answer themselves. A player sees the timer
            // fade; a scripted "choose" polling a moment later finds the question already answered, or a stale
            // answer list. The prefix runs before the window reads the time, so a test run gets no countdown.
            info.availableTime = 0f;

            // The line before the answers is over. Continuing it later would land on the answers and pick
            // the first one - a "skip" right after a "choose" did exactly that.
            _line = null;
        }

        /// <summary>
        /// <c>find &lt;name fragment&gt;</c>: authored actors whose spawner is named like that, as "name=guid;" -
        /// the guid every other actor command takes.
        /// </summary>
        private static string FindActors(string fragment)
        {
            StringBuilder text = new();

            foreach (LazyActor lazy in Resources.FindObjectsOfTypeAll<LazyActor>())
            {
                if (lazy == null || !lazy.gameObject.scene.IsValid()) continue;
                if (!lazy.gameObject.name.Contains(fragment, StringComparison.OrdinalIgnoreCase)) continue;

                text.Append(lazy.gameObject.name).Append('=').Append(lazy.GUID).Append(';');
            }

            return text.Length == 0 ? "none" : text.ToString();
        }

        /// <summary>
        /// <c>journal [fragment]</c>: the journal entries this player holds, by resource path, filtered by
        /// fragment and joined by ";". The path is the identity the savegame stores.
        /// </summary>
        private static string JournalEntries(string fragment)
        {
            if (!PlayerMetaDataGameHandler.TryGet(out PlayerMetaDataGameHandler handler) || handler == null)
            {
                throw new InvalidOperationException("no journal yet");
            }

            Il2CppDrova.JournalSystem.Journal journal = handler.GetJournal() ?? throw new InvalidOperationException("no journal yet");
            List<string> paths = new();
            Il2CppSystem.Collections.Generic.List<Il2CppDrova.JournalSystem.JournalEntry> entries = new();

            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Il2CppDrova.JournalSystem.AJournalTopic, Il2CppDrova.JournalSystem.Journal_RuntimeTopic> pair in journal._topicLookUp)
            {
                if (pair.Value == null) continue;

                entries.Clear();
                pair.Value.GetEntries(ref entries);

                for (int index = 0; index < entries.Count; index++)
                {
                    string? path = entries[index] == null ? null : entries[index].GetResourcesPath();
                    if (path != null && (fragment.Length == 0 || path.Contains(fragment, StringComparison.OrdinalIgnoreCase))) paths.Add(path);
                }
            }

            return paths.Count == 0 ? "none" : string.Join(";", paths);
        }

        /// <summary><c>give &lt;readable id&gt; [amount]</c>: puts items into this player's inventory; answers the count after.</summary>
        private static string Give(string readableId, int amount)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Il2CppDrova.Items.Item item = ResolveItem(readableId);

            player.GetInventory().AddItem(new Il2CppDrova.Items.ItemStack(item, amount));
            return Items(readableId);
        }

        /// <summary>An item by its readable id, or failing that by its asset name - the name asset dumps show.</summary>
        private static Il2CppDrova.Items.Item ResolveItem(string id)
        {
            Il2CppDrova.Items.Item? item = ProviderAccess.ItemDatabase?.GetItemByReadableId(id);
            if (item != null) return item;

            foreach (Il2CppDrova.Items.Item candidate in Resources.FindObjectsOfTypeAll<Il2CppDrova.Items.Item>())
            {
                if (candidate != null && candidate.name == id) return candidate;
            }

            throw new InvalidOperationException("no item " + id);
        }

        /// <summary><c>items &lt;readable id&gt;</c>: how many of that item this player carries.</summary>
        private static string Items(string readableId)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Il2CppDrova.Items.Item item = ResolveItem(readableId);

            return player.GetInventory().GetAmount(item).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary><c>inventory [fragment]</c>: what this player carries, as "id:amount;".</summary>
        private static string Inventory(string fragment)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            return Stacks(player.GetInventory().Cast<Il2CppDrova.InventorySystem.IInventoryContainer>(), fragment);
        }

        private static string Stacks(Il2CppDrova.InventorySystem.IInventoryContainer? inventory, string fragment)
        {
            if (inventory == null) return "none";

            StringBuilder text = new();
            foreach (Il2CppDrova.Items.ItemStack stack in inventory.InventoryItems)
            {
                string? id = stack?.Item?.ReadableId;
                if (id == null || stack!.Amount <= 0) continue;
                if (fragment.Length > 0 && !id.Contains(fragment, StringComparison.OrdinalIgnoreCase)) continue;

                text.Append(id).Append(':').Append(stack.Amount).Append(';');
            }

            return text.Length == 0 ? "none" : text.ToString();
        }

        /// <summary><c>equipped</c>: what this player wears and holds, by readable id, placeholders left out.</summary>
        private static string Equipped()
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Actor.EquipmentModule equipment = player.GetEquipmentModule() ?? throw new InvalidOperationException("no equipment yet");
            List<string> ids = new();

            foreach (ActorEquipSlot slot in equipment.AllItemSlotsRef)
            {
                if (slot == null || slot.IsDummy) continue;

                Il2CppDrova.Items.Item item = slot.GetItemOnSlot();
                if (item == null) continue;

                // Proxies over native objects: compare pointers, not instances.
                Il2CppDrova.Items.Item placeholder = equipment.GetEmptyItemForSlot(slot.GetSlotType());
                if (placeholder != null && placeholder.Pointer == item.Pointer) continue;

                ids.Add(item.ReadableId);
            }

            return ids.Count == 0 ? "none" : string.Join(";", ids);
        }

        /// <summary>
        /// <c>chest &lt;guid&gt;</c>: what a loaded container holds, as "id:amount;". The guid is any guid component
        /// between the container and its scene root - usually its save key.
        /// </summary>
        private static string Chest(string guid)
        {
            foreach (Interact_Bhvr_LootInventory container in Resources.FindObjectsOfTypeAll<Interact_Bhvr_LootInventory>())
            {
                if (container == null || !container.gameObject.scene.IsValid()) continue;

                for (Transform? node = container.transform; node != null; node = node.parent)
                {
                    if (node.GetComponent<GuidComponent>()?.GuidString == guid) return Stacks(container.OwnerInventory, string.Empty);
                }
            }

            throw new InvalidOperationException("no loaded container " + guid);
        }

        /// <summary>
        /// <c>camera [reset]</c>: what the camera follows and how many temporary shots are stacked on it; "reset"
        /// drops them all and hands the camera back to the player. A shot a cutscene never released leaves the
        /// camera away from the player - and camera-loaded volumes around the player switched off.
        /// </summary>
        private static string CameraTarget(bool reset)
        {
            if (!CameraGameHandler.TryGet(out CameraGameHandler camera) || camera == null) throw new InvalidOperationException("no camera yet");
            if (reset) camera.ResetAllTargets();

            Camera? active = camera._activeCamera;
            CameraMovement? movement = active != null ? active.GetComponent<CameraMovement>() : null;
            Transform? target = movement?.TargetTransform;
            string name = target == null ? "none" : PathOf(target);
            int stacked = camera._previousTargets?.Count ?? -1;

            return $"target={name} stacked={stacked} temporary={(camera.HasTemporaryCamTarget ? 1 : 0)}";
        }

        /// <summary>
        /// <c>takeall</c>: presses "take all" on the open loot window, as the button does - which also closes it
        /// and lets the container do what it does on close (the mine's stash chest re-equips the saved loadout).
        /// </summary>
        private static string TakeAll()
        {
            foreach (Il2CppDrova.GUI.GUI_LootInventory window in Resources.FindObjectsOfTypeAll<Il2CppDrova.GUI.GUI_LootInventory>())
            {
                if (window == null || !window.isActiveAndEnabled) continue;

                window.TakeAll();
                return "taken";
            }

            throw new InvalidOperationException("no loot window open");
        }

        /// <summary>
        /// <c>hit &lt;guid&gt; &lt;amount&gt; [kind] [nosource|striker]</c>: a blow from this player, before the target's
        /// defenses - attributed, so it aggroes, counts in a brawl and credits the kill. A striker names another actor to
        /// strike as, such as a creature hitting the player. Answers the health left.
        /// </summary>
        private static string Hit(string guid, int amount, string kind, string? source)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            Actor? striker = source == null ? player : source == "nosource" ? null : ResolveActor(source);

            Actor target = ResolveActor(guid);
            DamageStatDesc? type;
            bool found = kind switch
            {
                "physical" => DamageAccess.TryGetPhysicalDamageType(out type),
                "magical" => DamageAccess.TryGetMagicalDamageType(out type),
                _ => DamageAccess.TryGetPureDamageType(out type),
            };
            if (!found || type == null) throw new InvalidOperationException("no damage type yet");
            if (!DamageAccess.TryApplyDamage(target, amount, type, striker)) throw new InvalidOperationException("the blow did not land");

            Health health = target.GetHealth();
            float distance = (target.GetFeetPosition() - player.GetFeetPosition()).magnitude;
            return $"health={health.CurHealth}" + (health.IsDead ? " dead" : string.Empty) + $" dist={Format(distance)}";
        }

        /// <summary>
        /// <c>use &lt;name fragment&gt; [range]</c>: interacts with the nearest interactable named like that (its object
        /// or a parent), as the interact key does - a bed, a chest, a pickup, a work spot. Work-spot minigames
        /// are skipped with full success, the game's own "minigames off" path. A player out of the trigger's
        /// reach is first stood on its interact point, where walking up would have put them.
        /// </summary>
        private static string Use(string fragment, float range)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            InteractableTrigger trigger = NearbyInteractables(player, fragment, range).FirstOrDefault().Trigger
                ?? throw new InvalidOperationException($"nothing named like {fragment} within {Format(range)}");

            trigger.GetComponentInParent<Interact_Bhvr_ResourceSpot>()?.GetMinigameModule()?.SetSkip(true);
            foreach (Interact_Bhvr_ResourceSpot spot in trigger.GetComponentsInChildren<Interact_Bhvr_ResourceSpot>()) spot.GetMinigameModule()?.SetSkip(true);

            if (!trigger.IsActorInRange(player))
            {
                Vector2 at = trigger.GetInteractPosition();
                Teleport(at.x, at.y);
            }

            // The game's own refusals, named, so a scenario says why rather than just "refused".
            Interaction? interaction = trigger.RequestInteraction(player);
            if (interaction == null) throw new InvalidOperationException("no interaction offered here: " + PathOf(trigger.transform));
            if (!interaction.CanInteract(player)) throw new InvalidOperationException("not usable now: " + PathOf(trigger.transform));

            return trigger.TryStartInteractAction(player)
                ? "used " + PathOf(trigger.transform)
                : throw new InvalidOperationException("the game refused " + PathOf(trigger.transform));
        }

        /// <summary>
        /// <c>colliders x,y [radius]</c>: every 2D collider overlapping that circle, as
        /// "path(trigger|solid,layer,enabled);" - what is standing in the player's way at a spot.
        /// </summary>
        private static string Colliders(string xy, float radius)
        {
            string[] parts = xy.Split(',');
            if (parts.Length != 2) throw new InvalidOperationException("colliders x,y [radius]");

            Vector2 at = new(Parse(parts[0]), Parse(parts[1]));
            StringBuilder text = new();

            foreach (Collider2D collider in Physics2D.OverlapCircleAll(at, radius))
            {
                if (collider == null) continue;

                text.Append($"{PathOf(collider.transform)}({(collider.isTrigger ? "trigger" : "solid")},{LayerMask.LayerToName(collider.gameObject.layer)},{(collider.enabled ? 1 : 0)});");
            }

            return text.Length == 0 ? "none" : text.ToString();
        }

        /// <summary><c>interactables [fragment | *] [range]</c>: interactables near this player, nearest first, as "parent/name@x,y dist;".</summary>
        private static string Interactables(string fragment, float range)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            StringBuilder text = new();
            foreach ((InteractableTrigger trigger, float distance) in NearbyInteractables(player, fragment, range))
            {
                Vector2 at = trigger.transform.position;
                text.Append($"{PathOf(trigger.transform)}@{Format(at.x)},{Format(at.y)} {Format(distance)};");
            }

            return text.Length == 0 ? "none" : text.ToString();
        }

        private static List<(InteractableTrigger Trigger, float Distance)> NearbyInteractables(Actor player, string fragment, float range)
        {
            Vector2 feet = player.GetFeetPosition();
            List<(InteractableTrigger, float)> found = new();

            foreach (InteractableTrigger trigger in Resources.FindObjectsOfTypeAll<InteractableTrigger>())
            {
                if (trigger == null || !trigger.isActiveAndEnabled) continue;
                if (fragment.Length > 0 && fragment != "*" && !PathOf(trigger.transform).Contains(fragment, StringComparison.OrdinalIgnoreCase)) continue;

                float distance = ((Vector2)trigger.transform.position - feet).magnitude;
                if (distance <= range) found.Add((trigger, distance));
            }

            return found.OrderBy(entry => entry.Item2).ToList();
        }

        /// <summary>An object's name with its parent's and grandparent's: interactables are mostly generic children.</summary>
        private static string PathOf(Transform node)
        {
            string path = node.name;
            for (int up = 0; up < 2 && node.parent != null; up++)
            {
                node = node.parent;
                path = node.name + "/" + path;
            }

            return path;
        }

        /// <summary>
        /// <c>talk &lt;guid&gt; [tree]</c>: starts that NPC's dialogue with this player, as talking to them does - or,
        /// with a tree name, that dialogue tree directly, skipping the conversation that would lead to it.
        /// </summary>
        private static string Talk(string guid, string? treeName)
        {
            if (!PlayerAccess.TryGetPlayer(out Actor? player)) throw new InvalidOperationException("no player yet");

            // A guid or a name, like the other actor commands: a creature placed in the scene without a lazy handle
            // (an Alraune) has no guid a scenario can know in advance.
            Actor npc = Entities().GetActorFromGuid(guid) ?? ResolveActor(guid);
            if (!npc.IsInitialized) throw new InvalidOperationException("still initializing: " + guid);

            DialogueTree? tree = null;
            if (treeName != null)
            {
                foreach (DialogueTree candidate in Resources.FindObjectsOfTypeAll<DialogueTree>())
                {
                    if (candidate != null && candidate.name == treeName)
                    {
                        tree = candidate;
                        break;
                    }
                }

                if (tree == null) throw new InvalidOperationException("no loaded dialogue tree " + treeName);
            }

            _line = null;
            _choices = null;

            return npc.DialogueModule.StartDialogue(player, tree) ? "talking" : throw new InvalidOperationException("the game refused the dialogue");
        }

        /// <summary>
        /// <c>scenes [fragment]</c>: the world-started dialogue observers in the loaded scenes, as
        /// "object:dialogue:owner:started(0/1):x,y;" - where they stand is where their owner is, if loaded.
        /// </summary>
        private static string Scenes(string fragment, bool rearm = false)
        {
            StringBuilder text = new();

            foreach (GlobalObserverBhvr_StartDialogue observer in Resources.FindObjectsOfTypeAll<GlobalObserverBhvr_StartDialogue>())
            {
                if (observer == null || !observer.gameObject.scene.IsValid()) continue;

                string name = observer.gameObject.name;
                string dialogue = observer._dialogue != null ? observer._dialogue.name : "-";
                if (fragment.Length > 0 && !name.Contains(fragment, StringComparison.OrdinalIgnoreCase)
                    && !dialogue.Contains(fragment, StringComparison.OrdinalIgnoreCase)) continue;

                EntityInfo? owner = observer._dialogueOwner;
                Actor? body = owner?.GetActorFromInfo();
                LazyActor? lazy = body == null ? owner?.GetLazyActorFromInfo() : null;
                Vector2 at = body != null ? body.GetFeetPosition() : lazy != null ? lazy.GetFeetPosition() : observer.transform.position;

                // Each participant as name=loaded|lazy|none - a scene that will not start usually names one that is none.
                List<string> participants = new();
                if (observer._participants != null)
                {
                    foreach (EntityInfo? info in observer._participants)
                    {
                        if (info == null) { participants.Add("null"); continue; }
                        participants.Add(info.name + "=" + (info.GetActorFromInfo() != null ? "loaded" : info.GetLazyActorFromInfo() != null ? "lazy" : "none"));
                    }
                }

                string ownerState = body == null ? "unloaded" : $"init={(body.IsInitialized ? 1 : 0)},dm={(body.DialogueModule != null ? 1 : 0)},dead={(body.GetHealth() != null && body.GetHealth().IsDead ? 1 : 0)}";
                int teleports = observer._teleportActors ? 1 : 0;

                text.Append($"{name}:{dialogue}:{(owner != null ? owner.name : "-")}:{(observer._startState ? 1 : 0)}:{Format(at.x)},{Format(at.y)}" +
                    $":[{string.Join(",", participants)}] force={(observer._forceLoadActor ? 1 : 0)}{(observer._forceDialogue ? 1 : 0)} owner={ownerState} teleports={teleports};");

                // "scenes <frag> rearm": lets an observer that already fired (or threw) start again.
                if (rearm) observer._startState = false;
            }

            return text.Length == 0 ? "none" : text.ToString();
        }

        /// <summary><c>time</c>: the game clock, as total game seconds and the hour of the day.</summary>
        private static string GameTime()
        {
            DaytimeGameHandler handler = DaytimeGameHandler.TryGet()
                ?? throw new InvalidOperationException("no clock yet");

            double seconds = handler.GetTime().GetTotalSeconds();
            return $"{seconds.ToString("0", CultureInfo.InvariantCulture)} hour={((seconds / 3600d) % 24d).ToString("0.0", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// <c>dialogue</c>: whether this player is in a dialogue, the last line shown, and the answers offered
        /// right now as <c>index:text</c> joined by " | ".
        /// </summary>
        private static string Dialogue()
        {
            bool talking = PlayerAccess.TryGetPlayer(out Actor? player) && player.DialogueModule != null && player.DialogueModule.IsInDialogue();

            string said = talking ? _line?.statement?.text ?? "-" : "-";
            string offered = string.Join(" | ", Offered().Select(option => option.Index + ":" + option.Text));

            return $"in={(talking ? 1 : 0)} said={said.Replace(' ', '_')} choices={(offered.Length == 0 ? "-" : offered)} " +
                $"node={NodeName()} line={(_line == null ? "-" : IsCurrent(_lineNode) ? "current" : "stale")}";
        }

        /// <summary>
        /// <c>choose &lt;index | text fragment&gt;</c>: picks an offered answer through the window's own path, so
        /// the dialogue goes on exactly as a click would take it. <c>choose ?text</c> picks it only if it is offered,
        /// for a scene that sometimes skips a question.
        /// </summary>
        private static string Choose(string pick)
        {
            bool optional = pick.StartsWith('?');
            if (optional) pick = pick.Substring(1);

            if (_choices is not { } choices || !IsCurrent(_choicesNode))
            {
                return optional ? "not offered" : throw new InvalidOperationException("no answers offered");
            }

            List<(int Index, int Value, string Text)> offered = Offered();

            (int Index, int Value, string Text) chosen = int.TryParse(pick, out int index)
                ? offered.FirstOrDefault(option => option.Index == index)
                : offered.FirstOrDefault(option => option.Text.Contains(pick, StringComparison.OrdinalIgnoreCase));

            if (chosen.Text == null)
            {
                return optional ? "not offered" : throw new InvalidOperationException($"no answer '{pick}' among: " + string.Join(" | ", offered.Select(o => o.Text)));
            }

            _choices = null;
            _line = null;
            choices.Window.Finalize(choices.Info, chosen.Value);
            return chosen.Text;
        }

        /// <summary>
        /// <c>bye</c>: ends this player's dialogue where it stands, the way the game itself stops one. For a
        /// teammate's leftover copy of a scene the other player already played through.
        /// </summary>
        private static string Bye()
        {
            _skipping = false;
            _line = null;
            _choices = null;

            // The player's conversation through its controller, the way the game ends one: that also resets the
            // window and raises GraphFinishedEvent, which is what clears "the player is in a dialogue". Stopping
            // only the graph left the player stuck in one - the dialogue system kept the camera, and camera-loaded
            // volumes near the player (the mine exit) never switched on.
            string name;
            if (Il2CppDrova.DialogueNew.DialogueSystemGameHandler.TryGet(out Il2CppDrova.DialogueNew.DialogueSystemGameHandler handler)
                && handler != null && handler.ActivePlayerDialogue != null && handler.ActivePlayerDialogue.isRunning)
            {
                name = handler.ActivePlayerDialogue.graph?.name ?? "-";
                handler.ActivePlayerDialogue.StopDialogue();
            }
            else if (DialogueTree.currentDialogue is { } running)
            {
                name = running.name;
                running.Stop();
            }
            else
            {
                return "not in a dialogue";
            }

            if (CameraGameHandler.TryGet(out CameraGameHandler camera) && camera != null) camera.ResetTemporaryCamTarget();

            return "stopped " + name;
        }

        /// <summary><c>next</c>: moves past the spoken line on screen, as the continue key does.</summary>
        private static string Next()
        {
            SubtitlesRequestInfo line = _line ?? throw new InvalidOperationException("no line on screen");

            _pressSkip = true;
            return line.statement?.text ?? "-";
        }

        /// <summary>The skip input, pressed once per request - what a click on the dialogue does.</summary>
        private static bool SkipInputPrefix(ref bool __result)
        {
            if (!_pressSkip) return true;

            _pressSkip = false;
            if (_choices != null && IsCurrent(_choicesNode)) return true;

            __result = true;
            return false;
        }

        private static void CameraLeadPostfix(SmoothCameraTarget __instance)
        {
            Transform parent = __instance._parent;
            if (parent == null) return;

            Vector3 target = __instance.transform.position;
            __instance.transform.position = new Vector3(parent.position.x, parent.position.y, target.z);
        }

        private static IntPtr CurrentNode()
        {
            return PlayerTree()?.currentNode?.Pointer ?? IntPtr.Zero;
        }

        /// <summary>
        /// The player's own conversation. DialogueTree.currentDialogue is whichever tree ran last - often a world
        /// dialogue nearby (preaching, barks) - so a question the player was asked was filed under that tree's node,
        /// never counted as current, and its answers were hidden: the Nemeton oath "hung" on its first question.
        /// Followed down into the running sub-dialogue, so a stuck dialogue names the node it really waits on.
        /// </summary>
        private static DialogueTree? PlayerTree()
        {
            DialogueTree? tree = DialogueTree.currentDialogue;
            if (Il2CppDrova.DialogueNew.DialogueSystemGameHandler.TryGet(out Il2CppDrova.DialogueNew.DialogueSystemGameHandler handler)
                && handler != null && handler.ActivePlayerDialogue != null && handler.ActivePlayerDialogue.isRunning)
            {
                tree = handler.ActivePlayerDialogue.graph?.TryCast<DialogueTree>() ?? tree;
            }

            for (int depth = 0; depth < 8 && tree?.currentNode?.TryCast<SubDialogueTree>()?.currentInstance is { isRunning: true } inner; depth++)
            {
                tree = inner;
            }

            return tree;
        }

        /// <summary>Who an actor's AI is after, as <c>name@x,y</c>, or <c>-</c>.</summary>
        private static string Target(Actor actor)
        {
            try
            {
                Il2CppDrova.AI.Logic.IAIActor? target = actor.AIModule?.TargetService?.GetCurrentTargetActor();
                if (target == null) return "-";
                Vector2 at = target.GetPosition();
                string name = target.GetObject()?.TryCast<UnityEngine.Object>()?.name ?? "?";
                return name.Replace(' ', '_') + "@" + Format(at.x) + "," + Format(at.y);
            }
            catch
            {
                return "?";
            }
        }

        /// <summary>The current node as <c>Tree#ID:Type</c> - where a dialogue that stopped moving stands.</summary>
        private static string NodeName()
        {
            DialogueTree? tree = PlayerTree();
            var node = tree?.currentNode;
            return node == null ? (tree == null ? "none" : tree.name + "#-") : $"{tree.name}#{node.ID}:{node.GetIl2CppType().Name}";
        }

        /// <summary>Whether the dialogue still stands on the node that made a request.</summary>
        private static bool IsCurrent(IntPtr node)
        {
            return node != IntPtr.Zero && CurrentNode() == node;
        }

        private static void TickSkip()
        {
            if (!_skipping) return;

            if (_choices != null && IsCurrent(_choicesNode))
            {
                // For a talk whose answers are all the same to the story, played on whichever machine got it.
                if (_skipAnswering)
                {
                    Choose("0");
                    return;
                }

                _skipping = false;
                return;
            }

            // Over only after a moment out of dialogue: a cutscene inside a conversation (the mine's morning call)
            // briefly leaves the player "not talking", and stopping there left the next line waiting forever. The
            // conversation itself keeps running through such a cutscene, however long (Bruna's and Hasso's), so it
            // counts as talking too.
            bool talking = PlayerAccess.TryGetPlayer(out Actor? player) && player.DialogueModule != null && player.DialogueModule.IsInDialogue();
            bool conversing = Il2CppDrova.DialogueNew.DialogueSystemGameHandler.TryGet(out Il2CppDrova.DialogueNew.DialogueSystemGameHandler dialogues)
                && dialogues != null && dialogues.ActivePlayerDialogue != null && dialogues.ActivePlayerDialogue.isRunning;
            _skipIdle = talking || conversing ? 0f : _skipIdle + Time.unscaledDeltaTime;

            // Answering stops with the conversation it was asked for: still on, it took the first answer of the next
            // dialogue the player opened (a relic pedestal) before anybody looked at it.
            if (!talking && !conversing) _skipAnswering = false;
            if (_skipIdle > 10f)
            {
                _skipping = false;
                return;
            }

            // A line outlives its conversation in this field. Continued after the conversation ended, the game resets
            // that graph ("cannot continue ... Resetting Graph") - which left a later conversation hanging.
            // Also while a cutscene inside the conversation holds the player: its lines wait on the cutscene, and one
            // continued from here stepped the cutscene's own line away from it - the conversation then hung for good.
            if (!talking)
            {
                _line = null;
                return;
            }

            // Press while a line is up; the window moves it on itself - once - and asks again for the next.
            if (_line is not { } line) return;

            _pressSkip = true;

            // Some lines are not reached by the window's input at all - one from another tree than the one on
            // screen (the mine's morning call), or a cutscene-driven wake-up. Left standing well past any
            // auto-advance, the line is continued directly: a line of the current node that is still current has
            // not been moved on by anything else, so this cannot be a second step.
            float age = Time.unscaledTime - _lineSince;
            if (age > 4f && age < 30f)
            {
                _line = null;
                MelonLogger.Msg($"[TestAgent] line continued directly after {age:0.0}s: {line.statement?.text}");
                line.Continue?.Invoke();
            }
        }

        /// <summary>The answers on offer, in display order: position, the value the node expects, the text.</summary>
        private static List<(int Index, int Value, string Text)> Offered()
        {
            List<(int Index, int Value, string Text)> offered = new();
            if (_choices is not { } choices || choices.Info.options == null || !IsCurrent(_choicesNode)) return offered;

            int position = 0;
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<IStatement, int> option in choices.Info.options)
            {
                offered.Add((position++, option.Value, option.Key?.text ?? string.Empty));
            }

            return offered;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        private static string Arg(string[] parts, int index)
        {
            return index < parts.Length ? parts[index] : throw new InvalidOperationException($"argument {index} missing");
        }

        private static int Int(string[] parts, int index) => int.Parse(Arg(parts, index), CultureInfo.InvariantCulture);

        private static float Float(string[] parts, int index) => Parse(Arg(parts, index));

        private static float Parse(string text) => float.Parse(text, CultureInfo.InvariantCulture);

        private static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
