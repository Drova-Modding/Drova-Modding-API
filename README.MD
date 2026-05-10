# Unofficial Drova Modding API

This is an unofficial API for Drova. It is not supported by Drova native. It is a community project to make modding Drova easier.

This is a growing project and will be updated with more features step by step.

## Installation Instructions for Developers

Follow Instructions for https://melonwiki.xyz/#/README?id=automated-installation

After that: https://melonwiki.xyz/#/modders/quickstart (Be aware that Drova is a IL2Cpp Project and follow the instrcutions for that)

## Usage
Put into your Drova Installation Mods the .dll file (you can find it under Releases or build it yourself).

Add to your mod project:

```csharp

// other assembly infos...
[assembly: MelonAdditionalDependencies("Drova_Modding_API")]
public class MyMod : MelonMod
{
}
```

And to your csproj: 
```csproj
<Reference Include="DrovaModdingAPI">
	<HintPath>C:\Program Files (x86)\Steam\steamapps\common\Drova - Forsaken Kin\Mods\Drova_Modding_API.dll</HintPath>
</Reference>
```

## Wiki
See [Wiki](https://github.com/Drova-Modding/Drova-Modding-API/wiki)

## Follow the Project

- [Trello](https://trello.com/b/8sUEknxR/drova-modding-api)
- [Discord](https://discord.com/channels/779751611756707890/1304552763761492089) It's the Drova Discord
- [Nexusmods](https://www.nexusmods.com/drovaforsakenkin/mods/4)

## Features
- Easier access to OptionsMenu, you can create your own Panel or use the existing modding panel!
- You can create persistent settings in there including, keyboard bindings, switches, dropdown, buttons and sliders. 
- Create your own loc files for your mods
- Easier access to the Drova Providers and GameManager and Player
- Access to the Addressables, including GUI, Items/Recipies/Spells, Creatures, Bandits and Interactions
- Spawning persistent and non-persistent actors in the world
- Event System to hook into the game events and create your own events
- Dialog System with 40+ nodes to create your own dialogs and hook into the existing ones
- Area Name System to know where the player is currently
- Audio System to play audio clips on actors
- GlobalVarList inspector editor for easier debugging and management of global variables (F6)

## Bindings in Dev/Debug
- F6 – GlobalVarList inspector editor
- ^ – Toggle the cheats menu
- When Cheats Menu is open:
  - Left-click on an actor to get on the bottom left a menu with options to enter the dialogue editor
- When in the Dialogue Editor:
  - Right-click on the graph background to open the graph context menu
  - Left-click on a node and pressing to drag it around
  - Middle click and drag to pan the graph
  - Scroll to zoom in and out of the graph
  - Double-click on a subgraph node to open the subgraph



### Contributing
Feel free to contribute to this project. If you have any questions or ideas, feel free to ask.
