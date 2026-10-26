# Routines

A **routine** is the set of waypoints an NPC walks between and waits at. It makes an NPC patrol or loiter
instead of standing still. You can give your own [lazy actors](./spawning.md) a routine, replace or edit a
vanilla NPC's routine with a JSON file, and give an NPC its own routine behaviour tree.

Entry points (static): `RoutineSystem`, `RoutineGraphAccess` (namespace `Drova_Modding_API.Systems.Routines`),
`RoutineOverrideStore`, `BehaviourTreeStore` (namespace `Drova_Modding_API.Systems.Routines.Store`).

## Quick example

For an NPC built with [`NpcCreator`](./spawning.md), add a `RoutineModule`:

```csharp
using Drova_Modding_API.Systems.Spawning;
using Drova_Modding_API.Systems.Spawning.Modules;
using UnityEngine;

RoutineModule routine = new RoutineModule();
routine.With(new Vector2(10, 20), new Vector2(14, 20));

new NpcCreator("Patroller", new Vector2(10, 20))
    .WithModule(routine)
    .CreateLazy(saveToLazyActorStore: true);
```

For a lazy actor you built yourself, call `RoutineSystem` directly:

```csharp
RoutineSystem.CreateRoutinePlace(lazyActor, entityInfo, new Vector2(10, 20), new Vector2(14, 20));
```

## Change a vanilla NPC's routine (JSON)

Put a `.json` file into `Mods/Modding_API/Routines/` (next to the API's DLL), one file per NPC. The file
name does not matter. Only `EntityGuid` is required.

```json
{
  "EntityGuid": "3f2a9c1e-0000-0000-0000-000000000000",
  "ActorGuid": null,
  "Name": "Rufus the guard",
  "GraphId": null,
  "ReplaceExisting": false,
  "Places": [
    { "Points": [ "-1350.5;-4430", "-1340;-4430" ] }
  ],
  "PlaceEdits": [
    {
      "PlaceKey": "9b1d4c2a-0000-0000-0000-000000000000",
      "Disabled": false,
      "Points": [
        { "Index": 0, "Position": "-1352;-4428" },
        { "Index": 2, "Removed": true }
      ],
      "AddedPoints": [ "-1345;-4431" ]
    }
  ]
}
```

| Field                         | Meaning                                                                                                                                                     |
|-------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `EntityGuid`                  | Required. GUID of the NPC's `EntityInfo`. Applies to every NPC sharing it.                                                                                  |
| `ActorGuid`                   | Limit the override to this one NPC. Generic NPCs (e.g. bandits) share one `EntityInfo`.                                                                     |
| `Name`                        | Your own note. Not used by the game.                                                                                                                        |
| `GraphId`                     | Name of a routine graph in `Mods/Modding_API/BehaviourTrees/` (without `.json`). Omit for the game's own routine.                                           |
| `ReplaceExisting`             | `true` switches the NPC's own routine places off. Use it together with `Places`.                                                                            |
| `Places`                      | New routine places. Each has `Points`: positions the NPC walks to and waits at.                                                                             |
| `Places[].Gate`, `GateTarget` | Optional door: an NPC in another region walks to `Gate`, fades out and appears at `GateTarget`. Give the place on the other side its own gate to come back. |
| `PlaceEdits`                  | Changes to a routine place the game ships, found by `PlaceKey`. Time of day, conditions and point types stay as they are.                                   |
| `PlaceEdits[].Name`           | Your own note.                                                                                                                                              |
| `PlaceEdits[].Disabled`       | `true` switches that place off.                                                                                                                             |
| `PlaceEdits[].Points`         | Changes to single points by `Index` (0 is the first). `Position` moves it, `"Removed": true` removes it.                                                    |
| `PlaceEdits[].AddedPoints`    | Extra wait points appended to the place.                                                                                                                    |

Positions are written `"x;y"`, e.g. `"-1350.5;-4430"`. A decimal comma also works. GUIDs are case-insensitive.

**Finding the GUIDs:** in a `Debug` build with cheat mode on, select the NPC and press **Edit Routine**.
The window shows the Entity GUID, Actor GUID, every place's `PlaceKey` and its numbered points. It can also
write the whole file for you: change what you want and press **Save**.

**Errors** never stop the game. The MelonLoader log (`MelonLoader/Latest.log`) gets a line starting with
`[Routines]` naming the file and the problem (unknown field, bad position, missing `PlaceKey`, `Index` out of
range, `GraphId` without a file, two files for one NPC). A broken file is skipped; the others still apply.

Files are read when the main menu loads. After editing a file by hand, go back to the main menu and load the save.

## Overrides from code

```csharp
using Drova_Modding_API.Systems.Routines.Store;

RoutineOverrideStore.Save(new RoutineOverride
{
    EntityGuid = entityInfo.GUID,
    ActorGuid = actor.GetGuidComponent().GuidString,
    ReplaceExisting = true,
    Places = [new RoutinePlaceOverride { Points = [new Vector2(10, 20), new Vector2(14, 20)] }]
});
```

`Save` writes the file and applies `GraphId` and `PlaceEdits` right away. New `Places` and
`ReplaceExisting` apply on the next world load.

## Own routine graphs

Every routine place runs one shared behaviour tree. To give an NPC its own, save a tree to the
`BehaviourTreeStore` and assign it to the actor's GUID (this NPC) or its `EntityInfo` GUID (every NPC sharing
it). An actor assignment wins over an entity assignment.

