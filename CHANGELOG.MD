# Changelog

## [0.4.1 - 2026-05-16]
### Added
- World event handling infrastructure with EncounterEvent, PersistentEncounterEvent, RegionalEventRegistry, WorldEventPicker, and WorldEventSystemManager
- Region management improvements with WorldRegionCoordinator for regional event coordination
- Modular NPC creator system with configurable trait modules (Alignment, CustomStats, EquipmentPreset, HealthPreset, TalentPreset, CosmeticsPreset, FlowPreset)
- BanditCreator enhanced with health and experience scaling for difficulty management
- Player-friendly NPC configuration with default factory methods and centralized readable IDs
- New NPC module architecture with INpcModule interface for extensibility
- Additional asset references and improved addressable file generation

### Changed
- NpcCreator refactored with modular prioritization and context-based handling
- Enhanced region and event handling throughout the API for better world event management
- UI improvements with better editor picker functionality (NpcMouseRaycast)
- Improved naming consistency across region and event management systems
- Better separation of concerns in NPC spawning and talent systems

### Fixed
- Apply suggestions from code review for code quality improvements
- Various system refinements and edge case handling

## [0.4.0 - 2026-05-03]
### Added
- Functional dialogue system with graph editor support (nodes, tasks, multiple choice flow)
- Generic, world, and player dialogue generation improvements
- Dialogue audio integration with provider interfaces and file-based audio loading
- Dynamic dialogue audio volume based on player distance
- Utility scripts and mappings for dialogue/TTS workflows
- GlobalVarList inspector editor for easier debugging and management of global variables (F6)
- Drova 1.3 / 1.3.1 compatibility (new items, GVarList fixes)
- Added many new dialogue nodes: Equip, UnEquip, UseItem, GiveItem, GiveExp, Heal, Faction, PlaySfx, SetSpell, and CutsceneAction
- Typing delay support for dialogue text
- Added `GetPlayerAttributeStats()` and equipment slot access to `PlayerAccess`
- Expanded `AddressableAccess` to include Interactions and new NPC templates
- Added `DialogueAudioDistanceManager` for more precise volume adjustments


### Changed
- Improved dialogue editor rendering, line drawing, and graph navigation behavior
- Refined dialogue creation flow and handling for more generic dialogue cases
- Cleanup and optimization around provider access and release compilation
- On building no more warnings :D

### Fixed
- Fixed an error message which would appear when we tried to patch the options menu
- Fixed a bug where interrupting a rebinding attempt could leave the control in a stale state and require clicking again before rebinding would work
- Fixed the custom input rebinding flow to clean up properly on cancel/complete and restore the UI state correctly
- Fixed a bug where the dialog window speech text mesh pro would reset incorrectly
- Prevented the options menu from closing during key rebinding to avoid interruption

## [0.3.0 - 2024-11-29]
### Added
- Event System
- Controller Bindings
- Actor Spawning for creatures
- Permanent Actor Spawning for creatures (Meaning they will save and load with the game)
- Input Access to Rewired (Thanks to HanHan)
- Area Name System, to know where the player is currently
- Save System to hook in
- GlobalFields added for common SceneNames

### Fixed
- Fixed a bug where option menu would not be open again after closing it

### Changed
- Changed from `KeyCode` to `InputAction` to support more input devices
- Added a Release Package and Debug Package (For User and Developer)

### Deprecated
- Deprecated the `KeyCode` approach in favor of the `InputAction` approach

### Known Issues
- In the console you will see 2 errors that something was not set properly regarding the Patches, it does not affect the Game from my investigations, but I will look into it in the future.

## [0.2.3 - 2024-11-04]
### Fixed
- Fixed a bug where the KeyBinding default value did not get saved and was not accessible at runtime

### Added
- A lot more null safety and better logging

## [0.2.2 - 2024-11-03]
### Fixed
- Fixed a bug where the KeyBindingSection would not refresh their conflict marker
- Fixed a bug where the KeyBindingSection would not display the conflict marker, when the keybinding in Drova have Spaces

## [0.2.1 - 2024-11-02]
### Fixed
- Fixed a bug where the KeyBindingSection would not refresh the keybindings when you change the language

## [0.2.0 - 2024-11-02]
### Added
- Button component
- PlayerAccess thanks to @HanHangit
- Addressable Access and Addressable Generation
- Added GameManager under ProviderAccess and related GameHandlers

### Changed
- OptionMenuAccess has now a method to get the builder for a common modding panel settings (instance method). This requires a identifier for your settings.
- OptionMenuAccess has now a method to get the builder for your own panel settings (static method)
- CreateKeyBindingSection has now a changed order of parameters. Title is now the second parameter and optional. Also will it now longer create a section if the title is null or empty. And it will put it into your corrosponding Panel.

### Fixed
- Fixed a bug where the OptionMenuAccess would not work display itself correctly when you are ingame
- Fixed a bug in the dictonary of the GameSettings where the key would already exist
- Fixed a lot of null checks in the OptionMenuAccess