using System.Runtime.InteropServices;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Steamworks flat-API calls into the game's own, already initialized <c>steam_api64.dll</c>. The IL2CPP
    /// Steamworks.NET wrappers would marshal every packet through interop arrays.
    /// </summary>
    internal static unsafe class SteamNative
    {
        private const string Lib = "steam_api64";

        /// <summary><c>k_nSteamNetworkingSend_UnreliableNoNagle</c>.</summary>
        internal const int SendUnreliable = 1;

        /// <summary><c>k_nSteamNetworkingSend_ReliableNoNagle</c>.</summary>
        internal const int SendReliable = 9;

        /// <summary><c>k_EFriendRelationshipFriend</c>.</summary>
        private const int RelationshipFriend = 3;

        /// <summary><c>k_EFriendFlagImmediate</c>: regular friends.</summary>
        internal const int FriendFlagImmediate = 4;

        /// <summary>Size of <c>SteamNetworkingIdentity</c>.</summary>
        internal const int IdentitySize = 136;

        /// <summary>Where <c>m_identityPeer</c> sits in <c>SteamNetworkingMessage_t</c>.</summary>
        internal const int MessagePeerOffset = 16;

        private static bool? _available;

        /// <summary>
        /// .NET's probing never looks in Drova_Data\Plugins, and may find another program's steam_api64 on PATH, a second
        /// copy Steam was never initialized in. Loading the game's own path returns the module the game already loaded.
        /// </summary>
        static SteamNative()
        {
            NativeLibrary.SetDllImportResolver(typeof(SteamNative).Assembly, (name, _, _) =>
                name == Lib ? NativeLibrary.Load(Path.Combine(UnityEngine.Application.dataPath, "Plugins", "x86_64", Lib + ".dll")) : IntPtr.Zero);
        }

        internal static IntPtr Messages { get; private set; }
        internal static IntPtr Sockets { get; private set; }
        internal static IntPtr Utils { get; private set; }
        internal static IntPtr Friends { get; private set; }
        internal static IntPtr User { get; private set; }
        internal static IntPtr Matchmaking { get; private set; }

        /// <summary><c>ISteamUtils</c>, as opposed to the networking utils in <see cref="Utils"/>.</summary>
        internal static IntPtr ClientUtils { get; private set; }

        /// <summary>
        /// False on a GOG copy, which ships no Steam library. A false before the game initialized Steam is asked again.
        /// The first true, or a missing library, is cached.
        /// </summary>
        internal static bool Available
        {
            get
            {
                if (_available.HasValue) return _available.Value;

                try
                {
                    if (!SteamAPI_IsSteamRunning() || SteamAPI_GetHSteamUser() == 0) return false;

                    Messages = SteamAPI_SteamNetworkingMessages_SteamAPI_v002();
                    Sockets = SteamAPI_SteamNetworkingSockets_SteamAPI_v012();
                    Utils = SteamAPI_SteamNetworkingUtils_SteamAPI_v004();
                    Friends = SteamAPI_SteamFriends_v017();
                    User = SteamAPI_SteamUser_v023();
                    Matchmaking = SteamAPI_SteamMatchmaking_v009();
                    ClientUtils = SteamAPI_SteamUtils_v010();

                    _available = Messages != IntPtr.Zero && Sockets != IntPtr.Zero && Utils != IntPtr.Zero
                        && Friends != IntPtr.Zero && User != IntPtr.Zero && Matchmaking != IntPtr.Zero && ClientUtils != IntPtr.Zero;
                }
                catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
                {
                    MelonLogger.Msg("[Networking] Steam is not available, so Steam sessions are off: " + e.Message);
                    _available = false;
                }

                return _available.Value;
            }
        }

        internal static ulong LocalId => Available ? SteamAPI_ISteamUser_GetSteamID(User) : 0;

        internal static bool IsFriend(ulong id) => SteamAPI_ISteamFriends_GetFriendRelationship(Friends, id) == RelationshipFriend;

        /// <summary>Fills a <c>SteamNetworkingIdentity</c> for a Steam id into a buffer of <see cref="IdentitySize"/>.</summary>
        internal static void SetIdentity(byte* identity, ulong id)
        {
            new Span<byte>(identity, IdentitySize).Clear();
            SteamAPI_SteamNetworkingIdentity_SetSteamID64(identity, id);
        }

        internal static ulong IdOf(byte* identity) => SteamAPI_SteamNetworkingIdentity_GetSteamID64(identity);

        internal static string? ReadString(IntPtr utf8) => utf8 == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(utf8);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamAPI_IsSteamRunning();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SteamAPI_GetHSteamUser();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_SteamNetworkingMessages_SteamAPI_v002();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_SteamNetworkingSockets_SteamAPI_v012();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_SteamNetworkingUtils_SteamAPI_v004();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_SteamFriends_v017();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_SteamUser_v023();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong SteamAPI_ISteamUser_GetSteamID(IntPtr self);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SteamAPI_SteamNetworkingIdentity_SetSteamID64(byte* identity, ulong id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong SteamAPI_SteamNetworkingIdentity_GetSteamID64(byte* identity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SteamAPI_ISteamNetworkingMessages_SendMessageToUser(IntPtr self, byte* identity, byte* data, uint size, int flags, int channel);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SteamAPI_ISteamNetworkingMessages_ReceiveMessagesOnChannel(IntPtr self, int channel, IntPtr* messages, int max);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamNetworkingMessages_AcceptSessionWithUser(IntPtr self, byte* identity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamNetworkingMessages_CloseSessionWithUser(IntPtr self, byte* identity);

        /// <summary>Returns the connection state. <paramref name="status"/> is a <c>SteamNetConnectionRealTimeStatus_t</c>.</summary>
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SteamAPI_ISteamNetworkingMessages_GetSessionConnectionInfo(IntPtr self, byte* identity, IntPtr info, byte* status);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_SteamNetworkingMessage_t_Release(IntPtr message);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_ISteamNetworkingSockets_RunCallbacks(IntPtr self);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamNetworkingUtils_SetGlobalCallback_MessagesSessionRequest(IntPtr self, IntPtr callback);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamNetworkingUtils_SetGlobalCallback_MessagesSessionFailed(IntPtr self, IntPtr callback);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_ISteamNetworkingUtils_InitRelayNetworkAccess(IntPtr self);

        /// <summary>Config value 25, <c>k_ESteamNetworkingConfig_TimeoutConnected</c>, in milliseconds.</summary>
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamNetworkingUtils_SetGlobalConfigValueInt32(IntPtr self, int setting, int value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SteamAPI_ISteamFriends_GetFriendRelationship(IntPtr self, ulong id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SteamAPI_ISteamFriends_GetFriendCount(IntPtr self, int flags);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong SteamAPI_ISteamFriends_GetFriendByIndex(IntPtr self, int index, int flags);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SteamAPI_ISteamFriends_GetFriendPersonaName(IntPtr self, ulong id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SteamAPI_ISteamFriends_GetFriendRichPresence(IntPtr self, ulong id, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamFriends_SetRichPresence(IntPtr self, [MarshalAs(UnmanagedType.LPUTF8Str)] string key,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string? value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_ISteamFriends_ActivateGameOverlayInviteDialogConnectString(IntPtr self,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string connect);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_ISteamFriends_SetPlayedWith(IntPtr self, ulong id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_SteamMatchmaking_v009();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SteamAPI_SteamUtils_v010();

        /// <summary><paramref name="mode"/> 0 is a single line.</summary>
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamUtils_ShowFloatingGamepadTextInput(IntPtr self, int mode, int x, int y, int width, int height);

        /// <summary>Returns the <c>SteamAPICall_t</c> of a <c>LobbyCreated_t</c>, 0 when Steam refused at once.</summary>
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong SteamAPI_ISteamMatchmaking_CreateLobby(IntPtr self, int lobbyType, int maxMembers);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_ISteamMatchmaking_LeaveLobby(IntPtr self, ulong lobby);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamMatchmaking_SetLobbyData(IntPtr self, ulong lobby,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string key, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_ISteamMatchmaking_AddRequestLobbyListStringFilter(IntPtr self,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string key, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, int comparison);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SteamAPI_ISteamMatchmaking_AddRequestLobbyListDistanceFilter(IntPtr self, int distance);

        /// <summary>Returns the <c>SteamAPICall_t</c> of a <c>LobbyMatchList_t</c>, 0 when Steam refused at once.</summary>
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong SteamAPI_ISteamMatchmaking_RequestLobbyList(IntPtr self);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong SteamAPI_ISteamMatchmaking_GetLobbyByIndex(IntPtr self, int index);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SteamAPI_ISteamMatchmaking_GetLobbyDataCount(IntPtr self, ulong lobby);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SteamAPI_ISteamMatchmaking_GetLobbyDataByIndex(IntPtr self, ulong lobby, int index,
            byte* key, int keySize, byte* value, int valueSize);
    }
}
