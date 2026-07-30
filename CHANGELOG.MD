# Changelog

## [Unreleased]
### Added
- `CoopAccess`: ask whether somebody else is in the world (`IsMultiplayer`), whether this machine decides what the shared world contains (`IsWorldAuthority`), and whether work of a given kind should happen here at all (`ShouldRun(CoopBehavior)`), plus the session's players, remote-body and replication checks, `Supports(CoopCapability)` and a shared seed with keyed derivation. See `docs/systems/coop.md`. This is a contract rather than a transport: compiled into every build, no networking dependency, and a co-op mod plugs in by registering an `ICoopProvider`. The defaults are deliberately inverted from a networking API. With nothing registered, `IsWorldAuthority` is true and `IsMultiplayer` is false, so an authority-gated mod runs normally on the machines that have no co-op mod at all, which is nearly all of them. `NetworkAccess.Role` is not a substitute and fails the other way, since with no networking build it is `None`.
- `ActorSpawnAccess`: `TrySpawn`, `TrySpawnFromAsset`, `TryDespawn`, `TryKill`, `TryKillOrDespawn`, plus `IsRuntimeCreated(guid)`, `RecordSpawn`, `RecordLazySpawn`, `TryGetSpawnAsset`, `TryGetLazySpawnAsset`, `OnActorSpawned` and `OnLazyActorSpawned`. See `docs/systems/spawning.md`. Removal needs an API because the obvious version does not look like a bug: destroying a `LazyActor` destroys the component only, leaving the creature it spawned standing in the world while the cleanup code reports success. Despawning and killing are different outcomes, one silent and one that drops loot, so both are exposed and the caller picks. `IsRuntimeCreated` answers whether a guid was invented in this process or read out of a scene, which a shared-world mod cannot work out by looking, because runtime handles are registered in the same guid table the scene-authored ones live in.
- `RegionAccess`: read the player's current region rather than only being told when it changes, with `TryGetCurrentRegion`, `GetCurrentRegions`, `IsInRegion`, `IsInCave`, `TryGetRegionByName` and `OnRegionChanged` forwarded from `AreaNameSystem`, which keeps the subscriber list itself and rebinds to whichever system is current, so subscribing before the world exists is safe and handlers survive a return to the main menu. A new `AreaNameSystem` is built on every gameplay load, so a handler attached to the system directly stops firing after the first reload. See `docs/systems/area-region.md`. Areas overlap and the game resolves them by priority, so the resolved region and the whole set are both offered. `TryGetRegionByName` returns false for an unrecognized name, where `RegionExtensions.GetRegionByName` cannot fail and hands back the catch-all region, letting a typo in a mod's configuration bind silently.
- `PlayerAccess.TryGetLevel`, for the local player and for any actor. A `Try` pair rather than a number because the read misses for a frame or two after a world loads, and a mod that answers a failed read with a default of one gets a plausible level instead of an error, so anything scaled by it silently collapses to its weakest form. The actor overload exists because in a co-op session there is more than one player.
- `DamageAccess`: deal damage to an actor, or heal one, through the game's own health calculation, plus accessors for the four damage types the game keeps on two different handlers (physical, magical, pure, healing). See `docs/systems/damage.md`. The reason this is worth an API rather than three lines in each mod is that the obvious three lines are wrong in two ways that only show up later. Writing `Health._curHealth` or calling `SetHealth` skips the calculation, and the damage blockers live inside it — including the one that turns a killing blow into something other than death, so a raw write that reaches zero latches `_isDead`, which nothing ever clears, and the actor is permanently dead with every revive path unused. The second is that naming a source does not merely attribute the blow: the game runs that entity's offensive stats over the value, so passing an attacker whose gear is already reflected in the figure counts their weapon twice. `DamageAccess` takes the source as an optional parameter and documents which case is which.

### Changed
- Networking: a typed message carrying a NaN or an infinity in any float or double is no longer delivered to its handler. Nothing for a mod to opt into — the reader marks the packet and the dispatcher drops it, counted and summarized every few seconds rather than logged per packet, because anyone who can send one can send thousands. There is a real failure behind this: a non-finite value assigned to a transform is read back as the input to the next frame's interpolation, so a single hostile packet leaves an object broken for the rest of the session, and no care on the receiving mod's part repairs it afterwards. Expecting every mod to validate every float it reads is how that ships. Finite values are untouched, so honest traffic costs nothing.

