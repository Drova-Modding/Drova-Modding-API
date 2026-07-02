# Drova Modding API — Documentation

The **Drova Modding API** is an unofficial, community-maintained convenience layer for
modding [Drova – Forsaken Kin](https://www.nexusmods.com/drovaforsakenkin). It runs on
[MelonLoader](https://melonwiki.xyz) and sits on top of the game's IL2CPP types, so you can
build menus, spawn NPCs, write dialogue, react to game events, and persist data without
fighting the engine internals yourself.

> **New here?** Start with **[Getting Started](./getting-started.md)** to set up a mod
> project, then skim **[Core Concepts](./concepts.md)** to understand the mod lifecycle and
> where files live. After that, jump to whichever system you need below.

## How these docs are organized

Every system page follows the same shape so you can scan quickly:

1. **What it does** — one or two sentences and when to use it.
2. **Quick example** — a minimal, copy-paste `MelonMod` snippet.
3. **How do I…?** — short recipes for common tasks.
4. **API reference** — a table of the public methods/properties.
5. **Notes & gotchas** — lifecycle timing, IL2CPP gotchas, persistence behavior.

All code samples are C# targeting the IL2CPP build of the game. Unless noted, calls happen
from your `MelonMod` subclass.

## Foundations

| Page                                    | What you'll learn                                                                   |
|-----------------------------------------|-------------------------------------------------------------------------------------|
| [Getting Started](./getting-started.md) | Install MelonLoader, set up your `.csproj`, write your first mod, dev key bindings. |
| [Core Concepts](./concepts.md)          | The mod/scene lifecycle, IL2CPP gotchas, where the API reads/writes files.          |

## Systems

### UI & configuration

| Page                                      | Summary                                                                                              |
|-------------------------------------------|------------------------------------------------------------------------------------------------------|
| [Options Menu](./systems/options-menu.md) | Add your own panel to the game's options window and build sliders, switches, dropdowns, and buttons. |
| [Localization](./systems/localization.md) | Ship translation files for your mod and read localized strings.                                      |
| [Config](./systems/config.md)             | Read back the persisted values your option controls saved.                                           |
| [Cheat Menu](./systems/cheat-menu.md)     | Register custom terminal/console commands.                                                           |

### Game access

| Page                                      | Summary                                                                                      |
|-------------------------------------------|----------------------------------------------------------------------------------------------|
| [Game Access](./systems/game-access.md)   | Reach the game's managers/handlers (`ProviderAccess`) and the player actor (`PlayerAccess`). |
| [Input](./systems/input.md)               | Register input actions, read button/axis state, and rebind keys.                             |
| [Addressables](./systems/addressables.md) | Load game assets (creatures, items, UI prefabs, entity infos) by reference.                  |

### NPCs & spawning

| Page                                                        | Summary                                                                          |
|-------------------------------------------------------------|----------------------------------------------------------------------------------|
| [Spawning NPCs](./systems/spawning.md)                      | Compose NPCs from modules with `NpcCreator`, spawn bandits, and use lazy actors. |
| [External NPCs (JSON & Wizard)](./systems/external-npcs.md) | Place NPCs from JSON files or the in-game wizard — no code required.             |

### Persistence

| Page                                  | Summary                                                                    |
|---------------------------------------|----------------------------------------------------------------------------|
| [Save Game](./systems/save-game.md)   | Store custom JSON blobs inside Drova save files with typed stores.         |
| [Relocators](./systems/relocators.md) | Serialize game objects (dialogue trees, items, gvars) into your save data. |

### World & content

| Page                                         | Summary                                                                     |
|----------------------------------------------|-----------------------------------------------------------------------------|
| [Dialogues](./systems/dialogues.md)          | Build dialogue graphs in code and edit existing ones in the in-game editor. |
| [Audio](./systems/audio.md)                  | Provide spoken dialogue audio from loose files or AssetBundles.             |
| [World Events](./systems/world-events.md)    | Register global and region-triggered world events.                          |
| [Areas & Regions](./systems/area-region.md)  | Track which region the player is in and react to entering/leaving.          |
| [Routines](./systems/routines.md)            | Give NPCs waypoint routines.                                                |
| [Global Variables](./systems/global-vars.md) | React to gvar changes and register custom global variables.                 |
| [Talents](./systems/talents.md)              | Register custom talents into the talent graph.                              |

## Contributing to the docs

These pages live in `/docs` as plain Markdown — no build step. Edit a file, open a PR.
Keep examples runnable and verified against the current source. If you add a new public
system, add a page here and link it from this index.
