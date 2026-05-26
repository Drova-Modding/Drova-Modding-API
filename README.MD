# Unofficial Drova Modding API

This is an unofficial API for Drova. It is not supported by Drova native. It is a community project to make modding Drova easier.

This is a growing project and will be updated with more features step by step.

## Installation Instructions for Developers

Follow Instructions for https://melonwiki.xyz/#/README?id=automated-installation

After that: https://melonwiki.xyz/#/modders/quickstart (Be aware that Drova is a IL2Cpp Project and follow the instructions for that)

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
- Spawning actors with the lazy actor system for save/load persistence
- Event System to hook into the game events and create your own events
- Dialog System with 40+ nodes to create your own dialogs and hook into the existing ones
- Area Name System to know where the player is currently
- Audio System to play audio clips on actors
- GlobalVarList inspector editor for easier debugging and management of global variables (F6)

## Quick Start Examples

### Creating an Options Menu Panel
```csharp
public class MyModMenu : MelonMod
{
    public override void OnInitializeMelon()
    {
        var builder = OptionMenuAccess.CreateModdingPanel("MyMod");
        builder.AddKeyBinding("MyBinding", "My Key Binding", InputActionType.Custom);
        builder.AddDropdown("MyDropdown", "Select Option", new[] { "Option 1", "Option 2" });
        builder.AddSlider("MySlider", "Volume", 0f, 100f, 50f);
    }
}
```

### Subscribing to Slider Changes
```csharp
public class MyModMenu : MelonMod
{
    private const string MinKey = "MyModMin";
    private const string MaxKey = "MyModMax";

    public override void OnInitializeMelon()
    {
        OptionMenuAccess.Instance.OnOptionMenuOpen += BuildOptions;
    }

    private void BuildOptions()
    {
        var builder = OptionMenuAccess.Instance.GetBuilder("MyMod");
        if (builder == null) return;

        List<GameObject> built = builder
            .CreateTitle(LocalizationAccess.GetLocalizedString("MyMod", "Title"))
            .CreateSlider(LocalizationAccess.GetLocalizedString("MyMod", "MinLabel"), MinKey, 1, 120, 30)
            .CreateSlider(LocalizationAccess.GetLocalizedString("MyMod", "MaxLabel"), MaxKey, 2, 360, 60)
            .Build();

        // Layout: 0 title, 1 min slider, 2 max slider
        if (built.Count < 3) return;

        Slider minSlider = built[1].GetComponentInChildren<Slider>();
        GUI_ConfigOption_Slider minOptions = built[1].GetComponentInChildren<GUI_ConfigOption_Slider>();
        Slider maxSlider = built[2].GetComponentInChildren<Slider>();
        GUI_ConfigOption_Slider maxOptions = built[2].GetComponentInChildren<GUI_ConfigOption_Slider>();
        if (minSlider == null || minOptions == null || maxSlider == null || maxOptions == null) return;

        minSlider.onValueChanged.AddListener(new Action<float>((value) =>
        {
            if (value >= maxSlider.value)
            {
                maxOptions.OnValueChangedListener(value + 1);
                maxOptions.UpdateOptionValue((int)value + 1);
            }

            // Run your mod logic when slider changes.
            ApplyRuntimeChanges((int)value, (int)maxSlider.value);
        }));

        maxSlider.onValueChanged.AddListener(new Action<float>((value) =>
        {
            if (value <= minSlider.value)
            {
                minOptions.OnValueChangedListener(value - 1);
                minOptions.UpdateOptionValue((int)value - 1);
            }

            // Run your mod logic when slider changes.
            ApplyRuntimeChanges((int)minSlider.value, (int)value);
        }));
    }

    private static void ApplyRuntimeChanges(int minMinutes, int maxMinutes)
    {
        // Example: update your own event scheduler/audio range/etc.
    }
}
```

### Spawning Bandits (Lazy Actors)
```csharp
public class BanditSpawner : MelonMod
{
    public override void OnInitializeMelon()
    {
        Vector2 spawnPos = new Vector2(0, 0);

        // Uses the bandit template pipeline and creates a lazy actor.
        var lazyBandit = BanditCreator.CreateSwordShieldBanditLazy(
            "Tough Bandit",
            spawnPos,
            BanditDifficulty.Hard,
            saveToLazyActorStore: true
        );
    }
}
```

### Spawning Custom NPCs (Lazy Actors)
```csharp
public class CustomNPCSpawner : MelonMod
{
    public override void OnInitializeMelon()
    {
        Vector2 spawnPos = new Vector2(10, 20);

        // NpcCreator can now build lazy actors via CreateLazy.
        var lazyNpc = new NpcCreator("My NPC", spawnPos)
            .WithCosmetic("hair_short_1")
            .WithItem("sword_iron")
            .IsPlayerFriendly(true)
            .CreateLazy(saveToLazyActorStore: true);
    }
}
```

### Placing NPCs Without Code (External JSON)
You can now place and configure NPCs without writing C#.

#### In-game Wizard (recommended)
1. Enable cheat mode and open the terminal.
2. Run `npc_wizard`.
3. Configure base data + modules in the window.
4. Click **Save Definition** or **Save + Spawn**.

The wizard is designed to be extensible: newly registered external NPC modules will automatically show up in the UI.
New definitions created in the wizard default their spawn position to the player's current position.

