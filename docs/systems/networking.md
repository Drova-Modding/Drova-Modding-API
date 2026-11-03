# Networking (coop transport)

**What it does:** gives mods a small UDP transport — start or join a session, send and receive
messages, and react to connection events. It is **plumbing only**: it never syncs game state and
never draws UI. A coop mod builds lobby, UI, and world sync on top of it.

Entry points: `Drova_Modding_API.Access.NetworkAccess` and
`Drova_Modding_API.Access.NetworkEvents` (both static).

> The transport ships in every API build. It is backed by LiteNetLib, which the API installs as
> `UserLibs/LiteNetLib.dll` — see [Shipping](#shipping).

## Quick example

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Networking;

// Messages are structs so sending and receiving does not allocate or box.
public struct PingMessage : INetMessage
{
    public int Sequence;

    public void Write(INetWriter writer)
    {
        writer.Put(Sequence);
    }

    public void Read(INetReader reader)
    {
        Sequence = reader.GetInt();
    }
}

public class MyCoopMod : MelonMod
{
    private const ushort PingId = 1;

    public override void OnInitializeMelon()
    {
        NetworkAccess.RegisterWithId<PingMessage>(PingId, (peer, message) =>
        {
            LoggerInstance.Msg($"Ping #{message.Sequence} from peer {peer.Id}");
        });

        NetworkEvents.OnPeerConnected += peer => NetworkAccess.Send(peer, new PingMessage { Sequence = 1 });
    }
}
```

Host with `NetworkAccess.StartHost(9050)`, join with `NetworkAccess.Connect("1.2.3.4", 9050)`.

A complete, buildable mod is in [`samples/EchoCoopMod`](../../samples/EchoCoopMod) — it references
`Drova_Modding_API.dll` and `MelonLoader.dll` and nothing else.

## How do I…?

### Host a session and let someone join

```csharp
// Host. The optional key is a shared secret; clients must pass the same string.
NetworkAccess.StartHost(9050, "my-secret");

// Client.
NetworkAccess.Connect("87.123.45.6", 9050, "my-secret");

// Either side.
NetworkAccess.Stop();
```

The host must have UDP `9050` reachable: forward it, let UPnP open it, or punch, see
[Getting through the router](#getting-through-the-router).

### Getting through the router

A direct host behind a home router is reachable in one of three ways, tried in this order:

1. **UPnP.** With `NetworkAccess.UsePortMapping` set (off by default, so offer it as a player
   option), `StartHost` asks the router to open its port. The port closes again on `Stop()` and when
   the game quits. The mapping is leased for an
   hour and renewed while hosting, so a crashed game leaves it open for an hour at most. Many routers
   ship with UPnP off; a FRITZ!Box needs "Selbstständige Portfreigaben" allowed for the PC.
2. **Hole punching.** Both ends learn their internet address from a public STUN server (Google,
   Cloudflare) and exchange it, for example by chat:

   ```csharp
   // Host: show this to the joiner.
   NetworkEvents.OnPublicEndPointFound += endPoint => ShowCode(endPoint.ToString());
   NetworkAccess.StartHost(9061, "secret");
   NetworkAccess.FindPublicEndPoint();

   // Joiner: enters the host's code. Its own code appears through the same event; it goes to the host.
   NetworkAccess.ConnectWithPunch("203.0.113.7", 13139, "secret");

   // Host: enters the joiner's code within two minutes.
   NetworkAccess.Punch("87.123.45.6", 51234);
   ```

   `ConnectWithPunch` keeps trying for two minutes instead of five seconds. The address is re-asked
   every 20 s, which keeps the router from forgetting it. Punching fails when a router gives every
   destination a new port (symmetric NAT, common on mobile and carrier-grade NAT); the log says so.
3. **Port forwarding** by hand, or a Steam session.

All of it is managed sockets and HTTP, so it works the same under Wine and Proton.

### Define a message

Implement `INetMessage` **on a struct** and write/read the fields in the same order. Keep fields
blittable — a `string` or `byte[]` field allocates on every receive, everything else does not.

```csharp
public struct MovePlayer : INetMessage
{
    public float X;
    public float Y;

    public void Write(INetWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
    }

    public void Read(INetReader reader)
    {
        X = reader.GetFloat();
        Y = reader.GetFloat();
    }
}
```

### Pick message ids

The `ushort` id is the wire contract: host and client must use the same id for the same type, so
declare them as constants in one place. Registration order does not matter, ids are independent of
it. Ids `0`–`255` are valid; each travels as a single byte in front of the message.

```csharp
internal static class MessageIds
{
    internal const ushort Ping = 1;
    internal const ushort MovePlayer = 2;
}
```

### Send to one peer or to everyone

```csharp
NetworkAccess.Send(peer, move, Delivery.Unreliable);
NetworkAccess.SendToAll(move, Delivery.Unreliable);
```

`SendToAll` serialises once and reuses the payload for every peer.

### React to connections

```csharp
NetworkEvents.OnHostStarted      += () => { };
NetworkEvents.OnConnectedToHost  += () => { };
NetworkEvents.OnPeerConnected    += peer => { };
NetworkEvents.OnPeerDisconnected += (peer, reason) => { };
NetworkEvents.OnNetworkError     += description => { };
```

All of them fire on the Unity main thread, and a throwing subscriber is caught and logged rather
than taking the receive loop down with it.

### Send raw bytes instead

When you bring your own serialization, use a raw channel. Channel `0` is reserved for typed messages
and `63` for transport control traffic; `1`–`62` are yours.

```csharp
NetworkAccess.RegisterChannel(1, (peer, data) =>
{
    // data is a ReadOnlySpan<byte> valid only for this call - copy what you need to keep.
});

Span<byte> payload = stackalloc byte[8];
NetworkAccess.SendRaw(peer, 1, payload);
```

## Steam sessions

A Steam session runs over Valve's relays: no port to forward, and players do not see each other's IP
address unless `SteamAccess.AllowDirectConnections` is on at both ends. Everything below does nothing
on a copy without Steam (`SteamAccess.IsAvailable`).

```csharp
// Host: friends see "Join Game" on you in Steam, or get your invite.
NetworkAccess.StartHostViaSteam();
SteamAccess.SetJoinString("my-mod:" + SteamAccess.LocalId);
SteamAccess.OpenInviteDialog("my-mod:" + SteamAccess.LocalId);

// Joiner: from the friends list, or when Steam hands over a join string.
List<(ulong Id, string Name, string JoinString)> friends = new();
SteamAccess.GetJoinableFriends(friends);
SteamAccess.OnJoinRequested += joinString => { /* parse, then */ NetworkAccess.ConnectViaSteam(hostId); };
```

### Public lobbies

A host only lets in its Steam friends, unless it opens a public lobby. The lobby is a listing: the
host sits in it alone, and whoever finds it joins the Steam session with `ConnectViaSteam`. While the
lobby is open **anyone** may join, so consider what a stranger can do in your mod.

```csharp
// Host, after StartHostViaSteam (which starts with no lobby data).
SteamAccess.OpenLobby();
SteamAccess.SetLobbyData("protocol", "7");
SteamAccess.SetLobbyData("name", hostName);

// Searcher: worldwide, only lobbies with protocol 7, never your own.
SteamAccess.OnLobbiesFound += () =>
{
    List<SteamLobby> lobbies = new();
    SteamAccess.GetFoundLobbies(lobbies);
    // lobby.Data is untrusted input. Join with NetworkAccess.ConnectViaSteam(lobby.HostId).
};
SteamAccess.FindLobbies(("protocol", "7"));
```

The lobby closes with the session. Keys starting with `drova_api` are reserved.

### Done for you

- Players in one Steam session are shown as **playing together** in the Steam friends list.
- Everyone you played with lands in Steam's **Recent players**, to befriend or invite again.

### Steam Deck keyboard

`SteamAccess.ShowKeyboard(x, y, width, height)` opens Steam's on-screen keyboard over a text field,
in Big Picture and on the Steam Deck. Call it when the field gets focus; it types into the field like a
real keyboard, and returns false elsewhere.

## Shipping

The transport is backed by LiteNetLib 2.1.4, which the build copies into the game's **`UserLibs`**
folder, where MelonLoader keeps shared managed dependencies, rather than next to the mod. Both
`dist/Modding_API.zip` and `dist/Modding_API_Dev.zip` contain `Mods/Drova_Modding_API.dll` plus
`UserLibs/LiteNetLib.dll`. Users need **both**; the mod DLL alone will fail to resolve LiteNetLib at
runtime.

A mod that uses networking references `Drova_Modding_API.dll` only — LiteNetLib never appears in a
consumer mod's references.

## Finding hosts on the local network

A direct host (`StartHost`) can answer LAN discovery, so players on the same network find it without
typing an address. The host decides what it says about itself; the API only carries the bytes.

```csharp
// Host: answer with a small payload of your own. Asked again for every request, so it can be live.
NetworkAccess.SetDiscoveryInfo(() => Encoding.UTF8.GetBytes($"{hostName};{players}/16"));
NetworkAccess.StartHost(9061, "secret");

// Client: broadcast now and every 2 s until stopped, then read what answered.
NetworkAccess.StartDiscovery(9061);
List<DiscoveredHost> found = new();
NetworkAccess.GetDiscoveredHosts(found);
foreach (DiscoveredHost host in found)
{
    // host.Payload is untrusted input from anyone on the network. Connect with host.Address and host.Port.
}
NetworkAccess.StopDiscovery();
```

- **Inert unless used.** Without `SetDiscoveryInfo` a host answers nothing, and without `StartDiscovery`
  nothing is broadcast.
- **Independent of a session.** Discovery runs on its own socket, so a client can look while idle.
- **Private networks only.** The host answers requests from private, link-local and loopback addresses,
  so a host reachable from the internet through a forwarded port does not describe itself to it.
- **IPv4 broadcast.** A host silent for three broadcasts (about 6 s) drops out of the list.
- Keep the payload well under 1 KB; it travels in one datagram.

A direct session is **not encrypted**. The connection key travels in the clear and only keeps out a
client that does not know it.

## Seeing what the transport is doing

A **`Debug` build** carries an in-game stats
window, opened from the developer console with **`api_netstats`**. It works in the main menu and in
the world, session or not. It does not take gameplay input while open, so you can keep playing and
watch the numbers move; drag it by the title bar.

| Command                | What it does                                                              |
|------------------------|---------------------------------------------------------------------------|
| `api_netstats`         | Toggle the window. `on` and `off` set it explicitly.                      |
| `api_netstats reset`   | Zero every counter, including LiteNetLib's.                               |
| `api_netstats log`     | Write the current numbers to the MelonLoader log.                         |

`reset` and `log` work whether the window is open. The same two are buttons in the window.

| Section            | What it answers                                                                                                                                |
|--------------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| **Session**        | Role, direct or Steam, peer count, and how long the counters have been running.                |
| **Traffic**        | Packets and bytes in each direction, the current rate, and how many reliable packets had to be resent.                                         |
| **Graphs**         | Round trip, outgoing rate and incoming rate over the last 30 seconds, sampled four times a second. Round trip is the **worst** peer's.         |
| **Dropped**        | Packets that never reached a handler, split by cause: NaN or infinity, unknown message id, malformed, receive failures.         |
| **Peers**          | Per peer: round trip, one-way latency, MTU, time since the last packet, bytes each way, and packets resent.                                    |
| **Typed messages** | Per message id: how many were sent and received and how many bytes each direction cost. This is where you find out which message is expensive. |

Resetting is what lets a single action be measured on its own rather than against the whole session,
and the log snapshot is what you attach to a bug report — a screenshot of a window that updates four
times a second usually is not enough.

Reading it honestly:

- **Resent packets are the only loss you can see from here.** They count reliable packets LiteNetLib
  sent again. Unreliable traffic that never arrived leaves no trace anywhere by definition.
- **A typed-message row counts a `SendToAll` once**, because it was serialized once. What the fan-out
  costs on the wire is in the byte counters under **Traffic**.
- **A steady "unknown message id" count is a version mismatch**, not a network fault: the two ends
  disagree about which id means which message.

Counting restarts whenever a session does and survives it ending, so **Dropped**, **Typed messages**
and `api_netstats log` still say something after a disconnect. **Traffic** is LiteNetLib's own tally
and lives on its manager, so it survives a peer dropping out but not an explicit `Stop()`.

Everything except the window itself is compiled into `Release` builds too — only the view is
stripped.

## API reference

### `NetworkAccess`

| Member                                                                                                            | Description                                                                                    |
|-------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|
| `NetRole Role`                                                                                                    | `None`, `Host` or `Client`.                                                                    |
| `bool IsConnected`                                                                                                | A session is running and at least one peer is connected.                                       |
| `int PeerCount`                                                                                                   | How many remote peers are connected.                                                           |
| `void StartHost(int port, string? key = null)`                                                                    | Listen on a UDP port; clients must present the same `key` when set. Not encrypted.             |
| `void Connect(string address, int port, string? key = null)`                                                      | Join a host.                                                                                   |
| `bool UsePortMapping`                                                                                             | `StartHost` opens its port through UPnP and closes it on stop and quit. Off by default.        |
| `void FindPublicEndPoint()`                                                                                       | Ask a STUN server for this direct session's internet address, then every 20 s.                 |
| `IPEndPoint? PublicEndPoint`                                                                                      | That address, once found. Null outside a direct session.                                       |
| `void ConnectWithPunch(string address, int port, string? key = null)`                                             | Join a host's public address, trying for two minutes, and find this end's address.             |
| `void Punch(string address, int port)`                                                                            | Open this end's router toward a joiner's public address for two minutes.                       |
| `void Stop()`                                                                                                     | Close the session and all connections.                                                         |
| `void SetDiscoveryInfo(Func<byte[]>? provider)`                                                                   | What a direct host answers LAN discovery with. Null (the default) answers nothing.             |
| `void StartDiscovery(int port)` / `void StopDiscovery()`                                                          | Broadcast for direct hosts on that port every 2 s, independent of any session.                 |
| `bool IsDiscovering`                                                                                              | Whether discovery is running.                                                                  |
| `void GetDiscoveredHosts(List<DiscoveredHost> buffer)`                                                            | Hosts that answered recently: `Address`, `Port`, `Payload`, `LastSeenUtc`.                     |
| `void GetPeers(List<INetPeer> buffer)`                                                                            | Clear the buffer and fill it with the current peers. Reuse one list to avoid allocating.       |
| `void RegisterWithId<T>(ushort id, Action<INetPeer, T> handler)`                                                  | Bind a message type to its wire id. `T` must be a `struct` implementing `INetMessage`.         |
| `void Unregister<T>()`                                                                                            | Drop a message type's handler and free its id.                                                 |
| `void Send<T>(INetPeer peer, in T message, Delivery delivery = Reliable)`                                         | Send a typed message to one peer.                                                              |
| `void SendToAll<T>(in T message, Delivery delivery = Reliable)`                                                   | Send a typed message to every peer, serialised once.                                           |
| `void RegisterChannel(byte channel, RawHandler handler)`                                                          | Bind a raw byte channel (1–62).                                                                |
| `void UnregisterChannel(byte channel)`                                                                            | Drop a raw channel's handler.                                                                  |
| `void SendRaw(INetPeer peer, byte channel, ReadOnlySpan<byte> data, Delivery)`                                    | Send raw bytes to one peer.                                                                    |
| `void SendRawToAll(byte channel, ReadOnlySpan<byte> data, Delivery)`                                              | Send raw bytes to every peer.                                                                  |

### `NetworkEvents`

| Event                                                    | Fires when                                                              |
|----------------------------------------------------------|-------------------------------------------------------------------------|
| `Action? OnHostStarted`                                  | The local host started listening.                                       |
| `Action? OnConnectedToHost`                              | The local client finished connecting.                                   |
| `Action<INetPeer>? OnPeerConnected`                      | A peer joined (host: per client; client: the host peer).                |
| `Action<INetPeer, DisconnectReason>? OnPeerDisconnected` | A peer left. Do not use the peer after this callback.                   |
| `Action<string>? OnNetworkError`                         | A socket-level error occurred. The string is for logs, not for parsing. |
| `Action<IPEndPoint>? OnPublicEndPointFound`              | STUN reported this end's internet address, or it changed.               |

### Supporting types

| Type                  | Members                                                                                                                                                                                                                        |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `INetPeer`            | `int Id`, `int Ping` (one-way, half the round trip), `void Disconnect()`                                                                                                                                                       |
| `INetMessage`         | `void Write(INetWriter)`, `void Read(INetReader)`                                                                                                                                                                              |
| `INetWriter`          | `Put` for `byte`/`sbyte`/`short`/`ushort`/`int`/`uint`/`long`/`ulong`/`float`/`double`/`bool`/`string`/`byte[]`                                                                                                                |
| `INetReader`          | The matching `GetByte()` … `GetBytes()`, plus `GetString(int maxBytes)` and `GetBytes(int maxLength)` for fields with a known bound                                                                                            |
| `RawHandler`          | `void (INetPeer peer, ReadOnlySpan<byte> data)`                                                                                                                                                                                |
| `NetRole`             | `None`, `Host`, `Client`                                                                                                                                                                                                       |
| `Delivery`            | `Reliable` (default), `Unreliable`, `ReliableUnordered`, `Sequenced`, `ReliableSequenced`                                                                                                                                      |
| `DisconnectReason`    | `ConnectionFailed`, `Timeout`, `RemoteClose`, `LocalClose`, `Rejected`, `Unknown`                                                                                                                                                |

## Notes & gotchas

- **Everything runs on the main thread.** The transport is polled from the API's `OnUpdate`, so
  handlers and events land on the Unity main thread and may touch game objects directly. The flip
  side: a slow handler costs frame time.
- **Writers and readers are reused instances.** Never stash the `INetWriter`/`INetReader` handed to
  `Write`/`Read`, and never keep the `ReadOnlySpan<byte>` from a raw handler past the callback.
- **Peers are only valid while connected.** After `OnPeerDisconnected` the `INetPeer` is dead.
- **No authority, no encryption, no validation.** Every peer can send any registered message, and
  packets are plain UDP. Treat incoming data as untrusted and keep the host authoritative over
  anything that matters — that is the coop mod's job, not the transport's.
- **Message ids are a compatibility contract.** Changing an id or a struct's field order breaks
  every peer on the old build. Version the payload if you need to evolve it.
- **Keep messages small.** Reliable messages larger than the MTU are fragmented and reassembled for
  you; unreliable ones above it cannot be sent at all.
- **Sending before a session exists does nothing.** `Send` and `SendToAll` are no-ops until
  `OnHostStarted` or `OnConnectedToHost` has fired. They are counted and summarized in the log rather
  than failing, so an early send shows up instead of vanishing.
- **A length prefix is never trusted.** `GetString()` and `GetBytes()` refuse a prefix naming more bytes
  than the packet holds, and the message is dropped rather than delivered half-read. Use the
  `maxBytes`/`maxLength` overloads wherever a field has a real bound.
