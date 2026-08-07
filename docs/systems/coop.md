# Co-op

**What it does:** tells a mod whether somebody else is in the world, and whether *this* machine is
the one that should be acting on it. Use it whenever a mod spawns creatures, rolls dice, or writes
world state, which are the things that go wrong when two games do them independently.

Entry point: `Drova_Modding_API.Access.CoopAccess` (static). Supporting types live in
`Drova_Modding_API.Systems.Coop`.

> **This is not the [networking transport](./networking.md).** Networking is sockets and packets.
> `CoopAccess` is a contract with no transport dependency: on a machine with no co-op mod at all it
> answers "single player, you decide everything", and a mod written against it behaves exactly as it
> always did.

## Quick example

One line wherever the mod is about to act on the world.

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Coop;
using MelonLoader;

public class Core : MelonMod
{
    private void SpawnMyEncounter()
    {
        // True in single-player, false on a joining client.
        if (!CoopAccess.ShouldRun(CoopBehavior.AuthorityGated)) return;

        // ... spawn it
    }
}
```

`ShouldRun` is `true` in single-player for every behavior, so adding the line does not change how
your mod behaves on the overwhelming majority of machines.

## How do I…?

### Decide whether to run

Pass the behavior of the work you are about to do:

```csharp
CoopAccess.ShouldRun(CoopBehavior.SinglePlayerOnly);  // false while a session is live
CoopAccess.ShouldRun(CoopBehavior.AuthorityGated);    // false off the authority, true solo
CoopAccess.ShouldRun(CoopBehavior.FullyReplicated);   // always true, the mod replicates itself
```

The behavior is named at the call rather than looked up from a registration, because a lookup keyed
on the calling assembly is unreliable once the runtime inlines the call.

### Announce what my mod does

```csharp
public override void OnInitializeMelon()
{
    CoopAccess.Declare(CoopBehavior.AuthorityGated);
}
```

This gates nothing. It writes one log line, so a player sending in a log says along with it what
every installed mod believes about itself.

### Check whether anyone else is actually there

```csharp
if (CoopAccess.IsMultiplayer)
{
    // A session is live and somebody else is in it.
}
```

`IsMultiplayer` is `false` for a host sitting alone in a session nobody has joined. The question is
whether there is another player, not whether a socket is open.

### Walk the players in the session

```csharp
private readonly List<CoopPlayer> _players = new(); // Reused, so filling it allocates nothing.

private void ListPlayers()
{
    CoopAccess.GetPlayers(_players);

    foreach (CoopPlayer player in _players)
    {
        LoggerInstance.Msg($"{player.Name} (level {player.Level}) " +
            $"{(player.IsAuthority ? "decides the world" : "follows")}");
    }
}
```

With no session the list holds the local player alone, so you do not need a special case.

### Avoid acting on somebody else's character

```csharp
if (CoopAccess.IsRemotePlayer(entity)) return;
```

A remote body is cloned from the player prefab, so it answers *yes* to the game's own "is this the
player" tests while being driven entirely from the wire.

### Not react to a change that came from elsewhere

```csharp
[HarmonyPostfix]
private static void OnSomethingChanged()
{
    // This change arrived from another machine.
    if (CoopAccess.IsReplicating) return;

    // ... react to a genuinely local change
}
```

Without the guard, a mod that reports what it observes hands a replicated change straight back to
where it came from.

### Make the same random decision on every machine

```csharp
// Every machine derives the same answer from the same key.
int count = CoopAccess.RollRange("banditcamp.spawncount", 2, 5);

