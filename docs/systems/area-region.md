# Areas & Regions

**What it does:** tracks which named regions the player is currently in and raises an event when
they enter or leave one. Useful for region-aware behavior — ambience, spawns, "you are now in
the Mine" messages — and it's the foundation [regional world events](./world-events.md) build on.

Entry points:
- `Drova_Modding_API.Systems.AreaNameSystem` — singleton (`AreaNameSystem.Instance`).
- `Drova_Modding_API.Systems.Region` (enum) + `RegionExtensions`.

## Quick example

React whenever the player crosses a region boundary:

```csharp
using Drova_Modding_API.Systems;
using MelonLoader;

public class Core : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        var areas = AreaNameSystem.Instance;
        if (areas != null)
            areas.OnRegionChanged += OnRegionChanged;
    }

    private void OnRegionChanged(Region region, bool hasEntered)
    {
        MelonLogger.Msg($"Player {(hasEntered ? "entered" : "left")} {region.GetRegionName()}");
    }
}
```

`AreaNameSystem.Instance` exists during gameplay (it's created on the gameplay scene), so subscribe
once it's available — re-checking on a scene load is a simple way to do that.

## How do I…?

### Check whether the player is in a cave

```csharp
bool inCave = AreaNameSystem.Instance?.IsInCave() == true;
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

- **`Instance` is `null` outside gameplay.** Subscribe after the gameplay scene loads.
- **Regions are a curated set**, not arbitrary strings — unknown area names map to
  `Overworld_Or_Cave`.
- **The player can be in multiple regions at once** (overlapping areas); `IsInCave()` checks if
  *any* current region is a cave.
- For lifecycle-managed reactions to region changes, build an [`ARegionalEvent`](./world-events.md)
  instead of hand-rolling logic in `OnRegionChanged`.
