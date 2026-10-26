# Map Editor

Change the overworld without touching the game's files: place props, chests and traps, put up
invisible walls, hide vanilla objects and add new walkable areas. Everything lives in JSON layout
files in `Mods/Modding_API/MapEditor/*.json` that ship with your mod.

## Example

```json
{
  "Version": 1,
  "Objects": [
    { "Id": "Banner_Ruins_Big_00_1", "Prefab": "0ae6038fe2f808243af1008a269593c6", "X": -2132, "Y": -5180 },
    { "Id": "Blocker_1", "X": -2132, "Y": -5330, "Width": 64, "Height": 64 },
    {
      "Id": "Container_Chest_Shod_Silver_1",
      "Scene": "Scene_Overworld_Interaction_-1_1",
      "Path": "Object/Default/Container_Chest_Shod_Silver",
      "X": -2000, "Y": -5262,
      "Locked": false,
      "Loot": [ { "Item": "item_potion_health_1", "Amount": 2 }, { "Item": "item_apple", "Amount": 3 } ]
    },
    { "Id": "Trap_BearTrap_1", "Dynamic": "Trap_BearTrap", "X": -2300, "Y": -5200 }
  ],
  "Removed": [
    { "Scene": "Scene_Overworld_Deco_Heather_Small_B_Chap4_Untouched_-2_-4", "Path": "Plants/Default/Plant_Heather_Small_01[35]" }
  ]
}
```

An object is one of four kinds, picked by which field is set:

| Kind                     | Field             | Examples                                               |
|--------------------------|-------------------|--------------------------------------------------------|
| Addressable prefab       | `Prefab`          | Ruins, banners, camps, bushes, decals.                 |
| Copy of a world object   | `Scene` + `Path`  | Chests, bags, trap plates, barrels, pickups, plants.   |
| Dynamic object           | `Dynamic`         | `Trap_BearTrap`, `Trap_AcidTrap`, `Trap_SplitterTrap`. |
| Blocker (invisible wall) | none of the above | A wall of `Width` × `Height`.                          |

A copy is the real object with all its behavior: a chest opens and keeps its contents in the save, a
trap springs, a pickup can be taken.

## Fields

| Field                                    | Meaning                                                                                                  |
|------------------------------------------|----------------------------------------------------------------------------------------------------------|
| `Objects[].Id`                           | Unique across all layout files.                                                                          |
| `Objects[].Prefab`                       | Addressable guid of an environment prefab (see `MapPrefabCatalog`).                                      |
| `Objects[].Scene`, `Path`                | Where the original world object sits (see the scene prop catalog).                                       |
| `Objects[].Dynamic`                      | Asset id of one of the game's dynamic objects.                                                           |
| `Objects[].X`, `Y`                       | World position.                                                                                          |
| `Objects[].Scale`, `FlipX`, `Rotation`   | Uniform scale, horizontal mirror, degrees counterclockwise.                                              |
| `Objects[].Width`, `Height`              | Blocker size before `Scale`. Default 64.                                                                 |
| `Objects[].Loot`                         | Contents of a copied container (`Item`, `Amount`). Leave it out to keep the original's loot.             |
| `Objects[].Locked`                       | Locks or unlocks a copied chest. Leave it out to keep the original's lock.                               |
| `Objects[].Crime`                        | `true` keeps the original's crime reactions (taking counts as stealing). Off by default.                 |
| `Objects[].Stamp`                        | Paints the ground: only the sprites, no behavior or colliders.                                           |
| `Objects[].SortingLayer`, `SortingOrder` | A stamp's sorting. Default `Floor` / -10: above the baked ground (-16), under water, bridges and decals. |
| `Objects[].Color`                        | A stamp's tint, `#RRGGBB` or `#RRGGBBAA`.                                                                |
| `Objects[].Terrain`                      | Footstep sound on a stamp: `Grass`, `Dirt`, `Stone`, `Water`, `Wood`, `Bones`, `Mud`, `Sand`.            |
| `Objects[].TargetX`, `TargetY`           | Where a copied gate (vanilla teleporter) sends the player.                                               |
| `Objects[].Seed`                         | Filled in by the editor. Keeps a new object from inheriting the saved state of a deleted one.            |
| `Removed[].Scene`                        | Scene that holds the vanilla object to hide, usually `Scene_Overworld_<Layer>_<x>_<y>`.                  |
| `Removed[].Path`                         | Names from the scene root, joined by `/`. A repeated sibling name carries its rank: `Name[2]`.           |
| `Areas[]`                                | New walkable areas: `Id`, center `X`/`Y`, `Width`, `Height` (default 1600), `Indoor`. See below.         |

## The editor (Debug builds)

Pause menu → **Modding Tools → Map Editor**. Every change is saved at once to
`MapEditor/map_editor.json`; ship that file (or rename it) with your mod. **Reload Content** in
Modding Tools re-reads all layout files.

| Action                | How                                                                                                                       |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------|
| Place                 | Pick an entry in the list, click in the world. Search matches name and category.                                          |
| Blocker               | **Blocker** at the top of the list, then click.                                                                           |
| Paint the ground      | **Paint ground**, then place decals: they become stamps.                                                                  |
| Select / move         | Click a placed object or blocker, drag it.                                                                                |
| Change                | **Flip**, **Smaller**, **Bigger**, **Turn**, **Delete**; blockers use **W-/W+**, **H-/H+**.                               |
| Add an area           | **Area**, then drag a rectangle (at least 64 × 64). Click inside to select; **W-/W+/H-/H+**, **Indoor**, **Delete area**. |
| Hide a vanilla object | **Hide vanilla objects**, click it, **Parent** widens the pick, **Hide**. **Restore last** undoes a hide.                 |
| Undo                  | **Undo** or `Ctrl+Z`, last 50 changes, kept across sessions in `map_editor.undo`.                                         |
| Cancel / jump         | Right click cancels. `T` moves the player to the mouse.                                                                   |