#### Small module author example
```csharp
using Drova_Modding_API.Systems.Spawning;
using System.Text.Json;

public sealed class MyTagModule : ExternalNpcModuleRegistry.IExternalNpcModule
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

    private sealed class State
    {
        public string Tag { get; set; } = "Guard";
    }

    private sealed class RenameWithTagModule(string tag) : INpcModule
    {
        public int Priority => 1000;
        public void Apply(ModuleContext context)
        {
            if (context.Npc != null && !string.IsNullOrWhiteSpace(tag))
                context.Npc.name = $"{context.Npc.name} [{tag}]";
        }
    }
}

// Register once during your mod initialization:
// ExternalNpcModuleRegistry.Register(new MyTagModule());
```

#### Direct JSON editing (for mod authors)
Every mod can ship its own `*.json` file inside the `Mods/Modding_API/NpcPlacement/` folder.
All files in that folder are scanned on game start — each mod's file is completely independent.

1. Create a file, e.g. `Mods/Modding_API/NpcPlacement/my_mod_npcs.json`.
2. Populate it with the structure below and set `enabled` to `true`.
3. On game start, enabled entries are spawned as lazy actors and then managed by the normal save pipeline.

The in-game wizard saves wizard-created definitions to `wizard_placements.json` in the same folder and never touches mod-provided files.

Example entry:
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
        "friendly": "{\"isPlayerFriendly\":true}",
        "equipment": "{\"itemReadableIds\":[\"sword_iron\"]}",
        "cosmetics": "{\"cosmeticReadableIds\":[\"hair_short_1\"]}",
        "entityInfo": "{\"assetGuid\":\"\"}"
      },
      "enabled": true
    }
  ]
}
```

`moduleConfig` is the canonical schema for external NPC definitions.

### Handling World Events
```csharp
public class EventListener : MelonMod
{
    public override void OnInitializeMelon()
    {
        // Register a world event to be picked randomly by the event system
        WorldEventSystemManager.RegisterWorldEvent(new MyCustomEvent());
        
        // Or start an event immediately
        var myEvent = new MyCustomEvent();
        WorldEventSystemManager.Instance?.StartEvent(myEvent);
    }
}
```

### Persisting NPCs (Lazy Actors)
```csharp
public class PersistentNPCSpawner : MelonMod
{
    public override void OnInitializeMelon()
    {
        // Create a lazy actor that will be saved and reloaded
        var lazyActorParams = new LazyActorCreator.LazyActorParams
        {
            AssetReference = AddressableAccess.Creatures.Wolf,
            EntityInfo = new AssetReference("path_to_entity_info"),
            Position = new Vector2(0, 0),
            HasCustomHealth = true,
            MaxHealth = 500,
            CurrentHealth = 500
        };
        
        var lazyActor = LazyActorCreator.CreateLazyActorCreature(lazyActorParams);
    }
}
```

## Bindings in Dev/Debug

### General
- F6 – GlobalVarList inspector editor for runtime variable debugging and editing
- ^ (Caret) – Toggle the cheats menu

### Cheats Menu
When Cheats Menu is open:
- Left-click on an actor to get a menu on the bottom left with options to enter the dialogue editor

### Dialogue Editor
When in the Dialogue Editor:
- Right-click on the graph background to open the graph context menu
- Left-click on a node and drag to move it around
- Middle click and drag to pan the graph
- Scroll wheel to zoom in and out of the graph
- Double-click on a subgraph node to open the subgraph

## Documentation

For detailed documentation on specific systems:
- **Dialogue System**: See [Wiki - Dialogue System](https://github.com/Drova-Modding/Drova-Modding-API/wiki)
- **NPC Spawning**: See [Wiki - NPC Creation](https://github.com/Drova-Modding/Drova-Modding-API/wiki)
- **World Events**: See [Wiki - World Events](https://github.com/Drova-Modding/Drova-Modding-API/wiki)
- **Options Menu**: See [Wiki - Options Menu](https://github.com/Drova-Modding/Drova-Modding-API/wiki)

## Current Version

See [CHANGELOG](./CHANGELOG.MD) for detailed version history.

## API Overview

### Core Systems
- **OptionMenuAccess** - Create and manage custom mod menu panels
- **PlayerAccess** - Access player stats, equipment, and data
- **AddressableAccess** - Load game assets (items, creatures, spells, interactions)
- **InputAccess** - Handle input through Rewired integration
- **ProviderAccess** - Access core game managers and handlers
- **AreaNameSystem** - Track player location in the game world
- **AudioSystem** - Play and manage audio on actors with distance-based volume
- **DialogueSystem** - Create complex dialogue trees with 40+ node types
- **WorldEventSystem** - Create and manage world events with regional coordination
- **NpcCreator** - Spawn and configure NPCs with modular trait system

## License

This project is provided as-is for modding purposes. See [LICENSE](./LICENSE) for details.

## Troubleshooting

### Common Issues

**Q: The mod doesn't load**
- Ensure MelonLoader is properly installed
- Check that the Drova_Modding_API.dll is in your Mods folder
- Verify your mod has the correct MelonAdditionalDependencies attribute

## Contributing

Feel free to contribute to this project! We welcome:
- Bug reports and fixes
- Feature suggestions
- Documentation improvements
- Examples and tutorials

If you have any questions or ideas, feel free to ask on the [Discord](https://discord.com/channels/779751611756707890/1304552763761492089) or create an issue on GitHub.