```csharp
using Drova_Modding_API.Systems.Routines;
using Drova_Modding_API.Systems.Routines.Store;

BehaviourTreeStore.SaveBehaviourTree("MyPatrol", myTree);
RoutineGraphAccess.Assign(actor.GetGuidComponent().GuidString, "MyPatrol");
RoutineGraphAccess.RefreshRunningRoutines();   // apply to NPCs already in a routine
```

`Assign` lasts until the game closes. For a permanent assignment set `GraphId` in an override, or use
`RoutineModule.WithGraph` for a spawned NPC.

To change the shared graph for everyone, edit it and save it under its own asset name. The stored version
replaces the game's whenever the main menu loads.

## In-game editor

In a `Debug` build with cheat mode on, select an NPC and press **Edit Routine**. From there you can:

- open the shared routine or group graph in the graph editor (changes affect every NPC),
- copy the shared graph into an own graph for this NPC (or every NPC with its `EntityInfo`),
- move, remove and add points of existing places, or switch a place off,
- pick points for a new place by typing them or clicking in the world.

The graph editor is the [dialogue editor](./dialogues.md) working on a behaviour tree. Saving restarts the
routines that use the graph, so changes show at once.

## API reference

### `RoutineSystem`

| Member                                                                                                          | Description                                                                                                                                                      |
|-----------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DefaultRoutinePlace? CreateRoutinePlace(LazyActor? lazyActor, EntityInfo entityInfo, params Vector2[] points)` | Create a routine with these wait points. `lazyActor` may be null; the actor is then found through `entityInfo`. Returns `null` before the AI-logic scene exists. |
| `TeleportRoutinePoint AddGatePoint(BaseRoutinePlace place, Vector2 gate, Vector2 target)`                       | Add a door point to a place (what `Gate`/`GateTarget` do).                                                                                                       |
| `GameObject? RoutineRoot`                                                                                       | Parent of API-created routines, set when `Scene_AILogic` loads.                                                                                                  |

### `RoutineGraphAccess`

| Member                                                        | Description                                                                             |
|---------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| `void Assign(string guid, string? treeId)`                    | Run a stored tree for an actor or entity GUID until the game closes. `null` removes it. |
| `bool TryGetAssigned(string guid, out string treeId)`         | The tree assigned to a GUID.                                                            |
| `void Invalidate(string treeId)`                              | Reload a stored tree and restart the routines running it.                               |
| `void RefreshRunningRoutines(params string[] changedTreeIds)` | Restart routines whose tree or assignment changed.                                      |
| `BehaviourTree? DefaultRoutineGraph` / `DefaultGroupGraph`    | The shared trees.                                                                       |

### `BehaviourTreeStore`

| Member                                                              | Description                                                      |
|---------------------------------------------------------------------|------------------------------------------------------------------|
| `bool SaveBehaviourTree(string treeId, BehaviourTree tree)`         | Write to `Mods/Modding_API/BehaviourTrees/<treeId>.json`.        |
| `bool TryLoadBehaviourTree(string treeId, out BehaviourTree? tree)` | Create a new tree from the stored version.                       |
| `bool TryPatchInMemoryBehaviourTree(string treeId)`                 | Overwrite the live asset named `treeId` with the stored version. |
| `IEnumerable<string> GetSavedIds()`                                 | Every stored id.                                                 |

### `RoutineOverrideStore`

| Member                                                                                                                 | Description                                                                  |
|------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------|
| `bool Save(RoutineOverride routineOverride)`                                                                           | Write the override and apply its graph and place edits.                      |
| `bool TryGet(string key, out RoutineOverride? routineOverride)`                                                        | A copy of the override for an actor or entity GUID. Change it and `Save` it. |
| `void Delete(string key)`                                                                                              | Remove the override, its file and its graph assignment.                      |
| `List<BaseRoutinePlace> FindExistingPlaces(string? actorGuid, string entityGuid)`                                      | The places the game ships for an NPC.                                        |
| `IReadOnlyList<RoutinePoint?> GetAuthoredPoints(BaseRoutinePlace place, out IReadOnlyList<Vector2> authoredPositions)` | A place's points in the order `PlaceEdits` indexes them.                     |
| `bool IsMovable(RoutinePoint point)`                                                                                   | False for interaction and door points.                                       |

## Gotchas

- **Interaction and door points cannot be moved** (they stand at a bed, work spot or door). They can be removed.
- **Entity overrides are shared.** All NPCs with one `EntityInfo` get them; set `ActorGuid` for just one.
- **Shared places stay on.** NPCs with one `EntityInfo` may share routine places. `ReplaceExisting` with an
  `ActorGuid` does not switch those off (it would affect every NPC using them); the log says so. Use
  `PlaceEdits` or `Places` instead.
- **Group places always run the shared group graph.** Own graphs only apply to normal routine places.
- **Restarting re-enters the place.** After `RefreshRunningRoutines` an NPC continues at the point it had
  reached, not at the first one.