### Fixed
- `Hooking.TryPrefix`/`TryPostfix` by name threw `AmbiguousMatchException` and disabled the hook whenever a base type declared another method of the same name, for example `PlayerAttributeStats.CheckPlayerGainExp(Actor, HealthChangeArgs, int)` shadowed by `AttributeStats.CheckPlayerGainExp(HealthChangeArgs)`. A name-only lookup walks base types and the interop assemblies are full of these pairs, so the failure is common. Resolution now narrows to the methods the named type itself declares. It is still refused when that type declares several overloads of the name, because guessing would patch the wrong method and report success, and the error now says to pass a resolved `MethodBase` from `AccessTools.Method` instead.
- Networking: a typed packet shorter than its own two-byte message id read past the end of the buffer. The receive path's handler caught it, but each one produced a logged stack trace, so anyone who knew the port could fill the log. Short packets are now ignored.
- Spawning more than one lazy actor flooded the log with "Attempting to load AssetReference that has already been loaded" followed by "Attempting to use an invalid operation handle" in `LazyActor.LoadActor`. `AssetReference.LoadAssetAsync` stores its handle on the reference instance, so the shared statics from `AddressableAccess` (e.g. `NPCs.Human_Template`, used by every `NpcCreator`) could only be loaded once. `LazyActorCreator` now copies the reference per actor, and the equipment, cosmetics, and flow preset modules load through `Addressables.LoadAssetAsync` instead of the reference instance.

## [0.5.4 - 2026-07-22]
### Added
- Introduced new Systems for modders including, GameEvents, QuestStateReader, SceneStreamAcces, and TeleporterAccess.
- Added Hookutils for easier modding of harmony patches.

### Fixed
- Savegame system heard on the wrong hooks.

## [0.5.3 - 2026-07-19]

### Added
- `DifficultyAccess`: read the savegame's game mode (Classic, Insane, …), with insane-family and permadeath checks. See `docs/systems/difficulty.md`.

### Fixed
- Fixed a NullReferenceException in `GUI_OptionPanel_Generic.InitRows` when activating a custom option panel: the panel clone created by `OptionMenuAccess.AddPanel` kept serialized references to the destroyed template rows (`_rows`, `_insaneOptions`, …). The stale lists are now cleared, which also prevents custom switches from persisting wrong values after the aborted init.

## [0.5.2 - 2026-07-12]

### Fixed
- Fixed a bug where some dialogues would be run from a separate thread and not on the main thread, which could cause issues with Unity API calls. Now all dialogue tasks are executed on the main thread.

## [0.5.1 - 2026-07-02]

### Added
- Added Documentation for the core Systems under `docs/systems/` (dialogues, addressables, cheat menu, routines).
- Added Assetbundle Loading as a new default for Audios
- Improved Performance of loading Audios for AssetBundles and the IsInCave check

### Fixed
- Fixed a bug where loading process of the ActorAddLoadingCompPatch would not filter for our self created actors, which could cause issues with game actors initialization.

## [0.5.0 - 2026-06-20]

### Added
- NPC Wizard: a UI to create NPCs without writing code, with definition browsing, overwrite handling, and one-click export of your NPC as a zip mod.
- External NPC system: place fully configured NPCs in the world via JSON, with modules for routines, dialogue, equipment, cosmetics, health, talents, alignment, friendliness, and entity info.
- Custom global variables: `CustomGVarStore` lets mods create, delete, and persist their own global variables.
- `GvarBusSystem`: get notified when global boolean variables change.
- Localization editor (`LocalizationEntriesEditorSystem`) and mod packaging, so localized strings get bundled with your mod.
- Lots of new dialogue condition tasks: faction checks, crime/atone checks, talent and attribute learn checks, item/equipment checks, instigator and witness checks, and more.
- New dialogue node editors for teaching stats and talents, plus many smaller node editor improvements.
- Main menu fast get in for starting with noclip and godmode.

### Changed
- Global variable inspector reworked into separate read and write views and made easier to use.
- Dialogue editor: better node creation/deletion/duplication, actor parameter controls, and dropdown caching.
- `DialogueStore` now supports stable object references, base64 serialization, and in-memory dialogue patching.
- Cleaner player initialization and event handling using coroutines.

### Fixed
- Fixed dropdown rendering in the global variable inspector.
- Various spawning, entity consistency, and cleanup fixes for custom NPCs.

## [0.4.3 - 2026-05-32]

### Added
- Added a RoutineSystem for modders to define custom routines for Npcs. For now only wait points are supported.
- Added NoSaveGuidRegistry to track spawned entities that should not be saved.
- Added Npc creation via lazy, to prepare all fully fleshed npc since this is needed as part of the Drova Actor system and also to support the new RoutineSystem.
- Added a Cleanup callback for INpcModules to clean up any spawned entities when the lazy actor is unloaded.

### Changed
- Clean up the codebase and added some nullables where appropriate

### Fixed
- Fixed that bandit spawned by the mod no longer saves their data in the Savegame when not intended by the mod developer.
- Fixed a visual in the DS_HubNodeEdtior, it should now be better readable
- Fixed starting gear of harder bandits, which was actually easy equipment on some

## [0.4.2 - 2026-05-18]

### Fixed
- Fixed an issue where the loading screen could freeze

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
