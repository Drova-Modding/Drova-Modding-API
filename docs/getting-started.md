# Getting Started

From a clean game install to a running mod that uses the Drova Modding API.

## 1. Install MelonLoader

Drova is a Unity **IL2CPP** game, so install MelonLoader in IL2CPP mode.

1. Run the [MelonLoader installer](https://melonwiki.xyz/#/README?id=automated-installation) on
   `Drova - Forsaken Kin.exe`.
2. Start the game once. MelonLoader opens a console and generates `MelonLoader/Il2CppAssemblies/`,
   which your mod references.

## 2. Install the API

Download `Modding_API.zip` (or `Modding_API_Dev.zip` for the developer build) from the
[Releases](https://github.com/Drova-Modding/Drova-Modding-API/releases) page and extract it into
the game folder:

```
…/Drova - Forsaken Kin/
├── Mods/
│   └── Drova_Modding_API.dll
└── UserLibs/
    └── LiteNetLib.dll
```

- **Both files are required.** Without `UserLibs/LiteNetLib.dll` the API does not load.
- Keep the name exactly `Drova_Modding_API.dll`, directly in `Mods/`. Mods find the API by that
  name, so a renamed copy (`Drova_Modding_API (1).dll`) or one in a subfolder is invisible to them.
- Delete older copies first. Two copies load two APIs.

### Troubleshooting

**`Could not load file or assembly 'Drova_Modding_API, Version=1.0.0.0'`** — the version is not
the problem; the API could not be loaded at all. Check that `UserLibs/LiteNetLib.dll` exists, the
file name is exact, and only one copy is installed.

**The Modding tab is missing from the options menu** — it only appears once a mod adds options, so
this usually means your mods failed to load. Fix the error above.

`MelonLoader/Latest.log` names the failing mod and the exception.

## 3. Create your mod project

Create a .NET class library. Reference the API DLL plus the MelonLoader, `Il2Cpp*` and Unity
assemblies (see the [MelonLoader IL2CPP quickstart](https://melonwiki.xyz/#/modders/quickstart),
IL2CPP path, not Mono):

```xml
<ItemGroup>
  <Reference Include="DrovaModdingAPI">
    <HintPath>C:\Program Files (x86)\Steam\steamapps\common\Drova - Forsaken Kin\Mods\Drova_Modding_API.dll</HintPath>
  </Reference>
</ItemGroup>
```

## 4. Write your first mod

```csharp
using MelonLoader;

[assembly: MelonInfo(typeof(MyMod.Core), "My First Drova Mod", "1.0.0", "Your Name")]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: MelonAdditionalDependencies("Drova Modding API")]

namespace MyMod
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("My First Drova Mod initialized!");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName}");
        }
    }
}
```

`MelonAdditionalDependencies` makes MelonLoader load the API before your mod. Build, copy your DLL
into `Mods/`, start the game and watch the console.

Most calls need a specific scene (no player in the main menu). See
**[Core Concepts](./concepts.md)** for which lifecycle method to use.

## 5. Build the API from source (optional)

```sh
dotnet build "Drova Modding API.sln" -c Debug
```

`Debug` builds include the developer tools below. `Release` builds leave them out.

## Developer tools

Press `Esc` in a game and choose **Modding Tools** (or type `modding_tools` in the console). In a
`Debug` build it lists the NPC wizard, NPC dialogue/routine editing, global variables,
localization, the [Map Editor](./systems/map-editor.md), network stats, content reload, and a
button that opens the folder with all JSON files. The button only shows while at least one tool is
registered. Add your own:

```csharp
ModdingTools.Register(new ModdingTool("mymod_debug", "My Mod Debug", "Shows my mod's state.", MyWindow.Toggle)
{
    IsAvailable = () => PlayerAccess.TryGetPlayer(out _)
});
```

| Key / command       | Action                                                                                      |
|---------------------|---------------------------------------------------------------------------------------------|
| `F6`                | Global variable inspector (Debug build).                                                    |
| `` ` `` / `^`       | Toggle cheat mode / the developer console.                                                  |
| `modding_tools`     | Open the Modding Tools window.                                                              |
| `api_dumpnearby`    | Log the renderers around the player, to identify world objects.                             |
| `api_netstats`      | Co-op network stats; see [Networking](./systems/networking.md#seeing-what-the-transport-is-doing). |

With cheat mode on, left-click an actor to open a small menu (bottom-left), for example to edit its
dialogue. See **[Dialogues](./systems/dialogues.md)**.

## Next steps

- **[Core Concepts](./concepts.md)** — lifecycle, IL2CPP gotchas, file locations.
- **[Options Menu](./systems/options-menu.md)** — add settings to your mod.
- **[Spawning NPCs](./systems/spawning.md)** — put custom characters in the world.
