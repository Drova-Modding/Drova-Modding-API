# External NPCs (JSON & Wizard)

**What it does:** lets you place and configure NPCs **without writing C#**. Every mod can ship a
`*.json` file describing NPCs; on game start the API scans the placement folder and spawns each
enabled definition as a [lazy actor](./spawning.md). There's also an in-game **wizard** for
building definitions visually, and an extensibility point (`IExternalNpcModule`) so mods can add
their own wizard-configurable options.

Entry points:
- `Drova_Modding_API.Systems.Spawning.ExternalNpcPlacementSystem` — scans/spawns/edits JSON.
- `Drova_Modding_API.Systems.Spawning.ExternalNpcModuleRegistry` + `IExternalNpcModule` — the
  module extensibility model.

## The placement folder

```
…/Drova - Forsaken Kin/Mods/Modding_API/NpcPlacement/
  my_mod_npcs.json        ← your mod's file (you ship this)
  another_mod.json        ← some other mod's file
  wizard_placements.json  ← owned by the in-game wizard
```

All `*.json` files in this folder are scanned on game start, and each file is independent. The
**wizard never touches mod-provided files** — it only writes `wizard_placements.json`.

## Option A — the in-game wizard (recommended)

1. Enable cheat mode and open the terminal (see [Cheat Menu](./cheat-menu.md)).
2. Run `npc_wizard`.
3. Configure the base data (id, name, position) and the modules in the window.
4. Click **Save Definition** or **Save + Spawn**.

New definitions default their spawn position to the player's current position. The wizard renders
one section per registered `IExternalNpcModule`, so any module a mod registers automatically
appears in the UI.

## Option B — author a JSON file directly

Create `NpcPlacement/my_mod_npcs.json`, set `enabled` to `true`, and the enabled entries spawn on
game start. The schema:

```json
{
  "version": 1,
  "npcs": [
    {
      "id": "my_first_custom_npc",
      "name": "Custom NPC",
      "positionX": 12.5,
      "positionY": -3.0,
      "moduleConfig": {
        "equipment": "{\"ItemReadableIds\":[\"weapon_sword_longsword\"]}",
        "cosmetics": "{\"CosmeticReadableIds\":[\"hair_short_spiky\"]}"
      },
      "enabled": true
    }
  ]
}
```

- `id` — a stable, unique identifier (used to dedupe and to edit later).
- `name` — the NPC display name.
- `positionX` / `positionY` — world spawn position.
- `moduleConfig` — a map of **module key → serialized payload string**. Each registered module
  reads its own key; the API fills in defaults for any module you omit.
- `enabled` — `false` skips this entry at startup.

### Module keys and payloads

The modules registered by default (and the JSON key each reads) are:

| Key                   | Module            | Payload shape (example)                          |
|-----------------------|-------------------|--------------------------------------------------|
| `equipment`           | Equipment items   | `{"ItemReadableIds":["weapon_sword_longsword"]}` |
| `cosmetics`           | Cosmetic items    | `{"CosmeticReadableIds":["hair_short_spiky"]}`   |
| `health`              | Custom health     | module-specific                                  |
| `talents`             | Talents           | module-specific                                  |
| `alignment_container` | Faction/alignment | module-specific                                  |
| `entityInfo`          | Entity info       | `{"assetGuid":""}`                               |
| `dialogue`            | Dialogue tree     | module-specific                                  |
| `teacher`             | Trainer           | module-specific                                  |
| `trader`              | Merchant          | module-specific                                  |
| `routine`             | Waypoint routine  | module-specific                                  |

> The payload is itself JSON, embedded as a **string** inside `moduleConfig`. The simplest way to
> get correct payloads is to build the NPC once in the wizard and copy the generated
> `wizard_placements.json` — or call `ExternalNpcPlacementSystem.CreateDefaultDefinition()` which
> fills in baseline payloads for every module.

## How do I…?

### Add my own wizard-configurable module

Implement `IExternalNpcModule` and register it once during mod init. It then shows up in the
wizard and is honored when reading JSON:

```csharp
using Drova_Modding_API.Systems.Spawning;
using System.Text.Json;
using UnityEngine; // GUILayout

public sealed class MyTagModule : IExternalNpcModule
{
    public string Key => "myTag";
    public string DisplayName => "My Tag";

    public string CreateDefaultPayload() => JsonSerializer.Serialize(new State { Tag = "Guard" });

    public string DrawWizardUiAndSerialize(string payload)
    {
        State state = Parse(payload);
        GUILayout.Label("Tag text");
        state.Tag = GUILayout.TextField(state.Tag);
        return JsonSerializer.Serialize(state);
    }

    public void ApplyToCreator(NpcCreator creator, string? payload)
    {
        State state = Parse(payload);
        creator.WithModule(new RenameWithTagModule(state.Tag));
    }

    private static State Parse(string? payload)
        => string.IsNullOrWhiteSpace(payload)
            ? new State()
            : JsonSerializer.Deserialize<State>(payload) ?? new State();

    private sealed class State { public string Tag { get; set; } = "Guard"; }

    private sealed class RenameWithTagModule(string tag) : INpcModule
    {
        public int Priority => 1000;
        public void Apply(ModuleContext context)
        {
            var npc = context.GetComponent<Il2CppDrova.Actor>();
            if (npc != null && !string.IsNullOrWhiteSpace(tag))
                npc.gameObject.name = $"{npc.gameObject.name} [{tag}]";
        }
    }
}

// Register once during mod initialization:
ExternalNpcModuleRegistry.Register(new MyTagModule());
```

