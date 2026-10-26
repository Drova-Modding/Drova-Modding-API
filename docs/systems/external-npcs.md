# External NPCs (JSON & Wizard)

**What it does:** place and configure NPCs **without writing C#**. Ship a `*.json` file and the API
spawns each enabled NPC in it when the game world loads. An in-game **wizard** builds these files for you,
and mods can add their own wizard options with `IExternalNpcModule`.

## The placement folder

```
…/Drova - Forsaken Kin/Mods/Modding_API/NpcPlacement/
  my_mod_npcs.json        ← your mod's file (you ship this)
  wizard_placements.json  ← written by the in-game wizard
```

Every `*.json` file here is read on its own. The wizard only ever writes `wizard_placements.json`.

## Option A: the in-game wizard (recommended)

1. Enable cheat mode and open the terminal (see [Cheat Menu](./cheat-menu.md)).
2. Run `npc_wizard`.
3. Set id, name and position (defaults to where the player stands), then configure the modules.
4. Click **Save Definition** or **Save + Spawn**.

## Option B: write the JSON yourself

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

| Field                     | Meaning                                                                                                             |
|---------------------------|---------------------------------------------------------------------------------------------------------------------|
| `id`                      | Unique, stable id. Used to find, edit and despawn the NPC, and to keep its save state.                              |
| `name`                    | Display name.                                                                                                       |
| `positionX` / `positionY` | World spawn position.                                                                                               |
| `moduleConfig`            | Module key → payload. The payload is JSON **written as a string**. Missing modules get defaults.                    |
| `enabled`                 | `false` skips the NPC.                                                                                              |
| `conditions`              | Optional list of `{ "GVarList", "Var", "Value" }`. The NPC is only there while all hold. See [Quests](./quests.md). |

### Module keys

| Key                   | What it sets                                                       |
|-----------------------|--------------------------------------------------------------------|
| `equipment`           | Items, e.g. `{"ItemReadableIds":["weapon_sword_longsword"]}`       |
| `cosmetics`           | Looks, e.g. `{"CosmeticReadableIds":["hair_short_spiky"]}`         |
| `health`              | Health                                                             |
| `talents`             | Talents                                                            |
| `alignment_container` | Faction / alignment                                                |
| `entityInfo`          | Name and identity info                                             |
| `dialogue`            | Dialogue tree                                                      |
| `teacher`             | Makes the NPC a trainer                                            |
| `trader`              | Makes the NPC a merchant                                           |
| `routine`             | Daily routine                                                      |

The easiest way to get correct payloads: build the NPC once in the wizard and copy the entry from
`wizard_placements.json`.

## Add your own wizard module

Implement `IExternalNpcModule` and register it during mod init. It then shows up in the wizard and
is read from JSON under its `Key`:

```csharp
using Drova_Modding_API.Systems.Spawning;
using System.Text.Json;
using UnityEngine;

public sealed class MyTagModule : IExternalNpcModule
{
    public string Key => "myTag";
    public string DisplayName => "My Tag";

    public string CreateDefaultPayload() => JsonSerializer.Serialize(new State());

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
        creator.WithModule(new MyRenameModule(state.Tag));
    }

    private static State Parse(string? payload)
        => string.IsNullOrWhiteSpace(payload) ? new State() : JsonSerializer.Deserialize<State>(payload) ?? new State();

    private sealed class State { public string Tag { get; set; } = "Guard"; }
}

ExternalNpcModuleRegistry.Register(new MyTagModule());
```

`MyRenameModule` is any `INpcModule` (see [Spawning](./spawning.md)).

## Code API

```csharp
ExternalNpcPlacementSystem.ExternalNpcDefinition def = ExternalNpcPlacementSystem.CreateDefaultDefinition();
def.Id = "guard_01";
def.Name = "Town Guard";
ExternalNpcPlacementSystem.UpsertDefinition(def);
ExternalNpcPlacementSystem.SpawnDefinition(def);

ExternalNpcPlacementSystem.DespawnDefinition("guard_01");
```

### `ExternalNpcPlacementSystem` (static)

| Member                                                                     | Description                                                                 |
|----------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| `GetNpcPlacementFolderPath()` / `GetWizardFilePath()`                      | Folder path / path of `wizard_placements.json`.                             |
| `SpawnDefinition(def, skipIfAlreadySpawned = true, requireEnabled = true)` | Spawn one definition.                                                       |
| `TryGetDefinition(id, out def)`                                            | Find a definition in any file.                                              |
| `ReadAllDefinitions()` / `ReadDefinitionsFromFile(path)`                   | Read definitions.                                                           |
| `UpsertDefinition(def)`                                                    | Save into the file that already has this id, else `wizard_placements.json`. |
| `UpsertDefinitionToModFile(def, modFileName, out savedFileName)`           | Save into a named mod file.                                                 |
| `DespawnDefinition(id)`                                                    | Remove the spawned NPC and its save entries.                                |
| `CreateDefaultDefinition()`                                                | A new definition with default payloads for every module.                    |
| `IsDefinitionSpawned(id)` / `TryGetSpawnedActor(id, out Actor)`            | Is it spawned / get the actor.                                              |

### `IExternalNpcModule`

| Member                                             | Description                                            |
|----------------------------------------------------|--------------------------------------------------------|
| `string Key`                                       | Key in `moduleConfig`.                                 |
| `string DisplayName`                               | Name shown in the wizard.                              |
| `string CreateDefaultPayload()`                    | Default payload.                                       |
| `string DrawWizardUiAndSerialize(string payload)`  | Draw wizard controls, return the new payload.          |
| `void ApplyToCreator(NpcCreator, string? payload)` | Apply the payload to the NPC being built.              |
| `void OnWizardUpdate()`                            | Optional, called every frame while the wizard is open. |

`ExternalNpcModuleRegistry.Register(module)` adds a module; a duplicate key is ignored.

## Gotchas

- **Don't rename an `id`.** The NPC's save state (death, health, inventory) is tied to it; a new id
  is a fresh NPC.
- **Register custom modules during mod init**, before the world loads, or startup spawns won't
  use them.
- **Creatures** (monsters, animals) go in `CreaturePlacement/*.json` instead. See
  [Quests](./quests.md).
