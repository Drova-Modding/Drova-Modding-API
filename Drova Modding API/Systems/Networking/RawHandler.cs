namespace Drova_Modding_API.Systems.Networking
{
    /// <summary>
    /// Handles a raw packet on a channel registered with
    /// <see cref="Access.NetworkAccess.RegisterChannel"/>. The span points into the transport's
    /// pooled receive buffer and is only valid for the duration of the call, so copy out anything
    /// that must outlive it. Runs on the Unity main thread.
    /// </summary>
    /// <param name="peer">The peer the packet came from.</param>
    /// <param name="data">The raw payload, valid only during this call.</param>
    public delegate void RawHandler(INetPeer peer, ReadOnlySpan<byte> data);
}