### Spawn / edit definitions from code

```csharp
// Find the placement folder (e.g. to ship a file alongside your mod):
string folder = ExternalNpcPlacementSystem.GetNpcPlacementFolderPath();

// Read everything:
List<ExternalNpcPlacementSystem.ExternalNpcDefinition> all =
    ExternalNpcPlacementSystem.ReadAllDefinitions();

// Spawn one definition immediately:
ExternalNpcPlacementSystem.TryGetDefinition("my_first_custom_npc", out var def);
ExternalNpcPlacementSystem.SpawnDefinition(def, skipIfAlreadySpawned: true, requireEnabled: false);

// Create + persist a new one into wizard_placements.json:
var newDef = ExternalNpcPlacementSystem.CreateDefaultDefinition();
newDef.Id = "guard_01";
newDef.Name = "Town Guard";
ExternalNpcPlacementSystem.UpsertDefinition(newDef);
```

### Remove a spawned NPC

```csharp
ExternalNpcPlacementSystem.DespawnDefinition("guard_01"); // destroys it and clears save entries
```

## API reference

### `ExternalNpcPlacementSystem` (static) — selected members

| Member                                                                                         | Description                                                                                             |
|------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| `string GetNpcPlacementFolderPath()`                                                           | Absolute path of the `NpcPlacement` folder.                                                             |
| `string GetWizardFilePath()`                                                                   | Path of `wizard_placements.json`.                                                                       |
| `void SpawnConfiguredNpcs()`                                                                   | Scan all files and spawn enabled, not-yet-spawned definitions. (Called by the API on actor-scene load.) |
| `bool SpawnDefinition(ExternalNpcDefinition?, bool skipIfAlreadySpawned, bool requireEnabled)` | Spawn one definition as a lazy actor.                                                                   |
| `bool TryGetDefinition(string id, out ExternalNpcDefinition?)`                                 | Find a definition across all files.                                                                     |
| `List<ExternalNpcDefinition> ReadAllDefinitions()` / `ReadDefinitionsFromFile(path)`           | Read definitions.                                                                                       |
| `bool UpsertDefinition(ExternalNpcDefinition?)`                                                | Insert/update in `wizard_placements.json` (or the file that already owns the id).                       |
| `bool UpsertDefinitionToModFile(def, modFileName, out savedFileName)`                          | Insert/update in a named mod file.                                                                      |
| `bool DespawnDefinition(string id)`                                                            | Destroy spawned instance(s) and clear save entries.                                                     |
| `ExternalNpcDefinition CreateDefaultDefinition()`                                              | A template definition with baseline module payloads.                                                    |
| `bool IsDefinitionSpawned(string id)` / `bool TryGetSpawnedActor(string id, out Actor?)`       | Query spawned state.                                                                                    |

`ExternalNpcDefinition`: `Id`, `Name`, `PositionX`, `PositionY`, `Dictionary<string,string>
ModuleConfig`, `Enabled`. `ExternalNpcConfigFile`: `Version`, `List<ExternalNpcDefinition> Npcs`.

### `ExternalNpcModuleRegistry` (static)

| Member                                                 | Description                                     |
|--------------------------------------------------------|-------------------------------------------------|
| `IReadOnlyList<IExternalNpcModule> Modules`            | All registered modules, in wizard render order. |
| `void Register(IExternalNpcModule)`                    | Register a module (duplicate keys ignored).     |
| `void ApplyModules(NpcCreator, ExternalNpcDefinition)` | Apply all module payloads to a creator.         |
| `void EnsureDefaults(ExternalNpcDefinition)`           | Fill missing module payloads with defaults.     |

### `IExternalNpcModule`

| Member                                             | Description                                               |
|----------------------------------------------------|-----------------------------------------------------------|
| `string Key { get; }`                              | Unique key used in `moduleConfig`.                        |
| `string DisplayName { get; }`                      | Name shown in the wizard.                                 |
| `string CreateDefaultPayload()`                    | Default serialized payload.                               |
| `string DrawWizardUiAndSerialize(string payload)`  | Draw wizard controls; return updated payload.             |
| `void ApplyToCreator(NpcCreator, string? payload)` | Apply this module's config to a creator.                  |
| `void OnWizardUpdate()`                            | Optional per-frame hook while the wizard section is open. |

## Notes & gotchas

- **JSON-defined NPCs become lazy actors** managed by the normal save pipeline — they persist.
- **Mod files are read-only to the wizard.** Wizard edits land in `wizard_placements.json`; ship
  your own file separately.
- **`id` must be unique and stable** — it's how the system dedupes spawns and finds definitions to
  edit or despawn.
- **Payloads are strings of JSON inside `moduleConfig`.** Easiest path to correct payloads: build
  in the wizard and copy the output, or start from `CreateDefaultDefinition()`.
- Register custom `IExternalNpcModule`s **before** the placement scan (during mod init) so they're
  applied to startup spawns and appear in the wizard.
