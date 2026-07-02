# Routines

**What it does:** gives a [lazy actor](./spawning.md) a **routine** — a set of waypoints the NPC
moves between/waits at. This is what makes a spawned NPC patrol or loiter rather than stand
still.

Entry point: `Drova_Modding_API.Systems.Routines.RoutineSystem` (static).

> For NPCs created with [`NpcCreator`](./spawning.md), the easiest way to add a routine is the
> built-in `RoutineModule` (it calls into this system for you). Use `RoutineSystem` directly when
> you need full control or are working with a lazy actor you built yourself.

## Quick example

Give a lazy actor a two-point patrol:

```csharp
using Drova_Modding_API.Systems.Routines;
using UnityEngine;

// lazyActor + entityInfo come from your spawn (see Spawning).
RoutineSystem.CreateRoutinePlace(
    lazyActor,
    entityInfo,
    new Vector2(10, 20),   // point 0
    new Vector2(14, 20));  // point 1
```

Each `Vector2` becomes a wait point; the routine is parented under the API's routine root in the
AI-logic scene.

## How do I…?

### Add a routine via the NPC builder

```csharp
using Drova_Modding_API.Systems.Spawning;
using Drova_Modding_API.Systems.Spawning.Modules;
using UnityEngine;

var routine = new RoutineModule();
routine.With(new Vector2(10, 20), new Vector2(14, 20));

new NpcCreator("Patroller", new Vector2(10, 20))
    .WithModule(routine)
    .CreateLazy(saveToLazyActorStore: true);
```

The `RoutineModule` resolves the entity info and lazy actor from the spawn context and calls
`RoutineSystem.CreateRoutinePlace` under the hood.

## API reference

### `RoutineSystem` (static)

| Member                                                                                                          | Description                                                                                                              |
|-----------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| `DefaultRoutinePlace? CreateRoutinePlace(LazyActor? lazyActor, EntityInfo entityInfo, params Vector2[] points)` | Create a routine for `lazyActor` with the given wait points. Returns `null` if the actor or routine root is unavailable. |
| `GameObject? RoutineRoot { get; }`                                                                              | Parent object grouping API-created routines (set by the API on the AI-logic scene).                                      |

## Notes & gotchas

- **Needs a lazy actor and an `EntityInfo`.** `CreateRoutinePlace` warns and returns `null` for a
  null actor.
- **`RoutineRoot` is set on the AI-logic scene** (`Scene_AILogic`). Creating a routine before that
  scene exists returns `null` — spawn/route during gameplay.
- Prefer the `RoutineModule` when building NPCs with [`NpcCreator`](./spawning.md); reach for
  `RoutineSystem` directly only for custom lazy actors.
