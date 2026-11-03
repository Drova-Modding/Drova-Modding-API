using System.Text;
using Drova_Modding_API.Systems.Networking;
using Drova_Modding_API.Systems.Networking.Impl;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Steam friends join and invite support for multiplayer mods. Sessions start with <see cref="NetworkAccess.StartHostViaSteam"/>.
    /// When the game is not running, Steam passes the join string on the command line instead of raising
    /// <see cref="OnJoinRequested"/>. Everything here is a no-op without Steam (GOG).
    /// </summary>
    public static class SteamAccess
    {
        private const string ConnectKey = "connect";
        private const string ReservedKeyPrefix = "drova_api";

        /// <summary>k_nMaxLobbyKeyLength, less the terminator.</summary>
        private const int MaxLobbyKeyBytes = 255;

        /// <summary>k_cubChatMetadataMax, less the terminator.</summary>
        private const int MaxLobbyValueBytes = 8191;

        private static Action<string>? _joinRequested;
        private static bool _listening;

        /// <summary>
        /// A <see cref="FindLobbies"/> search finished. Read the result with <see cref="GetFoundLobbies"/>. Raised on the main thread.
        /// </summary>
        public static event Action? OnLobbiesFound;

        /// <summary>Whether Steam is running for this copy of the game.</summary>
        public static bool IsAvailable => SteamNative.Available;

        /// <summary>This player's 64-bit Steam id, or 0 without Steam.</summary>
        public static ulong LocalId => SteamNative.LocalId;

        /// <summary>
        /// Lets Steam sessions try a direct path before the relay. Both ends need it, and it is read when a session starts.
        /// Off by default because finding the path freezes the game for about 2 s per new player and exposes IP addresses.
        /// </summary>
        public static bool AllowDirectConnections { get; set; }

        /// <summary>
        /// A friend chose "Join Game" or accepted an invite while the game runs. Carries the join string. Raised on the main thread.
        /// </summary>
        public static event Action<string>? OnJoinRequested
        {
            add
            {
                _joinRequested += value;
                Listen();
            }
            remove => _joinRequested -= value;
        }

        /// <summary>
        /// Shows "Join Game" to Steam friends, handing them <paramref name="joinString"/>. Null or empty takes it away.
        /// </summary>
        public static void SetJoinString(string? joinString)
        {
            if (!IsAvailable) return;

            SteamNative.SteamAPI_ISteamFriends_SetRichPresence(SteamNative.Friends, ConnectKey, joinString ?? string.Empty);
        }

        /// <summary>Opens the Steam overlay's invite dialog. An invited friend receives <paramref name="joinString"/>.</summary>
        /// <returns>False without Steam. The overlay may still be switched off in the player's Steam settings.</returns>
        public static bool OpenInviteDialog(string joinString)
        {
            if (!IsAvailable) return false;

            SteamNative.SteamAPI_ISteamFriends_ActivateGameOverlayInviteDialogConnectString(SteamNative.Friends, joinString);
            return true;
        }

        /// <summary>
        /// Appends the friends who show a join string. Steam shares it only while they play the same game.
        /// </summary>
        public static void GetJoinableFriends(List<(ulong Id, string Name, string JoinString)> buffer)
        {
            if (!IsAvailable) return;

            int count = SteamNative.SteamAPI_ISteamFriends_GetFriendCount(SteamNative.Friends, SteamNative.FriendFlagImmediate);

            for (int index = 0; index < count; index++)
            {
                ulong id = SteamNative.SteamAPI_ISteamFriends_GetFriendByIndex(SteamNative.Friends, index, SteamNative.FriendFlagImmediate);

                string? joinString = SteamNative.ReadString(SteamNative.SteamAPI_ISteamFriends_GetFriendRichPresence(SteamNative.Friends, id, ConnectKey));
                if (string.IsNullOrEmpty(joinString)) continue;

                string name = SteamNative.ReadString(SteamNative.SteamAPI_ISteamFriends_GetFriendPersonaName(SteamNative.Friends, id)) ?? id.ToString();
                buffer.Add((id, name, joinString));
            }
        }

        /// <summary>
        /// Shows Steam's on-screen keyboard over a text field, for a Steam Deck or Big Picture player without a keyboard.
        /// It types into the focused field like a real keyboard. Call it when the field gets focus.
        /// </summary>
        /// <param name="x">The field's left edge, in pixels from the game window's left.</param>
        /// <param name="y">The field's top edge, in pixels from the game window's top.</param>
        /// <param name="width">The field's width in pixels.</param>
        /// <param name="height">The field's height in pixels.</param>
        /// <returns>False without Steam, and outside Big Picture and the Steam Deck.</returns>
        public static bool ShowKeyboard(int x, int y, int width, int height)
        {
            return IsAvailable && SteamNative.SteamAPI_ISteamUtils_ShowFloatingGamepadTextInput(SteamNative.ClientUtils, 0, x, y, width, height);
        }

        /// <summary>
        /// Lists the running <see cref="NetworkAccess.StartHostViaSteam"/> session publicly, so <see cref="FindLobbies"/> finds it.
        /// While it is open, players who are not Steam friends may join too. It closes with the session.
        /// <see cref="NetworkEvents.OnNetworkError"/> fires when Steam cannot open it.
        /// </summary>
        public static void OpenLobby()
        {
            if (!IsAvailable || NetworkSystem.Role != NetRole.Host || NetworkSystem.Transport?.Mode != TransportMode.Steam)
            {
                NetworkEvents.RaiseNetworkError("a lobby needs a session hosted over Steam");
                return;
            }

            SteamLobbies.Open();
        }

        /// <summary>Takes the listing down. Players already in the session stay. Does nothing when no lobby is open.</summary>
        public static void CloseLobby()
        {
            SteamLobbies.Close();
        }

        /// <summary>Whether this host is listed. False while Steam is still opening the lobby.</summary>
        public static bool IsLobbyOpen => SteamLobbies.IsOpen;

        /// <summary>
        /// Sets what searchers see about this host, such as a name, a mode or a player count, and what
        /// <see cref="FindLobbies"/> can filter on. Kept until the session stops, so it may be set before the lobby opened.
        /// Set it after <see cref="NetworkAccess.StartHostViaSteam"/>, which starts with none.
        /// </summary>
        /// <param name="key">Up to 255 bytes. Keys starting with <c>drova_api</c> are reserved.</param>
        /// <param name="value">Up to 8191 bytes.</param>
        public static void SetLobbyData(string key, string value)
        {
            if (key.StartsWith(ReservedKeyPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException($"lobby keys starting with {ReservedKeyPrefix} are reserved", nameof(key));
            }

            if (Encoding.UTF8.GetByteCount(key) > MaxLobbyKeyBytes || Encoding.UTF8.GetByteCount(value) > MaxLobbyValueBytes)
            {
                throw new ArgumentException("a lobby key holds 255 bytes and a value 8191");
            }

            SteamLobbies.SetData(key, value);
        }

        /// <summary>
        /// Searches Steam worldwide for open lobbies of this API, skipping this player's own.
        /// <see cref="OnLobbiesFound"/> fires when done. Ignored while a search runs.
        /// </summary>
        /// <param name="filters">Lobby data a host must have set to the same value, for example a protocol version.</param>
        public static void FindLobbies(params (string Key, string Value)[] filters)
        {
            if (IsAvailable) SteamLobbies.Find(filters);
        }

        /// <summary>Fills a buffer with the last search's lobbies. The buffer is cleared first.</summary>
        public static void GetFoundLobbies(List<SteamLobby> buffer)
        {
            buffer.Clear();
            SteamLobbies.CopyFound(buffer);
        }

        internal static void RaiseJoinRequested(string joinString)
        {
            _joinRequested?.Invoke(joinString);
        }

        internal static void RaiseLobbiesFound()
        {
            try
            {
                OnLobbiesFound?.Invoke();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Steam] OnLobbiesFound failed: " + e);
            }
        }

        private static void Listen()
        {
            if (_listening || !IsAvailable) return;

            try
            {
                SteamJoinCallback.Install();
                _listening = true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Steam] could not listen for join requests; friends can still join from the main menu: " + e);
            }
        }
    }
}
