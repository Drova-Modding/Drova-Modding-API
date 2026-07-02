# Game Access

**What it does:** gives you cached, null-safe access to Drova's internal managers, databases,
and the player. Two static facades cover most needs:

- `Drova_Modding_API.Access.ProviderAccess` — the game's resource provider, databases (items,
  recipes, gvars, status effects, …), and every `…GameHandler` (entities, dialogue, save game,
  scene, weather, daytime, quests, …).
- `Drova_Modding_API.Access.PlayerAccess` — the player `Actor`, equipment slots, and a ready
  event so you don't grab the player before it exists.

Use these instead of `FindObjectOfType` or reflection.

## Quick example

Wait for the player to be ready, then read something from it:

```csharp
using Drova_Modding_API.Access;
using MelonLoader;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        // Fires every load, once the player actor exists and is initialized.
        PlayerAccess.OnPlayerFound += player =>
        {
            LoggerInstance.Msg($"Player ready: {player.name}");
        };
    }
}
```

The API starts the "wait for player" coroutine for you on a gameplay load and after a save loads,
so subscribing to `OnPlayerFound` is usually all you need.

## How do I…?

### Get the player safely

```csharp
if (PlayerAccess.TryGetPlayer(out Actor player))
{
    // player is non-null here
}

// or, if you want to handle null yourself:
Actor? player = PlayerAccess.GetPlayer(); // null in menus / before ready
```

> Don't call these the instant gameplay loads — the player isn't initialized yet. Prefer
> `OnPlayerFound`, or call `PlayerAccess.StartWaitForPlayerCoroutine()` and react in the event.

### Read the player's equipped weapon slots and stats

```csharp
ActorEquipSlot primary  = PlayerAccess.GetPrimarySlot();
ActorEquipSlot offhand  = PlayerAccess.GetSecondarySlot(); // same as primary for 2-handers
ActorEquipSlot bowAmmo  = PlayerAccess.GetBowSlot();
ActorEquipSlot slingAmmo = PlayerAccess.GetSlingshotSlot();

PlayerAttributeStats stats = PlayerAccess.GetPlayerAttributeStats();
```

### Reach a game handler

`ProviderAccess` exposes handlers either as `Get…()` methods (returning the handler or `null`)
or `TryGet…(out …)` methods. Always null-check:

```csharp
// Entities: the player object and spawn/despawn events.
EntityGameHandler? entities = ProviderAccess.GetEntityGameHandler();

// Scene transitions, fading, "is the player teleporting".
SceneGameHandler? scene = ProviderAccess.GetSceneGameHandler();

// Save game data and events.
SavegameGameHandler? save = ProviderAccess.GetSaveGameHandler();

// Time scale / game state.
if (ProviderAccess.TryGetGameStateGameHandler(out var gameState) && gameState != null)
{
    // pause/slow-mo, etc.
}

// Day/night and weather.
ProviderAccess.TryGetDaytimeGameHandler(out var daytime);
ProviderAccess.TryGetWeatherGameHandler(out var weather);
```

### Look something up in a database

```csharp
// Items, recipes, status effects, global variables.
var itemDb   = ProviderAccess.ItemDatabase;
var recipeDb = ProviderAccess.RecipeDatabase;
var gvarDb   = ProviderAccess.GVarDatabase;
var statusDb = ProviderAccess.StatusEffectDatabase;

// The big aggregate databases:
GameDatabase gameDb = ProviderAccess.GetGameDatabase();
AIDatabase   aiDb   = ProviderAccess.GetAIDatabase(); // behaviour trees / FSM graphs
```

### Get the GameManager

```csharp
if (ProviderAccess.TryGetGameManager(out GameManager gm))
{
    // gm is cached after the first successful lookup
}
```

## API reference

### `PlayerAccess` (static)

