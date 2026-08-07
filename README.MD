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
[Damage](./docs/systems/damage.md) ·
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
[Cheat Menu](./docs/systems/cheat-menu.md) ·
[Co-op](./docs/systems/coop.md) ·
[Networking](./docs/systems/networking.md)

## Installation (developers)

1. Install MelonLoader following the [automated installation guide](https://melonwiki.xyz/#/README?id=automated-installation).
2. Read the [IL2CPP modder quickstart](https://melonwiki.xyz/#/modders/quickstart) — Drova is an
   **IL2CPP** project, so follow that path.

Full setup, including the `.csproj` reference and your first mod, is in
**[Getting Started](./docs/getting-started.md)**.

## Usage

Extract `Modding_API.zip` (from [Releases](https://github.com/Drova-Modding/Drova-Modding-API/releases),
or build it yourself) over your Drova install. It puts `Drova_Modding_API.dll` into `Mods` and the
library it links against, `LiteNetLib.dll`, into `UserLibs`. Copying the mod DLL on its own is not
enough; the API cannot load without both, and MelonLoader reports that as a missing
`Drova_Modding_API` rather than a missing `UserLibs` file. See
[Getting Started](./docs/getting-started.md#troubleshooting) if a mod asks for
`Drova_Modding_API, Version=1.0.0.0`.

Then declare the dependency in your mod and reference the DLL:

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
- Ship localisation files for your mod and inject new languages.
- Easy access to the game's managers, databases, and the player.
- Load game assets (creatures, items, entity infos, UI prefabs) via ready-made addressable references.
- Spawn NPCs and bandits from a modular trait system; persist them as lazy actors across save/load.
- Place NPCs from JSON or an in-game wizard — no code required.
- A save system for storing your own data inside Drova save files.
- A dialogue system (40+ node types) plus an in-game dialogue editor.
- Spoken dialogue audio from loose files or AssetBundles.
- A world-event system with regional coordination, and area/region tracking.
- Routines, custom talents, custom global variables, and a runtime gvar inspector (F6).
- [Co-op](./docs/systems/coop.md): ask whether somebody else is in the world and whether this
  machine should be the one acting on it. One line, and no effect in single-player.
- A coop [networking transport](./docs/systems/networking.md) for mods that want to talk between two
  games.

See the [docs](./docs/README.md) for how to use each of these.

## Building from source

```sh
dotnet build "Drova Modding API.sln" -c Debug
```

`Debug` builds include the dev tooling (dialogue editor, F6 global var inspector, NPC wizard, and the
`api_netstats` network stats window); `Release` builds strip it. See
[Getting Started](./docs/getting-started.md#developer-key-bindings) for the dev key bindings and
console commands.

Both configurations ship the networking transport, which puts `LiteNetLib.dll` into the game's
`UserLibs` folder alongside the mod DLL in `Mods`. Mods compile against the API's public surface only
— a buildable example is in [`samples/EchoCoopMod`](./samples/EchoCoopMod), and the details are in
the [networking docs](./docs/systems/networking.md).

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

This project is provided as-is for modding purposes. See [LICENCE](./LICENSE) for details.
