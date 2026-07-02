# Global Variables

**What it does:** Drova stores a lot of world/quest states in **global variables** ("gvars").
This system lets your mod (a) **react** when a global bool changes via an event bus, (b) ship
**custom global variables** as JSON, and (c) inspect/edit gvars live with the **F6 inspector** in
debug builds.

Entry point: `Drova_Modding_API.Systems.GlobalVars.GvarBusSystem` (static).

## Quick example — react to a gvar change

```csharp
using Drova_Modding_API.Systems.GlobalVars;
using MelonLoader;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        GvarBusSystem.OnGBoolValueChanged += OnGBoolChanged;
    }

    private void OnGBoolChanged(GvarBusSystem.GvarChangeEvent e)
    {
        MelonLogger.Msg($"Global bool '{e.Name}' changed to {e.Value}");
    }
}
```

The bus fires whenever a tracked global **bool** variable changes — handy for triggering mod
behavior off quest/world flags without polling.

## How do I…?

### Read or write a gvar value

The gvar database is reached through [`ProviderAccess`](./game-access.md):

```csharp
using Drova_Modding_API.Access;

var gvarDb = ProviderAccess.GVarDatabase; // SubDatabase_GVars
// look up lists/vars by name/guid/id via the database, then read/set as needed
```

### Inspect and edit gvars live (debug)

In a `Debug` build of the API, press **F6** to open the **Global Variable inspector**. It lets you
browse the gvar lists, view current values, and edit them at runtime — invaluable for figuring out
which flag drives a piece of game behavior. The inspector also lets you create custom lists and
variables, which are persisted as JSON (see below).

### Ship custom global variables as JSON

Custom gvar definitions live in JSON files under the API's data folder:

```
…/Drova - Forsaken Kin/Mods/Modding_API/GlobalVars/<yourMod>.json
```

On the main-menu load, every `*.json` file there is read and its lists/variables are inserted into
the game's gvar database (supporting bool/int/float/string variables, and quest-var lists). The
in-game inspector writes definitions you create back into these files, so the simplest authoring
path is: create them in the F6 inspector, then ship the generated JSON with your mod.

## API reference

### `GvarBusSystem` (static)

| Member                                                | Description                                                  |
|-------------------------------------------------------|--------------------------------------------------------------|
| `Action<GvarChangeEvent>? OnGBoolValueChanged`        | Subscribe to be notified when a tracked global bool changes. |
| `struct GvarChangeEvent { bool Value; string Name; }` | The changed variable's new value and name.                   |

To read/write gvar values directly, use `ProviderAccess.GVarDatabase` and the game's
`SubDatabase_GVars` / `GVarList` / gvar types — see [Game Access](./game-access.md).

## Notes & gotchas

- **The bus currently surfaces global *bool* changes** (`OnGBoolValueChanged`). For other types,
  read through the gvar database directly.
- **Custom gvars load at the main menu** from `Modding_API/GlobalVars/*.json` — drop your file in
  before launching.
- **The F6 inspector is debug-only.** It's the easiest way to discover flag names and to author
  custom gvar JSON; release builds don't include it.
- Custom gvars are inserted into the live database, so other systems (dialogue conditions, quests)
  can reference them like any built-in gvar.
