# Drova Modding API — Documentation

The **Drova Modding API** is an unofficial, community-maintained convenience layer for
modding [Drova – Forsaken Kin](https://www.nexusmods.com/drovaforsakenkin). It runs on
[MelonLoader](https://melonwiki.xyz) and sits on top of the game's IL2CPP types, so you can
build menus, spawn NPCs, write dialogue, react to game events, and save data.

> **New here?** Read **[Getting Started](./getting-started.md)**, then
> **[Core Concepts](./concepts.md)**, then the system you need.

All examples are C# for the IL2CPP build and run from your `MelonMod` subclass unless noted.

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
| [Difficulty](./systems/difficulty.md)     | Read the savegame's game mode (Classic, Insane, …) and insane/permadeath checks.             |
| [Damage](./systems/damage.md)             | Damage or heal an actor through the game's own health calculation.                           |
| [Actor Buffs](./systems/actor-buffs.md)   | Buff an actor's health, damage and resistances, and find the nearest enemy of a player.      |
| [Status Effects](./systems/status-effects.md) | Apply food, potion and other status effects by GUID, and make an actor bleed.            |

### NPCs & spawning

| Page                                                        | Summary                                                                                             |
|-------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| [Spawning NPCs](./systems/spawning.md)                      | Compose NPCs from modules with `NpcCreator`, spawn bandits, use lazy actors, and remove them again. |
| [External NPCs (JSON & Wizard)](./systems/external-npcs.md) | Place NPCs from JSON files or the in-game wizard — no code required.                                |

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
| [Areas & Regions](./systems/area-region.md)  | Read which region the player is in and react to entering/leaving.           |
| [Routines](./systems/routines.md)            | Waypoint routines, own routine graphs, replacing vanilla routines.          |
| [Global Variables](./systems/global-vars.md) | React to gvar changes and register custom global variables.                 |
| [Quests](./systems/quests.md)                | Custom quests, items, journal entries and world placements from JSON.       |
| [Talents](./systems/talents.md)              | Register custom talents into the talent graph.                              |
| [Map Editor](./systems/map-editor.md)        | Place props and invisible walls, hide vanilla objects, from JSON or in game. |

### Multiplayer

| Page                                  | Summary                                                                                          |
|---------------------------------------|--------------------------------------------------------------------------------------------------|
| [Co-op](./systems/coop.md)            | Ask whether somebody else is in the world and whether this machine should act. Always available. |
| [Networking](./systems/networking.md) | Coop transport: sessions, typed messages, raw channels.                                          |

### Testing

| Page                            | Summary                                                                                          |
|---------------------------------|--------------------------------------------------------------------------------------------------|
| [Testing](./systems/testing.md) | Drive real game instances from a scenario file: load, walk, set variables, kill, assert results. |

## Contributing to the docs

Plain Markdown in `/docs`, no build step. Keep examples verified against the source, and link
every new system page from this index.
