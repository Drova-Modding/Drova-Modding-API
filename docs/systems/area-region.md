# Areas & Regions

**What it does:** tracks which named regions the player is currently in and raises an event when
they enter or leave one. Useful for region-aware behavior such as ambience, spawns, "you are now in
the Mine" messages — and it's the foundation [regional world events](./world-events.md) build on.

Entry points:
- `Drova_Modding_API.Access.RegionAccess`, **start here**: read where the player is, and subscribe
  without worrying about whether the world exists yet.
- `Drova_Modding_API.Systems.AreaNameSystem`, the underlying singleton (`AreaNameSystem.Instance`).
- `Drova_Modding_API.Systems.Region` (enum) + `RegionExtensions`.

## Quick example

React whenever the player crosses a region boundary:

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems;
using MelonLoader;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        // Safe before the world exists, and safe across a return to the main menu.
        RegionAccess.OnRegionChanged += OnRegionChanged;
    }

    private void OnRegionChanged(Region region, bool hasEntered)
    {
        MelonLogger.Msg($"Player {(hasEntered ? "entered" : "left")} {region.GetRegionName()}");
    }
}
```

## How do I…?

### Ask where the player is right now

```csharp
if (RegionAccess.TryGetCurrentRegion(out Region region))
{
    MelonLogger.Msg($"You are in {region.GetRegionName()}");
}
```

Before this existed there was no way to *read* the current region, only to be told when it changed,
which forced every mod that cared to keep its own copy, updated from a callback it might have
subscribed to late.

The answer is **resolved, not picked**: standing in a cave inside a forest inside the overworld
resolves to the cave. For the rare caller that wants all of them unresolved:

```csharp
private readonly List<Region> _regions = new(); // Reused, so filling it allocates nothing.

RegionAccess.GetCurrentRegions(_regions);
```

### Check whether the player is in a specific region

```csharp
if (RegionAccess.IsInRegion(Region.DeathMoor)) { /* ... */ }
```

True whether it is the most specific one. This asks "am I anywhere in the moor", where
`TryGetCurrentRegion` asks "where am I".

### Resolve a region from a name in my config

```csharp
if (!RegionAccess.TryGetRegionByName(configuredArea, out Region region))
{
    LoggerInstance.Warning($"'{configuredArea}' is not a region this build knows.");
    return;
}
```

Use this rather than `RegionExtensions.GetRegionByName` for anything a user typed. See the notes.

### Check whether the player is in a cave

```csharp
bool inCave = RegionAccess.IsInCave();

// or directly on the system, if you already hold it:
bool alsoInCave = AreaNameSystem.Instance?.IsInCave() == true;
```

A region is considered a cave per `Region.IsCaveRegion()` (mines, dungeons, the academy, the
library, ruin interiors, etc.).

### Work with the `Region` enum

```csharp
Region r = RegionExtensions.GetRegionByName("Mine"); // string -> enum (case-insensitive)
string name = r.GetRegionName();                       // enum -> string
bool isCave = r.IsCaveRegion();
```

`Region` covers the game's areas: `RedTower, Mine, Cave, City, SpiderDungeon, Auwald, Nemeton,
EntryNemeton, Intro, Ruins, Tavern, CityDungeon, DeathMoor, Academy, Forest, Library, FriendlyMoor,
Mutter, Leuchtwald, River, RootenMoor, WoodCamp, RuinsCamp, RuinUnder, Magecamp, Ruinexplorer,
RuinSchmuggler, Hain, Heide, Schlund, Overworld_Or_Cave` (the last is the default/fallback).

### Trigger logic on entering a specific region

For one-off reactions you can branch in your `OnRegionChanged` handler, but if you want full
event lifecycle (start/stop, cooldowns, parallel rules) prefer an
[`ARegionalEvent`](./world-events.md):

```csharp
private void OnRegionChanged(Region region, bool hasEntered)
{
    if (hasEntered && region == Region.SpiderDungeon)
        MelonLogger.Msg("Watch out for spiders!");
}
```

## API reference

### `RegionAccess` (static)

| Member                                                        | Description                                                                                       |
|---------------------------------------------------------------|---------------------------------------------------------------------------------------------------|
| `event RegionChanged? OnRegionChanged`                        | Enter/leave, forwarded from `AreaNameSystem`. Safe to subscribe at any time, and survives reloads. |
| `bool TryGetCurrentRegion(out Region region)`                 | The region the player is most specifically in. `false` in menus and before they're placed.        |
| `void GetCurrentRegions(List<Region> buffer)`                 | Every region the player is inside, unresolved. Cleared first, and reusing one allocates nothing.  |
| `bool IsInRegion(Region region)`                              | Whether the player is inside it, most specific or not.                                            |
| `bool IsInCave()`                                             | Whether the player is underground. `false` in menus and outdoors.                                 |
| `bool TryGetRegionByName(string? areaKey, out Region region)` | Name → region, with an unrecognized name answered by `false` rather than the catch-all.           |

### `AreaNameSystem` (singleton: `AreaNameSystem.Instance`)

| Member                                | Description                                                |
|---------------------------------------|------------------------------------------------------------|
| `event RegionChanged OnRegionChanged` | Raised on enter/leave: `(Region region, bool hasEntered)`. |
| `bool IsInCave()`                     | Whether any current region is a cave.                      |

`delegate void RegionChanged(Region region, bool hasEntered)`.

> `OnAreaEntered` / `UnregisterAreaName` exist on the system but are called by the API's Harmony
> patches when the game raises area events — you subscribe to `OnRegionChanged`, you don't call
> these.

### `Region` (enum) & `RegionExtensions` (static)

| Member                                    | Description                                                    |
|-------------------------------------------|----------------------------------------------------------------|
| `string GetRegionName(this Region)`       | Enum → canonical name string.                                  |
| `Region GetRegionByName(string)`          | Name → enum (case-insensitive; unknown → `Overworld_Or_Cave`). |
| `bool IsCaveRegion(this Region)`          | Whether the region is a cave/interior.                         |
| `bool IsARegionInCave(this List<Region>)` | Whether any region in the list is a cave.                      |

## Notes & gotchas

- **`AreaNameSystem.Instance` is `null` outside gameplay, and it is a different object after every
  load.** A handler subscribed to the system directly stops firing the moment the player returns to
  the main menu and loads again, because the object it was attached to is gone.
  `RegionAccess.OnRegionChanged` keeps the subscriber list itself and rebinds to whichever system is
  current, so it does not care when you subscribe or how many times the world reloads.
- **Regions are a curated set**, not arbitrary strings — unknown area names map to
  `Overworld_Or_Cave`.
- **`RegionExtensions.GetRegionByName` cannot fail**, which is the problem: it answers an
  unrecognized name with the catch-all region rather than an error, so a typo in a mod's
  configuration binds silently to a bucket that really exists, and then behaves as though every
  unmatched area were that region. `RegionAccess.TryGetRegionByName` returns `false` instead, and
  only lets `Overworld_Or_Cave` through when the caller asked for it by name.
- **The player can be in multiple regions at once** (overlapping areas), and the game resolves that
  by priority rather than order of entry, so a single "current region" is a convenience, not a fact
  about the world. `TryGetCurrentRegion` picks the innermost. Caves win, and beyond that the last one
  entered, which matches how areas nest in practice. `GetCurrentRegions` gives you all of them.
- **`IsInCave()` checks if *any* current region is a cave**, not the resolved one.
- For lifecycle-managed reactions to region changes, build an [`ARegionalEvent`](./world-events.md)
  instead of hand-rolling logic in `OnRegionChanged`.