## New areas

An area is an empty rectangle far outside the map that gets its own navmesh, like Drova's interiors.
You add a floor, walls and a pair of gates:

```json
{
  "Areas": [ { "Id": "glade", "X": 80800, "Y": -79200, "Width": 1600, "Height": 1600, "Indoor": true } ],
  "Objects": [
    { "Id": "glade_floor", "Scene": "Scene_Overworld_Floor_-2_-4", "Path": "SplatPlaneLoader(Clone)",
      "X": 80400, "Y": -79600, "Stamp": true, "SortingOrder": -16, "Terrain": "Grass" },
    { "Id": "glade_enter", "Scene": "Scene_Teleporters", "Path": "Teleporter_Atlanterdungeon_Enter",
      "X": -2132, "Y": -5200, "TargetX": 80800, "TargetY": -79200 },
    { "Id": "glade_exit", "Scene": "Scene_Teleporters", "Path": "Teleporter_Atlanterdungeon_Enter",
      "X": 80800, "Y": -79320, "TargetX": -2132, "TargetY": -5320 },
    { "Id": "glade_wall", "X": 80800, "Y": -79000, "Width": 400, "Height": 32 }
  ]
}
```

- **Where:** around `80000, -80000` is safely outside the map (a chunk is 1600 units). Outside the map the
  game shows a dark background.
- **Floor:** stamp a whole chunk floor (`Scene_Overworld_Floor_<x>_<y>` / `SplatPlaneLoader(Clone)`,
  order -16). It covers 1600 × 1600 and `X`/`Y` is the center of its lower-left quarter.
- **Gates:** copy any walk-in gate from `Scene_Teleporters` with `TargetX`/`TargetY`. Put the exit gate
  away from the entry gate's arrival point, or the player walks straight back out.
- **Weather:** `Indoor: true` means no rain or snow and indoor light, like a cave.
- **NPCs** can walk in through a gate; see `Gate` in [Routines](./routines.md).
- Saving inside an area works; the player loads back in there.

## Code

```csharp
MapObjectDefinition banner = MapLayoutSystem.Add("0ae6038fe2f808243af1008a269593c6", new Vector2(-2132, -5180));
banner.Scale = 1.5f;
MapLayoutSystem.Update(banner);

MapObjectDefinition wall = MapLayoutSystem.Add(string.Empty, new Vector2(-2132, -5330));

MapObjectDefinition chest = MapLayoutSystem.Add(new MapObjectDefinition
{
    Scene = "Scene_Overworld_Interaction_-1_1",
    Path = "Object/Default/Container_Chest_Shod_Silver",
    X = -2000,
    Y = -5262,
    Locked = false,
    Loot = [new MapLootDefinition { Item = "item_apple", Amount = 3 }]
});

MapRemovalDefinition removal = MapLayoutSystem.Hide(someVanillaObject);
MapLayoutSystem.Unhide(removal);

MapLayoutSystem.Reload();
```

| Member                                                         | Description                                                                           |
|----------------------------------------------------------------|---------------------------------------------------------------------------------------|
| `Add(string prefabGuid, Vector2)` / `Add(MapObjectDefinition)` | Places an object. An empty guid places a blocker.                                     |
| `Update`, `Remove(string id)`                                  | Changes or deletes a placed object.                                                   |
| `Hide(GameObject)`, `Unhide(MapRemovalDefinition)`             | Hides or restores a vanilla object.                                                   |
| `AddArea`, `UpdateArea`, `RemoveArea(string id)`               | Manage new areas.                                                                     |
| `Objects`, `Removed`                                           | Everything loaded from all layout files.                                              |
| `TryGetInstance(string id, out GameObject?)`                   | The placed object, if it is currently in the world.                                   |
| `Reload()`                                                     | Re-reads all layout files.                                                            |
| `OnConsumed`, `MarkConsumed(string id)`                        | Event when the game uses up a placed object here; mark one used up elsewhere (co-op). |

New things are saved into `map_editor.json`; objects from other files are saved back into their own file.

## How it works

- **Streaming:** objects within 1600 units of the player (or a co-op teammate) are placed; beyond 2400
  they are removed again. Memory follows what is around the player.
- **Copies** are taken from the world object's chunk, loading that chunk briefly if needed. The chunk's
  assets stay loaded while a copy is nearby and are released 30 seconds after the last one goes.
- **Saving:** a copy gets its own save ids, so a chest's contents or a lock are saved like the original's.
  Something the game destroys (a sprung trap, a taken pickup) stays gone in that save.
- **Navmesh:** objects with colliders and blockers update the navmesh, so NPCs walk around them.
  Blockers also stop the player.
- **Hiding** is applied every time the object's scene loads.

## Gotchas

- Hiding gameplay objects (chests, pickups, NPC spawners) is allowed but can make quests unfinishable.
- In co-op every machine needs the same layout files (`ModContentAccess.GetContentHash()` compares them).
- After a game update, regenerate the catalogs: `python Python/envPrefabCatalog.py` (prefabs) and
  `python Python/scenePropCatalog.py` (world objects, from the AssetRipper export).
- Tests: `tests/scenarios/map_editor.scn` and `tests/scenarios/map_area.scn`.
