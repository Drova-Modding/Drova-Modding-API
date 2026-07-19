# Difficulty

**What it does:** reads the game mode (difficulty) of the currently loaded savegame.
Drova has no numeric difficulty multipliers — the game only distinguishes the insane
mode family from the non-insane modes and picks data-authored insane variants
(spawn data, AI graphs, stats) based on that single distinction.

Entry point: `Drova_Modding_API.Access.DifficultyAccess` (static).

## Quick example

```csharp
using Drova_Modding_API.Access;
using Il2CppDrova;

if (DifficultyAccess.TryGetGameMode(out GameMode mode))
{
    if (DifficultyAccess.IsInsaneMode(mode))
    {
        // Player runs an insane savegame.
    }
}
```

## How do I…?

### Check insane mode without caring about the exact mode

```csharp
if (DifficultyAccess.IsInsaneMode())
{
    // Insane_Classic, Insane_Immersive or Insane_Hardcore.
}
```

### Understand the game modes

`Il2CppDrova.GameMode` values map to the in-game selection:

| Value              | In-game name       |
|--------------------|--------------------|
| `Default`          | Classic            |
| `Immersive`        | Immersive          |
| `Hardcore`         | Hardcore           |
| `Story`            | Story              |
| `Insane_Classic`   | Insane (Classic)   |
| `Insane_Immersive` | Insane (Immersive) |
| `Insane_Hardcore`  | Insane (Hardcore)  |

## API reference

| Member                                       | Description                                                                                          |
|----------------------------------------------|------------------------------------------------------------------------------------------------------|
| `bool TryGetGameMode(out GameMode gameMode)` | Game mode of the loaded savegame. Returns `false` (and `GameMode.Default`) while bootstrapping.      |
| `bool IsInsaneMode()`                        | Whether the current savegame runs in an insane mode. `false` while bootstrapping.                    |

## Notes & gotchas

- **The mode lives in the savegame**, not in the global config. In the main menu (no savegame
  loaded) `TryGetGameMode` can fail, always check the return value.
- The game persists the mode via `SavegameDataExtensions.SetGameMode/GetGameMode` and fires
  `SavegameDataExtensions.GameModeChangeEvent` when the player changes it in the options.
- The internal hot-path check the game itself uses everywhere is
  `Il2CppDrova.GameModeAccess.IsInsaneMode()`, patch that if you need to intercept insane
  content selection at runtime.
