# Networking (coop transport)

**What it does:** gives mods a small UDP transport — start or join a session, send and receive
messages, and react to connection events. It is **plumbing only**: it never syncs game state and
never draws UI. A coop mod builds lobby, UI, and world sync on top of it.

Entry points: `Drova_Modding_API.Access.NetworkAccess` and
`Drova_Modding_API.Access.NetworkEvents` (both static).

> **The transport is optional.** The public surface above always exists, but the implementation is
> only compiled into a **coop-enabled API build** (`-p:Coop=true`). Check
> `NetworkAccess.IsSupported` first — on a plain build every action method throws
> `NotSupportedException` so a missing coop build fails loudly instead of silently doing nothing.
> See [Building with coop support](#building-with-coop-support).

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
        if (!NetworkAccess.IsSupported)
        {
            LoggerInstance.Warning("Install the coop-enabled Drova Modding API build.");
            return;
        }

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

The host must have UDP `9050` reachable (port forwarding or a relay, see
[Relay](#going-through-a-relay)).

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
it. Ids `0`–`255` are valid.

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
and `63` for relay control traffic; `1`–`62` are yours.

```csharp
NetworkAccess.RegisterChannel(1, (peer, data) =>
{
    // data is a ReadOnlySpan<byte> valid only for this call - copy what you need to keep.
});

Span<byte> payload = stackalloc byte[8];
NetworkAccess.SendRaw(peer, 1, payload);
```

## Building with coop support

The default build has **no** networking implementation and no LiteNetLib dependency at all. To get
the coop-enabled build:

```sh
dotnet build "Drova Modding API.sln" -c Release -p:Coop=true
```

That defines `NETCOOP`, compiles `Systems/Networking/Impl/*`, pulls in LiteNetLib 2.1.4, and copies
`LiteNetLib.dll` into the game's **`UserLibs`** folder (shared managed dependencies, same place as
`NVorbis.dll`). The mod DLL in `Mods` never references LiteNetLib on a default build.

### Turning it on in Rider (or Visual Studio)

The solution has four build configurations, so coop is a dropdown toggle in the IDE toolbar:

| Configuration | Dev tooling (dialogue editor, F6 inspector, NPC wizard) | Coop transport |
|---------------|---------------------------------------------------------|----------------|
| `Debug`       | yes                                                     | no             |
| `DebugCoop`   | yes                                                     | yes            |
| `Release`     | no                                                      | no             |
| `ReleaseCoop` | no                                                      | yes            |

The `Coop` variants are the base configuration plus the transport — same `DEBUG` define, same
optimization settings, own `bin`/`dist` output so a switch never reuses the other build's artifacts.
Each produces its own zip: `Modding_API_Dev.zip`, `Modding_API_Dev_Coop.zip`, `Modding_API.zip`,
`Modding_API_Coop.zip`.

Two other ways to the same result, if you would rather not switch configuration:

| Way                  | How                                                                                      |
|----------------------|------------------------------------------------------------------------------------------|
| Environment variable | Set `DROVA_COOP=1` in Windows, then restart Rider. The `.csproj` reads it as a fallback. |
| Command line         | `dotnet build … -p:Coop=true` on any configuration.                                      |

An explicit `-p:Coop=true` wins over everything; the configuration name comes next; `DROVA_COOP` is
the last resort. The build log line `Packaged … (coop: true)` confirms which you got.

### Shipping a coop build

`dist/Modding_API.zip` from a `-p:Coop=true` build contains `Mods/Drova_Modding_API.dll` plus
`UserLibs/LiteNetLib.dll`. Users of a coop mod need **both**; the DLL alone will fail to resolve
LiteNetLib at runtime.

## Going through a relay

Direct UDP needs the host to be reachable, which home connections usually are not without port
forwarding. A relay fixes that: host and clients all connect **out** to one server that forwards
their packets, addressed by a short session code.

```csharp
// Host: let the relay pick the code, and set a password to encrypt the session.
NetworkAccess.StartHostViaRelay("relay.example.com", 9060, null, "shared-secret");

NetworkEvents.OnHostStarted += () =>
{
    // Assigned by the relay and available by the time this fires. Show it to the players who join.
    LoggerInstance.Msg($"Session {NetworkAccess.SessionCode}, encrypted: {NetworkAccess.IsEncrypted}");
};

// Client: join that code with the same password.
NetworkAccess.ConnectViaRelay("relay.example.com", 9060, "ABCD12", "shared-secret");
```

Passing a code of your own to `StartHostViaRelay` works, but a relay-assigned code is better: it has
real entropy, so nobody joins by guessing. A relay can be configured to require its own codes.

Everything else is unchanged: same messages, same events, same `INetPeer`. Differences worth knowing:

- **Success is asynchronous.** `StartHostViaRelay` returns before the relay has accepted the code —
  wait for `NetworkEvents.OnHostStarted`, and handle `OnConnectionRejected` for a refusal (code taken,
  wrong password, relay full).
- **`INetPeer.Ping` is the round trip to the relay**, not to that player. Read it as a lower bound.
- **`INetPeer.Disconnect()` on the host kicks that client**; a client calling it on the host peer
  leaves the session.
- **Raw channel 63 is unavailable** — it carries relay control traffic. `RegisterChannel` refuses it in
  direct mode too, so channel numbers keep working when a session moves to a relay.

### Encryption

**A relay session with a password is encrypted end to end**, checked through
`NetworkAccess.IsEncrypted`. The key is derived from the password and the session code, so nothing is
exchanged for a relay to sit in the middle of, and both halves of the trust problem close: the relay
cannot read the traffic, and it cannot forge a message from another player either — the sender id and
channel are authenticated, and the relay is what stamps ids. A wrong password simply fails to decrypt,
and the transport logs a summary rather than one line per packet.

What that costs and what it does not cover:

- **28 bytes per message** (nonce and tag), out of the relay's payload budget — around 1200 bytes by
  default. Size messages accordingly.
- **An open session is not encrypted.** No password, no key. `IsEncrypted` says so; show it.
- **Replaying a genuine packet is still possible** for a hostile relay. Rejecting repeats would break
  unreliable channels, so that bound belongs to your own state handling.
- Addresses, packet sizes and timing stay visible to the relay regardless.

Keep the host authoritative over anything that matters, and tell players whose relay they are on.

The server is its own project (`Drova-Coop-Relay`) with the wire protocol specified in its
`docs/protocol.md`; the design notes live in
[`docs/coop-networking-plan.md`](../plans/coop-networking-plan.md#relay).

## API reference

### `NetworkAccess`

| Member                                                                                                            | Description                                                                                    |
|-------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|
| `bool IsSupported`                                                                                                | Whether this API build contains the transport. Check before anything else.                     |
| `NetRole Role`                                                                                                    | `None`, `Host` or `Client`.                                                                    |
| `bool IsConnected`                                                                                                | A session is running and at least one peer is connected.                                       |
| `int PeerCount`                                                                                                   | How many remote peers are connected.                                                           |
| `string? SessionCode`                                                                                             | The relay session code in use, including one the relay assigned. Null outside a relay session. |
| `bool IsEncrypted`                                                                                                | Whether messages are sealed end to end. True only for a relay session with a password.         |
| `void StartHost(int port, string? key = null)`                                                                    | Listen on a UDP port; clients must present the same `key` when set.                            |
| `void Connect(string address, int port, string? key = null)`                                                      | Join a host.                                                                                   |
| `void StartHostViaRelay(string relayAddress, int relayPort, string? sessionCode = null, string? password = null)` | Host through a relay; null code asks the relay to pick one. Confirmed by `OnHostStarted`.      |
| `void ConnectViaRelay(string relayAddress, int relayPort, string sessionCode, string? password = null)`           | Join a relay session. Confirmed by `OnConnectedToHost`.                                        |
| `void Stop()`                                                                                                     | Close the session and all connections.                                                         |
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
| `Action<ConnectionRejection>? OnConnectionRejected`      | A connection attempt was refused, with the reason when there is one.    |
| `Action<string>? OnNetworkError`                         | A socket-level error occurred. The string is for logs, not for parsing. |

### Supporting types

| Type                  | Members                                                                                                                                                                               |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `INetPeer`            | `int Id`, `int Ping`, `void Disconnect()`                                                                                                                                             |
| `INetMessage`         | `void Write(INetWriter)`, `void Read(INetReader)`                                                                                                                                     |
| `INetWriter`          | `Put` for `byte`/`sbyte`/`short`/`ushort`/`int`/`uint`/`long`/`ulong`/`float`/`double`/`bool`/`string`/`byte[]`                                                                       |
| `INetReader`          | The matching `GetByte()` … `GetBytes()`                                                                                                                                               |
| `RawHandler`          | `void (INetPeer peer, ReadOnlySpan<byte> data)`                                                                                                                                       |
| `NetRole`             | `None`, `Host`, `Client`                                                                                                                                                              |
| `Delivery`            | `Reliable` (default), `Unreliable`, `ReliableUnordered`, `Sequenced`, `ReliableSequenced`                                                                                             |
| `DisconnectReason`    | `ConnectionFailed`, `Timeout`, `RemoteClose`, `LocalClose`, `Rejected`, `Unknown`, plus `SessionClosed` and `Kicked` in relay sessions                                                |
| `ConnectionRejection` | `Unspecified`, `BadHandshake`, `BadSessionCode`, `SessionNotFound`, `SessionFull`, `DuplicateHost`, `WrongPassword`, `RelayFull`, `RateLimited`, `TooManySessions`, `TooManyAttempts` |

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
