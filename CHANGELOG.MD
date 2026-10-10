# Changelog

## [Unreleased]
### Added
- Networking: LAN discovery for direct hosts. `NetworkAccess.SetDiscoveryInfo(provider)` makes a direct host answer discovery broadcasts with a mod-defined payload; `StartDiscovery(port)`, `StopDiscovery()`, `IsDiscovering` and `GetDiscoveredHosts(buffer)` (`DiscoveredHost`: address, port, payload, last seen) find such hosts on the local network, broadcasting every 2 s on a socket of their own. Inert unless a mod uses it, never active in a relay session, and a host answers only senders on a private network. See `docs/systems/networking.md`.
- Test runner as a download: the Release packaging also writes `Drova_TestRunner_win-x64.zip` and `Drova_TestRunner_linux-x64.zip` (single-file, needs the .NET 8 runtime). The API zips are unchanged.
- Test runner finds the game through Steam (registry or default install on Windows, `~/.steam/steam` or `~/.local/share/Steam` on Linux, every library in `libraryfolders.vdf`); `--game` still overrides. On Linux it launches the native `Drova.x86_64`.
- Test runner suites: pass a folder (every `.scn`/`.coop` in it) or a `.txt` list of scenarios. Games are closed between scenarios, a summary lists each result with its time and first failing line, and the exit code is 0 only when all passed. With `--keep` a suite stops at the first failure and keeps its games.
- Test runner `--language <code>` (default `en`): sets the game's `language.txt` for the run and restores it afterwards, also on failure or Ctrl+C; `keep` leaves it alone.
- `CoopAccess.ShareHostSetting(key, read, enforce, needsReload)`: a setting that follows the host while a co-op session is live, for settings that change what the world contains. The co-op mod sends the host's value, enforces it on clients (reloading their save once when needed) and drops the override when the session ends.
- `Hooking.FiredCounts()` and `Hooking.NeverFired()`: how often each hook applied through `Hooking` has run, by label. A hook on an inlined method applies cleanly and never runs, and this is the only way to see it.
- `AudioManager.FindDialogueClip(actorName, clipName)` (and `AssetBundleAudioProvider.GetClipByName`): a dialogue clip by its own name, for a machine that knows which clip played but did not run the dialogue - a co-op teammate's conversation. Tries the actor's bundle, the narrator's, then every bundle already loaded.
- `TestAgent`: `shot <file.png>` (a screenshot of that instance), `netstats` (the transport counters as text), `critstop on|off` (the game's no-crit cheat, for a test whose outcome a critical hit would decide); `ai` also prints `home=`, `homereq=` and `target=name@x,y`; a target by name is looked for within 2500 units (`hit` reports `dist=`) - an NPC of the same name on another tower floor was being hit. The test runner records every instance of a run (a screenshot each 2 s, and `track <guid>` samples an actor's `ai` every 4 s): a failed run keeps the recording under `failed-logs/<run>/record`, the last five passed runs per scenario under `recordings/<scenario>`; `--no-record` turns it off. An instance whose agent does not answer within 3 minutes of launch is restarted once. Instances run windowed, side by side on the secondary monitor.
- `PauseMenuAccess.AddButton(id, label, onClick, visible, interactable, closeMenu)`: a mod's own button in the in-game pause menu, a copy of the game's "Load latest" button placed below it, so it looks and navigates like the rest. Label, visibility and whether it can be pressed are asked again each time the menu opens. `TestAgent` gains `weather` (the five levels, updater state, locks and the fog effect's live percentage), `foglets [range]` (nearby foglet fog patches: mode, defeated, player inside, foglets spawned/active/alive), `merchant [take|give <item> [n] | accept | close]` (drives the merchant window), `effect remove <name>` (takes an effect off the player the way a cure does), `pause` (opens the pause menu) and `click <name>` (presses an active UI button by its GameObject name - `ModButton_<id>` for these). The test runner starts its instances at 1280x720 (`-screen-width`/`-screen-height`, overridable per scenario): two or three games at native resolution kept the GPU hot for nothing. Set the test profiles' display mode to borderless (`DisplayMode { 1 }`): exclusive fullscreen at that size switches the whole monitor's mode.
- Automated in-game testing: `TestAgent` (`Systems/Testing`) lets a script drive a running game over a localhost port, and `tools/Drova.TestRunner` launches instances and plays scenario files against them. See `docs/systems/testing.md`. Started with `--agent-port N` the game answers `load`, `walk`, `tp`, `gvar`/`setgvar`, `kill`, `dead`, `counters`, `inside`, `near` and more; a mod adds its own commands with `TestAgent.RegisterCommand`. `--profile X` (`LaunchProfile`) moves that instance's savegames and settings to their own folder, because two instances side by side otherwise overwrite each other's slots. Both are inert without their arguments. Grown out of the co-op mod's test harness, where each rule here was paid for once: an unfocused Unity window stops ticking, the game drops a save load asked for while its main menu is still building, and only the movement input's own update can walk the player.
- `WindowContentAccess.OnJournalLetterShown`: fires after the journal's Letters tab builds a letter's content. The journal never passes through `GUI_Window_Letter` — `GUI_Journal_EntryController.Init` instantiates the letter content prefab into its own pane — so a content rewrite subscribed only to `OnLetterShown` silently showed vanilla content on every journal re-read. `OnLetterShown`'s documentation claimed it covered journal re-reads; that claim was wrong and is corrected.
- `CoopAccess`: ask whether somebody else is in the world (`IsMultiplayer`), whether this machine decides what the shared world contains (`IsWorldAuthority`), and whether work of a given kind should happen here at all (`ShouldRun(CoopBehavior)`), plus the session's players, remote-body and replication checks, `Supports(CoopCapability)` and a shared seed with keyed derivation. See `docs/systems/coop.md`. This is a contract rather than a transport: compiled into every build, no networking dependency, and a co-op mod plugs in by registering an `ICoopProvider`. The defaults are deliberately inverted from a networking API. With nothing registered, `IsWorldAuthority` is true and `IsMultiplayer` is false, so an authority-gated mod runs normally on the machines that have no co-op mod at all, which is nearly all of them. `NetworkAccess.Role` is not a substitute and fails the other way, since with no networking build it is `None`.
- `ActorSpawnAccess`: `TrySpawn`, `TrySpawnFromAsset`, `TryDespawn`, `TryKill`, `TryKillOrDespawn`, plus `IsRuntimeCreated(guid)`, `RecordSpawn`, `RecordLazySpawn`, `TryGetSpawnAsset`, `TryGetLazySpawnAsset`, `OnActorSpawned` and `OnLazyActorSpawned`. See `docs/systems/spawning.md`. Removal needs an API because the obvious version does not look like a bug: destroying a `LazyActor` destroys the component only, leaving the creature it spawned standing in the world while the cleanup code reports success. Despawning and killing are different outcomes, one silent and one that drops loot, so both are exposed and the caller picks. `IsRuntimeCreated` answers whether a guid was invented in this process or read out of a scene, which a shared-world mod cannot work out by looking, because runtime handles are registered in the same guid table the scene-authored ones live in.
- `RegionAccess`: read the player's current region rather than only being told when it changes, with `TryGetCurrentRegion`, `GetCurrentRegions`, `IsInRegion`, `IsInCave`, `TryGetRegionByName` and `OnRegionChanged` forwarded from `AreaNameSystem`, which keeps the subscriber list itself and rebinds to whichever system is current, so subscribing before the world exists is safe and handlers survive a return to the main menu. A new `AreaNameSystem` is built on every gameplay load, so a handler attached to the system directly stops firing after the first reload. See `docs/systems/area-region.md`. Areas overlap and the game resolves them by priority, so the resolved region and the whole set are both offered. `TryGetRegionByName` returns false for an unrecognized name, where `RegionExtensions.GetRegionByName` cannot fail and hands back the catch-all region, letting a typo in a mod's configuration bind silently.
- `PlayerAccess.TryGetLevel`, for the local player and for any actor. A `Try` pair rather than a number because the read misses for a frame or two after a world loads, and a mod that answers a failed read with a default of one gets a plausible level instead of an error, so anything scaled by it silently collapses to its weakest form. The actor overload exists because in a co-op session there is more than one player.
- `DamageAccess`: deal damage to an actor, or heal one, through the game's own health calculation, plus accessors for the four damage types the game keeps on two different handlers (physical, magical, pure, healing). See `docs/systems/damage.md`. The reason this is worth an API rather than three lines in each mod is that the obvious three lines are wrong in two ways that only show up later. Writing `Health._curHealth` or calling `SetHealth` skips the calculation, and the damage blockers live inside it — including the one that turns a killing blow into something other than death, so a raw write that reaches zero latches `_isDead`, which nothing ever clears, and the actor is permanently dead with every revive path unused. The second is that naming a source does not merely attribute the blow: the game runs that entity's offensive stats over the value, so passing an attacker whose gear is already reflected in the figure counts their weapon twice. `DamageAccess` takes the source as an optional parameter and documents which case is which.

- `TestAgent` combat and action commands: `press <bind> [hold] [delay]` (the attack, block and dodge buttons, with a hold for charged attacks and a delay so a burst is paced by the game clock), `stance <mode>`, `useitem <item> [hold]` (quick-slot use: potions, food, spells - placed on an ability slot and selected first), `equip <item>`, `itemsearch <fragment>`, `walkto <x,y|guid>` (walks the NPCs' own A* path instead of teleporting), and `actlog start <me|name>` / `read` / `brief`, which records what a body plays - bind, combo step, charged, prefab, stance - so a co-op scenario can compare a player with their copy on the other machine (`state` carries it as `actlog=`). A line continues its dialogue once only, which also removed the `DS_DialogueUGUI.InitializeText` errors a doubled continue caused.
- `TestAgent` quest-testing commands: `save <slot>` writes the running game into a slot of the current hero (refused unless the instance runs with its own `--profile`), which is how a scenario gets a fixture save - flags set in a running world leave NPCs in the older routine sphere and their cutscenes hang, flags saved and reloaded do not. `popup [yes|no]` reads or answers a confirm popup a dialogue stopped on. `objects <fragment> list` lists inactive matches too, with their parent and active state. `ai <guid>` names the active routine place, the actor's position and whether it is in a dialogue or ready for one (`talking=` `ready=`), its current action, whether it holds a torch and its interaction mode (`action=` `torch=` `mode=`), and `dialogue` names the tree, node id and node type it stands on, inside the running sub-dialogue if there is one. The runner gives a single-letter instance its letter's port (C is 47103), so one game can run beside two kept ones. Puzzle and story helpers: `perceive <fragment> [range]` makes hidden perception objects usable, `place <item>` puts an item on the nearest item place as its chooser window would, `port <guid>` sends an NPC to its routine place, and `skip first` answers every question with its first answer until the conversation ends. Name lookups now take the nearest living match rather than the first one found. A name also matches an actor by its parent's exact name, for dummies that all share one name. `CheatMenuAccess.FireCommand` (and so the agent's `run`) now reaches a mod's console commands before cheat mode was ever switched on; it used to run nothing, silently. The agent serves each connection on its own thread, so a second client can query a game while a runner holds its connection. A launched game's MelonLoader console is moved onto the secondary monitor (where the game already opens) so a test run does not cover the user's working window; with a single monitor nothing moves. `scenes <fragment> [rearm]` also lists each observer's participants and owner state and can let a fired observer start again.
- `--net-delay minMs-maxMs`, test-only: every received packet waits a random time in that range before it is handed on. Two instances on one machine talk with no delay at all, so a fault that needs two messages to land in one frame never shows there. Ordered and sequenced traffic keeps its order; plain unreliable packets may overtake each other.

- Networking: an in-game stats window on the `api_netstats` cheat command, in `Debug` builds only. `on`, `off`, `reset` and `log` are arguments, so counters can be zeroed and a snapshot written to the log without the window ever being open. Round trip, throughput and 30 seconds of graphs, everything the transport dropped split by cause (undecryptable, NaN or infinity, unknown message id, malformed, receive failure), per-peer latency and bytes, and a per-message-id table showing which message is spending the bandwidth. See `docs/systems/networking.md`. It exists because the three questions asked of a coop session — is it the network, whose connection, and which message — are the three a mod cannot answer for itself: the first two live inside LiteNetLib and the third is only visible where messages are serialized. Counting restarts with each session and outlives it ending, which is when somebody actually looks. A "Log snapshot" button writes the whole thing to the log, since that is what gets attached to a bug report. The counters themselves are compiled into `Release` builds too and cost a few interlocked adds per packet; only the window is stripped, so the build somebody was running when coop misbehaved is not the one without numbers.

### Changed
- Nullability annotations (no behavior change on any working path): every `Try...(out T value)` whose value is a reference is now `[NotNullWhen(true)] out T? value` (`[MaybeNullWhen(false)]` for `ConfigAccessor.TryGetConfigValue<T>`), and accessors that can return null say so: `ProviderAccess.ItemDatabase`/`GVarDatabase`/`RecipeDatabase`/`StatusEffectDatabase`/`GetGameDatabase()` (null before the game has loaded its database; they used to throw), the singleton getters, `PlayerAccess.Get*Slot()`/`GetPlayerAttributeStats()`, `SaveGameSystem.GetStore`, `ObjectRelocatorFactory.GetRelocator` and `IObjectRelocator<T>.DeserializeObjectFromJson`; the events `OptionMenuAccess.OnOptionMenuOpen`/`OnOptionMenuClose`, `AreaNameSystem.OnRegionChanged` and `InputActionRegister.OnInputActionsLoaded`, and the relocators' `SaveData` string fields. Binary compatible.
- Test scenarios are named `.scn` when they run one game and `.coop` when they run two or more, so the file name says whether a test needs a co-op session.
- Networking: a typed message id travels in one byte instead of two, so ids are 0-255 (`RegisterWithId` refuses anything higher). One byte per message, on the messages sent every frame per player.
- Networking ships in the ordinary `Debug` and `Release` builds. The `Coop` opt-in is gone — no `-p:Coop=true`, no `DROVA_COOP`, no `DebugCoop`/`ReleaseCoop` configurations, and `NetworkAccess.IsSupported` is removed along with the `NotSupportedException` every action method threw without it. Four release artifacts to describe, download and pick between was the cost of keeping the transport optional, and it was paid by everyone: a mod author had to guard every call, and a player had to know which of two zips their mods wanted. `LiteNetLib.dll` is now always installed to `UserLibs`, which is the whole footprint the toggle was avoiding.
- Networking: a typed message carrying a NaN or an infinity in any float or double is no longer delivered to its handler. Nothing for a mod to opt into — the reader marks the packet and the dispatcher drops it, counted and summarized every few seconds rather than logged per packet, because anyone who can send one can send thousands. There is a real failure behind this: a non-finite value assigned to a transform is read back as the input to the next frame's interpolation, so a single hostile packet leaves an object broken for the rest of the session, and no care on the receiving mod's part repairs it afterwards. Expecting every mod to validate every float it reads is how that ships. Finite values are untouched, so honest traffic costs nothing.

- The `MelonInfo` version now reads `0.6.0`. It had been left at `0.5.4`, so the MelonLoader console and every log a player attaches to a bug report named the previous release, which makes "which build are you running" unanswerable exactly when it is being asked.

### Removed
- `FileAudioProvider` and the NVorbis dependency behind it. Loose `.ogg` files are no longer a way to ship dialogue audio; pack the clips into a per-actor AssetBundle and `AssetBundleAudioProvider` picks them up under the same keys, minus the extension. The file provider stopped being the default in 0.5.1 when bundles took over, so nothing has reached it since unless a mod constructed it by name, and no published mod does — DrovaTTS, the one audio mod, implements `IAudioProvider` itself. What it did cost was an install step: `UserLibs/NVorbis.dll` had to be present or the API could not load at all, and the resulting error names neither NVorbis nor the audio system. `UserLibs` now holds `LiteNetLib.dll` alone.

### Fixed
- Networking: `StartHost` on a UDP port that cannot be opened (another program holds it) reported a running host that listened nowhere. It now raises `OnNetworkError` and leaves `Role` at `None`.
- Relocators saved and restored nothing: their `SaveData` payloads use public fields, which System.Text.Json skips by default, so `SerializeObjectToJson` wrote `{}`. They now serialize with `RelocatorSaveData.JsonOptions` (`IncludeFields = true`); a relocator of your own should pass it too.
- The player's voiced answers were never heard. Answer clips were loaded once per tree name when the tree asset was deserialized, and a conversation runs on a copy of the tree, which never got them; and the patch meant to say a voiced answer sat on `DS_MultipleChoiceNode.OnOptionSelected`, which hub answers (most NPC talk menus) never pass through. An answer's clip is now loaded when it is picked (`AudioManager.AnswerClip`), and a voiced answer is said through the game's own `saySelection`, switched on for that pick where the window settles every answer (`DS_DialogueUGUI.Finalize`): the window shows it as the player's line and plays it. Answers without a clip behave as before.
- An audio bundle Unity cannot read is no longer loaded again for every line: one unreadable file was hundreds of "Failed to read data for the AssetBundle" errors per conversation. It is reported once, and when the file is longer than its own header says, the message says it was copied as text (an FTP transfer in ASCII mode turns each line feed into two bytes) and to copy it again in binary mode.
- Networking: a peer that leaves for a LiteNetLib reason the API does not map (PeerNotFound, Reconnect, InvalidProtocol...) is now logged with that reason instead of a bare "Unknown"; a relay dropping both ends of a session was otherwise unexplainable. The test runner keeps the newest game log of each instance under `failed-logs` next to a failing scenario, before the game's log rotation can delete them.
- `SaveGameSystem.AfterSaveGameLoaded` (and the registered stores' `Load`) never ran for the game's synchronous load (`SavegameGameHandler.TryLoadSavegameSync`) - the patch sat on `LoadSavegameFromSavegame`, which that path does not pass through - and on scene loads it ran before the loaded scene existed. It now fires where the game raises its own after-loaded event, on every path (a postfix on `LoadFloatVars`, which each path calls directly before it). `BeforeSaveGameLoaded` gained the synchronous path too (a postfix on `SavegameLoader.TryLoadSavegameSync`). A co-op mod that loaded a save this way kept every "already sent" memory from the world it had left.
- Networking: a silent peer is now kept for 30 s instead of LiteNetLib's default 5 s. A story teleport that loads a big area stalls the whole game for longer than 5 s, and both ends of a session timed each other out. The relay's own peer timeout (`RELAY_PEER_TIMEOUT_SECONDS`, default 15) then applies on top.
- The dialogue-audio patch on `DS_StatementNode.OnExecute` threw a NullReferenceException for every line spoken by a speaker without a loaded body (a lazy NPC, common in a teammate's scene), and would have thrown an invalid cast for a speaker that is not a `DS_DialogueActor`.
- A game bug the API now absorbs: an NPC that opens a door subscribes to the door's "interaction ended" event and never unsubscribes, so after that NPC's body is unloaded every later use of the door threw a `NullReferenceException` per such NPC. The dead listener only cleared the gone NPC's own flag, so it is now skipped.
- World events (`WorldEventSystemManager`) fired every few seconds when the Modding options menu had never been opened, because the timer fell back to 1 s when the min/max sliders had never been written. It falls back to the sliders' own defaults (30/60) now.
- Installation docs told players to download `Drova_Modding_API.dll` on its own and drop it into `Mods`, omitting the `UserLibs` half of the release zip. The API links against `LiteNetLib.dll`, so that install cannot load, and the failure does not name the missing file: MelonLoader reports it as a mod being unable to resolve `Drova_Modding_API, Version=1.0.0.0`, which reads as a version mismatch even though the API's assembly version is permanently `1.0.0.0` and always has been. `README.MD` and `docs/getting-started.md` now describe extracting the archive, and a troubleshooting section covers that message, a renamed or duplicated mod DLL, and the missing Modding options tab — which is the same fault seen from the other end, since the tab is only built once a mod asks for it and an API that failed to load leaves nothing to ask.
- Controller navigation did not work on a modded options tab: the stick could not reach any row, and with a controller connected the tab opened with nothing selected and logged "Could not find selectable on OptionTab". `GUI_OptionPanel_Generic` discovers its rows once, in `InitRows`, and skips that work whenever its row list is non-null — so clearing the cloned panel's stale list in 0.5.3 left an empty list that the panel then treated as already-discovered, and the explicit up/down links it builds from it covered no rows at all. `OptionUIBuilder.Build()` now refills the list from the panel's row root and rebuilds the navigation, so a mod gets this by finishing its chain as it already does.
- Modded option rows no longer spam "Null object in list at index 0 on GUI_OptionRow_Description(Clone)". The warning is the game's own `ActivateByInputDevice` reading a null the row prefabs were authored with; it fires per instance on every enable and every input device change, so a panel of a few rows produces a burst each time the menu opens. The API strips the nulls off the clones it creates. Nothing behavioural — those entries never pointed at anything.
- `OptionMenuAccess.OnOptionMenuOpen`/`OnOptionMenuClose` threw a `NullReferenceException` out of the `GUI_Window` patches whenever no mod had subscribed, which is the normal case for anyone running the API without an options-using mod. The events are now raised conditionally. The Debug-only editor UI, which was the one subscriber and never unsubscribed, also left a dead handler on the singleton after a scene reload and threw from it on every subsequent menu open.
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