for (int index = 0; index < count; index++)
{
    float variance = CoopAccess.RollValue($"banditcamp.creature.{index}");
    // ...
}
```

Read the notes below first. Sharing a roll is the *second*-best tool, and letting the authority
decide is usually the right answer.

### Ask whether the installed co-op mod syncs what I need

```csharp
if (!CoopAccess.Supports(CoopCapability.SpawnedNpcs))
{
    // Spawned creatures are not shared, so do not spawn a group everyone is supposed to see.
}
```

### React to the session changing

```csharp
CoopAccess.OnSessionChanged += () => { /* somebody joined or left, or authority moved */ };
CoopAccess.OnProviderChanged += () => { /* a co-op mod registered or went away, drop caches */ };
```

Both are raised on the main thread, and each subscriber is isolated: one that throws does not stop
the rest being told.

### Implement co-op (for co-op mod authors)

Implement `ICoopProvider`, register it, and tell the API when the session changes:

```csharp
public sealed class MyCoopProvider : ICoopProvider { /* ... */ }

private readonly MyCoopProvider _provider = new();

public override void OnInitializeMelon()
{
    CoopAccess.RegisterProvider(_provider);
}

private void OnSomebodyJoined()
{
    CoopAccess.NotifySessionChanged();
}

public override void OnDeinitializeMelon()
{
    CoopAccess.UnregisterProvider(_provider); // Identity-checked.
}
```

## API reference

### `CoopAccess` (static)

| Member                                                          | Description                                                                                          |
|-----------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| `bool ShouldRun(CoopBehavior behavior)`                         | Whether work of that kind should happen here. `true` in single-player for every behavior.            |
| `bool IsWorldAuthority`                                         | Whether this machine decides what the shared world contains. **`true` when nothing has registered.** |
| `bool IsMultiplayer`                                            | Whether a session is live with somebody else in it. `false` when nothing has registered.             |
| `bool HasProvider`                                              | Whether any co-op implementation has registered.                                                     |
| `bool IsReplicating`                                            | Whether a change from elsewhere is being applied right now.                                          |
| `void Declare(CoopBehavior behavior)`                           | Announces what the calling mod does, for the log. Gates nothing.                                     |
| `void Declare(string modName, CoopBehavior behavior)`           | Same, for a mod that spans assemblies or wants to be explicit.                                       |
| `void GetPlayers(List<CoopPlayer> buffer)`                      | Fills the list with everybody in the session, the local player included. Cleared first.              |
| `bool TryGetPlayerActor(CoopPlayerId id, out Actor actor)`      | The body being shown for a player. `false` when they have none here.                                 |
| `bool IsRemotePlayer(Entity? entity)`                           | Whether this is somebody else's body. Always `false` with no session.                                |
| `bool Supports(CoopCapability capability)`                      | Whether the installed implementation keeps that thing in step. Always `false` with no session.       |
| `ulong SessionSeed`                                             | A number every machine in the session agrees on. Not stable across a save and load.                  |
| `uint Roll(string key)`                                         | Derives a number from the session seed.                                                              |
| `float RollValue(string key)`                                   | Derives a number in `[0, 1)`.                                                                        |
| `int RollRange(string key, int minInclusive, int maxExclusive)` | Derives a number in a half-open range.                                                               |
| `event Action? OnSessionChanged`                                | Somebody joined or left, or authority moved.                                                         |
| `event Action? OnProviderChanged`                               | A co-op implementation registered or unregistered.                                                   |
| `void RegisterProvider(ICoopProvider provider)`                 | Registers an implementation. One at a time, and a second replaces the first and warns.               |
| `void UnregisterProvider(ICoopProvider provider)`               | Takes a registration back. Identity-checked.                                                         |
| `void NotifySessionChanged()`                                   | Called by the implementation when players join or leave, or authority moves.                         |

### `CoopBehavior` (enum)

| Value              | `ShouldRun` answers                                              |
|--------------------|------------------------------------------------------------------|
| `Unreviewed` (`0`) | always `true`. The default, meaning "nobody has said".           |
| `SinglePlayerOnly` | `false` while a session is live.                                 |
| `AuthorityGated`   | `false` on a machine that is not the authority (so `true` solo). |
| `FullyReplicated`  | always `true`. The mod replicates its own effects.               |

### `CoopCapability` (enum)

`PlayerBodies`, `PlayerHealth`, `GlobalVariables`, `StatusEffects`, `Knowledge`, `SpawnedNpcs`,
`EnemyHealth`, `Loot`, `Experience`. Query it rather than assuming, because what a co-op mod syncs
changes release by release.

`Experience` is the one to check before scaling anything by player level. Where experience is not
shared, the players in a session drift apart in level for as long as they play, so "the player's
level" is not one number and a difficulty curve built on it fits at most one of them.

### `CoopPlayer` / `CoopPlayerId` (readonly structs)

| Member              | Description                                                                       |
|---------------------|------------------------------------------------------------------------------------|
| `CoopPlayerId Id`   | Who they are. Opaque, so compare and pass it around rather than constructing one. |
| `string Name`       | Never null. Comes from a stranger, so treat it as text.                           |
| `bool IsLocal`      | Whether this is the person at this machine.                                       |
| `bool IsAuthority`  | Whether their machine decides what the shared world contains.                     |
| `int Level`         | Their character level. **Zero means "not known yet"**, not level zero.            |
| `CoopPlayerId.None` | Nobody. What an unset or unknown player reads as.                                 |

### `ICoopProvider` (interface, for co-op mod authors)

The same surface `CoopAccess` exposes, which is what it forwards to: `IsMultiplayer`,
`IsWorldAuthority`, `SessionSeed`, `IsReplicating`, `GetPlayers(List<CoopPlayer>)`,
`TryGetPlayerActor(CoopPlayerId, out Actor)`, `IsRemotePlayer(Entity?)` and
`Supports(CoopCapability)`.

### `CoopRandom` (static)

| Member                                                                         | Description                               |
|--------------------------------------------------------------------------------|-------------------------------------------|
| `uint Derive(ulong seed, string? key)`                                         | FNV-1a over the seed's bytes and the key. |
| `float DeriveValue(ulong seed, string? key)`                                   | The same, mapped into `[0, 1)`.           |
| `int DeriveRange(ulong seed, string? key, int minInclusive, int maxExclusive)` | The same, mapped into a range.            |

## Notes & gotchas

- **The defaults are inverted from what a networking API would choose, and that is the whole
  design.** `IsWorldAuthority` is `true` and `IsMultiplayer` is `false` when nothing has registered,
  so a mod that gates on authority runs normally on every machine without a co-op mod, which is
  nearly all of them. Any other default would silently disable working single-player mods for
  everyone who installed this API.
- **Do not use `NetworkAccess.Role` instead.** It answers a different question and fails closed.
  With no networking build it is `None`, so `if (Role == Host) Spawn();` spawns nothing in
  single-player. A transport role is also not world authority, because a peer is a client while it
  is still connecting and before it has an identity.
- **`SessionSeed` is not stable across a save and load**, and there is no way to make it so. A Drova
  save is identified by hero index and slot index alone, which are the same numbers on every machine
  and in every playthrough. A mod needing randomness that survives a reload has to persist its own.
- **Shared rolls are the second-best tool.** Sharing a seed makes two machines agree only if
  everything else feeding the decision already agrees, which is rarer than it looks. Anything
  filtered by the local player's level or placed relative to their position diverges regardless of
  the roll. The first answer is nearly always to let the authority decide and replicate the result.
- **Name each draw specifically.** `Roll` is keyed derivation, never a sequential stream, so two
  draws sharing a key are one draw. Use `"banditcamp.creature.0"`, not `"creature"`. A shared
  `Random` would look easier and is a trap, because two machines only stay in step if they draw from
  it the same number of times in the same order, and nothing can enforce that.
- **`Supports` returning `false` with no session is not a missing feature.** It reads as "there is
  nobody to be out of step with". `ShouldRun` is the call most mods actually want.
- **Only one implementation can register.** A second replaces the first and logs a warning, because
  two implementations disagreeing about who the authority is would be worse than either being wrong
  alone.
