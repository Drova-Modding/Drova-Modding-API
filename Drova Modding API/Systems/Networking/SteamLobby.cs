namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// A public Steam host found by <see cref="Access.SteamAccess.FindLobbies"/>.
    /// </summary>
    /// <param name="HostId">The host's Steam id, ready for <see cref="Access.NetworkAccess.ConnectViaSteam"/>.</param>
    /// <param name="Data">What the host set with <see cref="Access.SteamAccess.SetLobbyData"/>. Untrusted input.</param>
    public readonly record struct SteamLobby(ulong HostId, IReadOnlyDictionary<string, string> Data);
}
