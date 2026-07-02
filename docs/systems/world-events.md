# World Events

**What it does:** runs your own "world events" — scripted happenings the system fires on a
timer while the player roams — and **regional events** that trigger when the player enters a
specific [region](./area-region.md). The manager handles cooldowns, blocking (no events during
dialogue, in menus, or in blocked regions), and auto-ending stuck events.

Entry points:
- `WorldEventSystemManager` — singleton (`WorldEventSystemManager.Instance`) + static registration.
- `IWorldEvent` — a global event.
- `ARegionalEvent` — a region-triggered event.

## Quick example

Register a global event up front; the system picks registered events at random over time:

```csharp
using Drova_Modding_API.Systems.WorldEvents;
using MelonLoader;

public sealed class MyAmbush : IWorldEvent
{
    public void StartEvent()  { /* spawn enemies, show a message, … */ }
    public void EndEvent()    { /* clean up */ }
    public bool CanStartEvent() => true; // gate on your own conditions
}

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        WorldEventSystemManager.RegisterWorldEvent(new MyAmbush());
    }
}
```

The manager only runs one event at a time and respects a configurable cooldown (the mod options
expose min/max timers). It won't start events while the player is in dialogue, teleporting, in
the options menu, or in a blocked region.

## How do I…?

### Start or stop an event immediately

```csharp
var mgr = WorldEventSystemManager.Instance;
if (mgr != null)
{
    mgr.StartEvent(new MyAmbush()); // ignored if another event is already running
    // …later…
    mgr.EndEvent();
}
```

### Make an event trigger when the player enters a region

Subclass `ARegionalEvent` with the [region](./area-region.md) to watch:

```csharp
using Drova_Modding_API.Systems;
using Drova_Modding_API.Systems.WorldEvents.Regional;

public sealed class CaveWhispers : ARegionalEvent
{
    public CaveWhispers() : base(Region.Cave) { }

    public override void OnRegionEntered() { /* start ambience / spawn */ }
    public override void OnRegionLeft()    { /* stop it */ }

    // Optional: allow this event to run alongside another in the same region.
    public override bool CanRunParallel() => false;
}

// register it:
WorldEventSystemManager.RegisterRegionalEvent(new CaveWhispers());
```

When the player enters the region, `OnRegionEntered()` fires; one eligible regional event is
started (plus any that opt into parallel running), and a per-region cooldown is applied.

### Block events in certain regions

```csharp
WorldEventSystemManager.Instance?.AddBlockedRegion(Region.Tavern);
WorldEventSystemManager.Instance?.RemoveBlockedRegion(Region.Tavern);
```

Several safe regions (towns, camps, the Nemeton, the Red Tower) are blocked by default.

### Check player state before doing something

```csharp
if (WorldEventSystemManager.IsPlayerInDialogueOrTeleporting()) return;
if (WorldEventSystemManager.Instance?.IsPlayerInBlockedRegion() == true) return;
```

## API reference

### `WorldEventSystemManager` (singleton: `.Instance`, may be `null` outside gameplay)

| Member                                                                                          | Description                                                                   |
|-------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------|
| `static void RegisterWorldEvent(IWorldEvent)` / `RegisterWorldEvents(IEnumerable<IWorldEvent>)` | Register global event(s) for random selection.                                |
| `static void RegisterRegionalEvent(ARegionalEvent)` / `RegisterRegionalEvents(IEnumerable<…>)`  | Register region-triggered event(s).                                           |
| `void StartEvent(IWorldEvent)`                                                                  | Start an event now (ignored if one is running or `CanStartEvent()` is false). |
| `void EndEvent()`                                                                               | End the current event.                                                        |
| `void AddBlockedRegion(Region)` / `RemoveBlockedRegion(Region)`                                 | Manage regions where events won't spawn.                                      |
| `bool IsPlayerInBlockedRegion()`                                                                | Whether the player is in a blocked region.                                    |
| `static bool IsPlayerInDialogueOrTeleporting()`                                                 | Whether the player is busy.                                                   |
| `IWorldEvent? CurrentEvent { get; }`                                                            | The running event, if any.                                                    |
| `IReadOnlyList<ARegionalEvent> RegionalEvents { get; }`                                         | Currently running regional events.                                            |

### `IWorldEvent`

| Member                 | Description                   |
|------------------------|-------------------------------|
| `void StartEvent()`    | Called when the event starts. |
| `void EndEvent()`      | Called when the event ends.   |
| `bool CanStartEvent()` | Gate; default `true`.         |

### `ARegionalEvent(Region regionToTrigger)` (abstract, implements `IWorldEvent`)

| Member                                               | Description                                                           |
|------------------------------------------------------|-----------------------------------------------------------------------|
| `abstract void OnRegionEntered()`                    | Player entered the region.                                            |
| `abstract void OnRegionLeft()`                       | Player left the region.                                               |
| `virtual bool CanRunParallel()`                      | Allow running alongside another event in the region; default `false`. |
| `bool IsRunning { get; }` / `Region Region { get; }` | Runtime state / target region.                                        |

## Notes & gotchas

- **`Instance` is `null` in menus.** The manager is created on the gameplay scene; register events
  (the static methods) any time, but call instance methods only during gameplay.
- **One global event at a time.** `StartEvent` is ignored while another is running; the timer also
  skips while the player is busy or in a blocked region, and force-ends events that overstay.
- **Cooldowns are config-driven.** The mod's options panel exposes the min/max event timers and a
  regional cooldown (see [Options Menu](./options-menu.md) / [Config](./config.md)).
- Regional events depend on the [Areas & Regions](./area-region.md) system to detect entry/exit.
