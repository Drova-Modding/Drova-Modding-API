# Unofficial Drova Modding API

This is an unofficial API for **Drova – Forsaken Kin**. It is not supported by Drova natively — it's
a community project to make modding Drova easier. The project is growing and updated step by step.

It runs on [MelonLoader](https://melonwiki.xyz) and wraps the game's IL2CPP types behind friendly
facades so you can build menus, spawn NPCs, write dialogue, react to game events, and persist data
without fighting the engine internals.

## 📖 Documentation

Full, example-driven docs live in **[`/docs`](./docs/README.md)**.

- **[Getting Started](./docs/getting-started.md)** — install MelonLoader, set up your project, write your first mod.
- **[Core Concepts](./docs/concepts.md)** — the mod lifecycle, IL2CPP gotchas, where files live.
- **[All systems →](./docs/README.md)** — a page per system with quick examples and an API reference.

Jump straight to a system:
[Options Menu](./docs/systems/options-menu.md) ·
[Localization](./docs/systems/localization.md) ·
[Game Access](./docs/systems/game-access.md) ·
[Input](./docs/systems/input.md) ·
[Addressables](./docs/systems/addressables.md) ·
[Spawning NPCs](./docs/systems/spawning.md) ·
[External NPCs (JSON/Wizard)](./docs/systems/external-npcs.md) ·
[Save Game](./docs/systems/save-game.md) ·
[Dialogues](./docs/systems/dialogues.md) ·
[Audio](./docs/systems/audio.md) ·
[World Events](./docs/systems/world-events.md) ·
[Areas & Regions](./docs/systems/area-region.md) ·
[Routines](./docs/systems/routines.md) ·
[Global Variables](./docs/systems/global-vars.md) ·
[Talents](./docs/systems/talents.md) ·
[Cheat Menu](./docs/systems/cheat-menu.md)

## Installation (developers)

1. Install MelonLoader following the [automated installation guide](https://melonwiki.xyz/#/README?id=automated-installation).
2. Read the [IL2CPP modder quickstart](https://melonwiki.xyz/#/modders/quickstart) — Drova is an
   **IL2CPP** project, so follow that path.

Full setup, including the `.csproj` reference and your first mod, is in
**[Getting Started](./docs/getting-started.md)**.

## Usage

Put `Drova_Modding_API.dll` (from [Releases](https://github.com/Drova-Modding/Drova-Modding-API/releases),
or build it yourself) into your Drova install's `Mods` folder. Then declare the dependency in your
mod and reference the DLL:

```csharp
// other assembly infos...
[assembly: MelonAdditionalDependencies("Drova Modding API")]
public class MyMod : MelonMod { }
```

```xml
<Reference Include="DrovaModdingAPI">
    <HintPath>C:\Program Files (x86)\Steam\steamapps\common\Drova - Forsaken Kin\Mods\Drova_Modding_API.dll</HintPath>
</Reference>
```

## Features

- Add your own panel (sliders, switches, dropdowns, buttons, key rebinds) to the options menu, with persistent settings.
- Ship localization files for your mod and inject new languages.
- Easy access to the game's managers, databases, and the player.
- Load game assets (creatures, items, entity infos, UI prefabs) via ready-made addressable references.
- Spawn NPCs and bandits from a modular trait system; persist them as lazy actors across save/load.
- Place NPCs from JSON or an in-game wizard — no code required.
- A save system for storing your own data inside Drova save files.
- A dialogue system (40+ node types) plus an in-game dialogue editor.
- Spoken dialogue audio from loose files or AssetBundles.
- A world-event system with regional coordination, and area/region tracking.
- Routines, custom talents, custom global variables, and a runtime gvar inspector (F6).

See the [docs](./docs/README.md) for how to use each of these.

## Building from source

```sh
dotnet build "Drova Modding API.sln" -c Debug
```

`Debug` builds include the dev tooling (dialogue editor, F6 global var inspector, NPC wizard);
`Release` builds strip it. See [Getting Started](./docs/getting-started.md#developer-key-bindings)
for the dev key bindings.

## Follow the project

- [Trello](https://trello.com/b/8sUEknxR/drova-modding-api)
- [Discord](https://discord.com/channels/779751611756707890/1304552763761492089) (the Drova Discord)
- [Nexusmods](https://www.nexusmods.com/drovaforsakenkin/mods/4)

## Versioning

See [CHANGELOG](./CHANGELOG.MD) for detailed version history.

## Troubleshooting

**The mod doesn't load**
- Ensure MelonLoader is properly installed (IL2CPP mode).
- Check that `Drova_Modding_API.dll` is in your `Mods` folder.
- Verify your mod has the correct `MelonAdditionalDependencies("Drova Modding API")` attribute.

## Contributing

Contributions are welcome — bug reports and fixes, feature suggestions, documentation improvements,
and examples/tutorials. Docs live in [`/docs`](./docs/README.md) as plain Markdown; edit a file and
open a PR. Questions and ideas are welcome on the
[Discord](https://discord.com/channels/779751611756707890/1304552763761492089) or as a GitHub issue.

## License

This project is provided as-is for modding purposes. See [LICENSE](./LICENSE) for details.
