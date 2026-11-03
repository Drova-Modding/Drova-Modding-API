using System.Runtime.InteropServices;
using Drova_Modding_API.Access;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Public Steam lobbies as a listing only: the host sits in its lobby alone, and players found there join its Steam
    /// session. A lobby lives as long as the hosting session.
    /// </summary>
    internal static unsafe class SteamLobbies
    {
        private const int LobbyTypePublic = 2;
        private const int ComparisonEqual = 0;
        private const int DistanceWorldwide = 3;
        private const int ResultOk = 1;

        /// <summary>Joiners never enter the lobby, but Steam leaves a full lobby out of searches.</summary>
        private const int MaxMembers = 16;

        private const string MarkerKey = "drova_api";
        private const string HostKey = "drova_api_host";
        private const int MaxKeyBytes = 256;
        private const int MaxValueBytes = 8192;

        private static readonly Dictionary<string, string> Data = new();
        private static readonly List<SteamLobby> Found = new();

        private static ulong _lobby;
        private static ulong _creating;
        private static bool _searching;

        /// <summary>Whether this host is listed, which also lets players who are not friends in.</summary>
        internal static bool IsOpen => _lobby != 0;

        internal static void Open()
        {
            if (_lobby != 0 || _creating != 0) return;

            ulong call = SteamNative.SteamAPI_ISteamMatchmaking_CreateLobby(SteamNative.Matchmaking, LobbyTypePublic, MaxMembers);
            if (call == 0)
            {
                NetworkEvents.RaiseNetworkError("Steam refused to open a lobby");
                return;
            }

            _creating = call;
            SteamCallResult.Await(call, (result, failed) => OnCreated(call, result, failed));
        }

        internal static void Close()
        {
            _creating = 0;
            Data.Clear();

            if (_lobby == 0) return;

            SteamNative.SteamAPI_ISteamMatchmaking_LeaveLobby(SteamNative.Matchmaking, _lobby);
            _lobby = 0;
            MelonLogger.Msg("[Steam] the lobby is closed.");
        }

        internal static void SetData(string key, string value)
        {
            Data[key] = value;
            if (_lobby != 0) SteamNative.SteamAPI_ISteamMatchmaking_SetLobbyData(SteamNative.Matchmaking, _lobby, key, value);
        }

        internal static void Find((string Key, string Value)[] filters)
        {
            if (_searching) return;

            foreach ((string key, string value) in filters)
            {
                SteamNative.SteamAPI_ISteamMatchmaking_AddRequestLobbyListStringFilter(SteamNative.Matchmaking, key, value, ComparisonEqual);
            }

            SteamNative.SteamAPI_ISteamMatchmaking_AddRequestLobbyListStringFilter(SteamNative.Matchmaking, MarkerKey, "1", ComparisonEqual);
            SteamNative.SteamAPI_ISteamMatchmaking_AddRequestLobbyListDistanceFilter(SteamNative.Matchmaking, DistanceWorldwide);

            ulong call = SteamNative.SteamAPI_ISteamMatchmaking_RequestLobbyList(SteamNative.Matchmaking);
            if (call == 0)
            {
                NetworkEvents.RaiseNetworkError("Steam refused to search for lobbies");
                return;
            }

            _searching = true;
            SteamCallResult.Await(call, OnListed);
        }

        internal static void CopyFound(List<SteamLobby> buffer)
        {
            buffer.AddRange(Found);
        }

        private static void OnCreated(ulong call, IntPtr result, bool failed)
        {
            // LobbyCreated_t: EResult, then the lobby id at 8.
            int code = *(int*)result;
            ulong lobby = *(ulong*)(result + 8);
            bool current = call == _creating;
            if (current) _creating = 0;

            if (failed || code != ResultOk || lobby == 0)
            {
                if (current) NetworkEvents.RaiseNetworkError($"Steam could not open a lobby (EResult {code})");
                return;
            }

            // The session ended, or another began, while Steam was still creating this one.
            if (!current)
            {
                SteamNative.SteamAPI_ISteamMatchmaking_LeaveLobby(SteamNative.Matchmaking, lobby);
                return;
            }

            _lobby = lobby;
            SteamNative.SteamAPI_ISteamMatchmaking_SetLobbyData(SteamNative.Matchmaking, lobby, MarkerKey, "1");
            SteamNative.SteamAPI_ISteamMatchmaking_SetLobbyData(SteamNative.Matchmaking, lobby, HostKey, SteamNative.LocalId.ToString());
            foreach (KeyValuePair<string, string> entry in Data)
            {
                SteamNative.SteamAPI_ISteamMatchmaking_SetLobbyData(SteamNative.Matchmaking, lobby, entry.Key, entry.Value);
            }

            MelonLogger.Msg("[Steam] the lobby is open; players who are not friends can find and join this session.");
        }

        private static void OnListed(IntPtr result, bool failed)
        {
            _searching = false;
            Found.Clear();

            if (!failed)
            {
                // LobbyMatchList_t: the number of lobbies found.
                int count = (int)*(uint*)result;
                ulong self = SteamNative.LocalId;

                byte* key = stackalloc byte[MaxKeyBytes];
                byte* value = stackalloc byte[MaxValueBytes];

                for (int index = 0; index < count; index++)
                {
                    ulong lobby = SteamNative.SteamAPI_ISteamMatchmaking_GetLobbyByIndex(SteamNative.Matchmaking, index);
                    Dictionary<string, string> data = ReadData(lobby, key, value);

                    if (!data.TryGetValue(HostKey, out string? hostText) || !ulong.TryParse(hostText, out ulong host) || host == self) continue;

                    Found.Add(new SteamLobby(host, data));
                }
            }

            SteamAccess.RaiseLobbiesFound();
        }

        private static Dictionary<string, string> ReadData(ulong lobby, byte* key, byte* value)
        {
            int count = SteamNative.SteamAPI_ISteamMatchmaking_GetLobbyDataCount(SteamNative.Matchmaking, lobby);
            Dictionary<string, string> data = new(count);

            for (int index = 0; index < count; index++)
            {
                if (!SteamNative.SteamAPI_ISteamMatchmaking_GetLobbyDataByIndex(SteamNative.Matchmaking, lobby, index, key, MaxKeyBytes, value, MaxValueBytes)) continue;

                string? name = Marshal.PtrToStringUTF8((IntPtr)key);
                if (!string.IsNullOrEmpty(name)) data[name] = Marshal.PtrToStringUTF8((IntPtr)value) ?? string.Empty;
            }

            return data;
        }
    }
}