| Member                                                   | Description                                                                                                          |
|----------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `event Action<Actor>? OnPlayerFound`                     | Raised once the player actor exists and is initialized. Fires on every load.                                         |
| `Actor? GetPlayer()`                                     | The player actor, or `null` in menus / before ready.                                                                 |
| `bool TryGetPlayer(out Actor player)`                    | Null-safe player getter.                                                                                             |
| `void StartWaitForPlayerCoroutine()`                     | Begin polling for the player; fires `OnPlayerFound` when ready. (The API starts this for you on gameplay/save load.) |
| `ActorEquipSlot GetPrimarySlot()` / `GetSecondarySlot()` | Primary / secondary weapon slot.                                                                                     |
| `ActorEquipSlot GetBowSlot()` / `GetSlingshotSlot()`     | Bow / slingshot ammo slot.                                                                                           |
| `PlayerAttributeStats GetPlayerAttributeStats()`         | The player's attribute stat container.                                                                               |

### `ProviderAccess` (static) — selected members

| Member                                                                             | Description                                          |
|------------------------------------------------------------------------------------|------------------------------------------------------|
| `DrovaResourceProvider GetDrovaResourceProvider()`                                 | The main resource provider (cached).                 |
| `GameDatabase GetGameDatabase()`                                                   | Aggregate game database.                             |
| `SubDatabase_Item ItemDatabase` / `SubDatabase_Recipe RecipeDatabase`              | Item / recipe databases.                             |
| `SubDatabase_GVars GVarDatabase` / `SubDatabase_StatusEffect StatusEffectDatabase` | Global-var / status-effect databases.                |
| `AIDatabase GetAIDatabase()`                                                       | Behaviour trees / AI graphs.                         |
| `bool TryGetGameManager(out GameManager)`                                          | The `GameManager` (cached).                          |
| `EntityGameHandler? GetEntityGameHandler()`                                        | Player object + entity spawn/despawn events.         |
| `DialogueSystemGameHandler? GetDialogueSystemGameHandler()`                        | Active dialogues / dialogue system.                  |
| `SavegameGameHandler? GetSaveGameHandler()`                                        | Save game data and events.                           |
| `SceneGameHandler? GetSceneGameHandler()`                                          | Scene transitions, fading, teleport state.           |
| `ConfigGameHandler? GetConfigGameHandler()`                                        | Config/options bridge (used by the options builder). |
| `CheatGameHandler? GetCheatGameHandler()`                                          | Cheat system (spawnables, cheat mode).               |
| `AudioGameHandler? GetAudioGameHandler()`                                          | SFX playback for actors.                             |
| `bool TryGetWeatherGameHandler(out …)` / `TryGetDaytimeGameHandler(out …)`         | Weather / day-night handlers.                        |
| `bool TryGetGameStateGameHandler(out …)`                                           | Time scale / game state.                             |
| `bool TryGetPlayerMetaDataGameHandler(out …)`                                      | Journal, map collection, crafting.                   |
| `bool TryGetLazyManager(out …)`                                                    | Lazy-loading manager.                                |
| `AstarPath GetAstarPath()`                                                         | Pathfinding (`AstarPath.active`).                    |

> `ProviderAccess` has many more `Get…GameHandler()` / `TryGet…` methods than listed here — one
> per game subsystem (achievements, analytics, camera, GUI, map, object pooler, quests, tutorial,
> crash reporting, and several lazy/factory managers). They all follow the same return-or-`null`
> pattern. Browse `Access/ProviderAccess.cs` for the full set.

## Notes & gotchas

- **Everything can be `null` during bootstrap.** Handlers resolve on demand; early in a startup 
  they may not exist yet. Null-check or use the `TryGet…` variants.
- **Prefer `OnPlayerFound` over polling.** The player isn't ready the moment a gameplay scene
  loads; the event fires only after `Actor._isInitialized`.
- **Returned objects are live game types** (`Il2CppDrova.*`). Use `TryCast<T>()`/`Cast<T>()` for
  casts and expect IL2CPP semantics (see [Core Concepts](../concepts.md)).
- `GetDrovaResourceProvider` and `TryGetGameManager` cache their result after the first hit.
