namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// A direct host on the local network that answered <see cref="Access.NetworkAccess.StartDiscovery"/>.
    /// </summary>
    /// <param name="Address">The host's IPv4 address, ready for <see cref="Access.NetworkAccess.Connect"/>.</param>
    /// <param name="Port">The host's UDP port.</param>
    /// <param name="Payload">What the host's <see cref="Access.NetworkAccess.SetDiscoveryInfo"/> provider returned. Untrusted input.</param>
    /// <param name="LastSeenUtc">When the host last answered.</param>
    public readonly record struct DiscoveredHost(string Address, int Port, byte[] Payload, DateTime LastSeenUtc);
}
