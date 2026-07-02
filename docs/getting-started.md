# Getting Started

This guide takes you from a clean game installation to a running mod that uses the Drova Modding
API.

## 1. Install MelonLoader

Drova is a Unity **IL2CPP** game, so MelonLoader must be installed in IL2CPP mode.

1. Follow the [MelonLoader automated installer guide](https://melonwiki.xyz/#/README?id=automated-installation)
   and point it at `Drova - Forsaken Kin.exe`.
2. Launch the game once so MelonLoader generates its support assemblies
   (`MelonLoader/Il2CppAssemblies/…`). These are what you reference from your mod.

If MelonLoader is set up correctly, you'll see its console window when the game starts.

## 2. Install the API

Download `Drova_Modding_API.dll` from the [Releases](https://github.com/Drova-Modding/Drova-Modding-API/releases)
page (or build it yourself, see below) and drop it into:

```
…/Drova - Forsaken Kin/Mods/Drova_Modding_API.dll
```

The API is itself a MelonLoader mod. Your own mod will depend on it.

## 3. Create your mod project

Create a .NET class library and reference both the game's MelonLoader assemblies and the API
DLL. A minimal `.csproj` reference looks like this:

```xml
<ItemGroup>
  <Reference Include="DrovaModdingAPI">
    <HintPath>C:\Program Files (x86)\Steam\steamapps\common\Drova - Forsaken Kin\Mods\Drova_Modding_API.dll</HintPath>
  </Reference>
  <!-- plus references to MelonLoader.dll, Il2Cppmscorlib.dll, the Il2CppDrova* assemblies,
       UnityEngine.*.dll, etc. from MelonLoader/Il2CppAssemblies and the game's managed folder -->
</ItemGroup>
```

> Follow the [MelonLoader IL2CPP modder quickstart](https://melonwiki.xyz/#/modders/quickstart)
> for the full set of references and project settings. Be sure to use the **IL2CPP** path, not
> the Mono one.

Declare the dependency on the API, so MelonLoader loads it before your mod:

```csharp
using MelonLoader;

// In your AssemblyInfo (or above your Core class):
[assembly: MelonAdditionalDependencies("Drova Modding API")]
```

## 4. Write your first mod

Your entry point is a `MelonMod` subclass. MelonLoader calls its lifecycle methods for you.

```csharp
using MelonLoader;

[assembly: MelonInfo(typeof(MyMod.Core), "My First Drova Mod", "1.0.0", "Your Name")]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: MelonAdditionalDependencies("Drova Modding API")]

namespace MyMod
{
    public class Core : MelonMod
    {
        // Called once, very early, before any scene is loaded.
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("My First Drova Mod initialized!");
        }

        // Called every time a Unity scene finishes loading.
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName}");
        }
    }
}
```

Build it and copy your DLL into the `Mods/` folder next to `Drova_Modding_API.dll`. Launch the
game and watch the MelonLoader console for your log lines.

> Most API calls only make sense once a particular scene is active (you can't touch the player
> in the main menu). See **[Core Concepts](./concepts.md)** for which lifecycle method to use
> for what.

## 5. Build the API from source (optional)

If you want to build the API yourself:

```sh
dotnet build "Drova Modding API.sln" -c Debug
```

`Debug` builds include extra developer tooling (the in-game dialogue editor, the global
variable inspector, the NPC wizard). `Release` builds strip those.

## Developer key bindings

These are available while a `Debug` build of the API is installed:

| Key                 | Action                                                               |
|---------------------|----------------------------------------------------------------------|
| `F6`                | Open the **Global Variable inspector** (runtime view/edit of gvars). |
| `` ` `` (backquote) | Toggle cheat mode / the developer console.                           |
| `^` (caret)         | Toggle cheat functionality (context dependent).                      |

With cheat mode on you can left-click an actor in the world to open a small menu (bottom-left)
that lets you, for example, enter the dialogue editor for that actor. See
**[Dialogues](./systems/dialogues.md)** for the editor controls.

## Next steps

- **[Core Concepts](./concepts.md)** — lifecycle, IL2CPP gotchas, file locations.
- **[Options Menu](./systems/options-menu.md)** — add settings to your mod.
- **[Spawning NPCs](./systems/spawning.md)** — put custom characters in the world.
