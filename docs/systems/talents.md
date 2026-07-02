# Talents

**What it does:** registers your own `TalentContainer` assets into the game's talent database and
talent graph, so custom talents show up (under a "Modded Talents" group) and can be granted to the
player or NPCs. It also exposes lookups over the existing talents.

Entry point: `Drova_Modding_API.Systems.Talents.TalentContainerDatabase` (static).

> The API drives this database's lifecycle for you: `InitializeDatabase()` runs at the main menu
> and `Init()` runs on a gameplay load. You mostly call `AddTalent(...)` and the lookup helpers.

## Quick example

Register a custom talent during startup:

```csharp
using Drova_Modding_API.Systems.Talents;
using Il2CppDrova.Talent;

public class Core : MelonLoader.MelonMod
{
    public override void OnInitializeMelon()
    {
        TalentContainer myTalent = BuildMyTalentContainer(); // your TalentContainer asset
        TalentContainerDatabase.AddTalent(myTalent);
    }
}
```

`AddTalent` registers the talent and queues it for insertion into the talent graph; the API adds it
to the graph as a `TalentNode` in the **"Modded Talents"** group when the graph initializes (at
gameplay start and when the player is found).

## How do I…?

### Grant a talent to a spawned NPC

Use the `TalentPresetModule` with [`NpcCreator`](./spawning.md). Readable talent IDs are in
`Drova_Modding_API.GlobalFields.TalentReadableIds`:

```csharp
using Drova_Modding_API.Systems.Spawning;
using Drova_Modding_API.Systems.Spawning.Modules;
using Drova_Modding_API.GlobalFields;

new NpcCreator("Swordsman", position)
    .WithModule(new TalentPresetModule().With(TalentReadableIds.Sword.Talent00))
    .CreateLazy(saveToLazyActorStore: true);
```

### Look up existing talents

```csharp
// By exact name:
if (TalentContainerDatabase.TryGetTalent("Talent_Sword_00", out var talent)) { /* … */ }

// Grouped by category prefix ("Talent", "TalentBhvr", "Test", "Other"):
Dictionary<string, List<TalentContainer>> grouped = TalentContainerDatabase.GetGroupedTalents();

// All talents in one category:
List<TalentContainer> swords = TalentContainerDatabase.GetTalentsByCategory("Talent");
```

## API reference

### `TalentContainerDatabase` (static)

| Member                                                          | Description                                                                                 |
|-----------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| `void AddTalent(TalentContainer talent)`                        | Register a custom talent and queue it for the talent graph.                                 |
| `bool TryGetTalent(string name, out TalentContainer?)`          | Look up a talent by exact name.                                                             |
| `Dictionary<string, List<TalentContainer>> GetGroupedTalents()` | All talents grouped by category prefix.                                                     |
| `List<TalentContainer> GetTalentsByCategory(string category)`   | Talents in a category (`Talent`, `TalentBhvr`, `Test`, `Other`).                            |
| `void InitializeDatabase()`                                     | Populate the cache from the game (called by the API at the main menu).                      |
| `void Init()`                                                   | Initialize the talent graph and insert queued talents (called by the API on gameplay load). |
| `void ClearDatabase()`                                          | Clear the cached database.                                                                  |

## Notes & gotchas

- **The database initializes at the main menu**; if you call a lookup before then, it logs a warning
  and returns empty. Register talents in `OnInitializeMelon` and they'll be inserted when the graph
  initializes.
- **Custom talents land in the "Modded Talents" group** in the talent graph and are deduplicated by
  GUID, so re-registration is safe.
- **Categories are derived from the talent name prefix** (`Talent_`, `TalentBhvr_`, `Test_`,
  otherwise `Other`).
- To actually give a talent to a character, use a `TalentPresetModule` on an NPC (see
  [Spawning](./spawning.md)) or apply it to the player via the game's talent APIs.
