# AGENTS.md

## Big picture
- This repo is a single `net6.0` MelonLoader mod assembly (`Drova Modding API/Drova Modding API.csproj`) for the IL2CPP game *Drova*; almost every public API is a thin convenience layer over game types from `Il2CppDrova*`, Unity, and Harmony.
- Startup is scene-driven. Read `Core.cs` first: `OnInitializeMelon()` registers save stores and option-menu hooks, `OnLateInitializeMelon()` creates the persistent `InputActionRegister`, and `OnSceneWasLoaded()` fans out into `SystemInit.GameplayInit()` / `AiLogicInit()` / main-menu setup.
- Runtime systems are attached as Unity components at scene load, not resolved from DI. `SystemInit.cs` creates a `ModdingAPI` GameObject for gameplay systems like `AreaNameSystem`, `WorldEventSystemManager`, dialogue-audio distance handling, external NPC placement, and the NPC wizard.
- `Patches/` is the glue layer: Harmony patches bridge Drova lifecycle events into this API (`GUIWindowPatch.cs`, `SaveGameHandlerPatch.cs`, `ActorStartInitAsyncPatch.cs`). When behavior seems “magical”, check patches before editing higher-level code.

## Key subsystems and patterns
- Options/config UI: use `Access/OptionMenuAccess.cs` plus `UI/Builder/OptionUIBuilder.cs`. Panels are injected into Drova’s existing options window when Harmony signals open/close. The builder writes directly into Drova config handlers, so UI creation usually has side effects.
- Save/load extensibility: `Systems/SaveGame/SaveGameSystem.cs` stores custom JSON blobs inside Drova save data by store key. Stores must be registered in `SystemInit.RegisterStores()`; current example is `Store/LazyActorStore.cs`.
- NPC spawning is module-based. `Systems/Spawning/NpcCreator.cs` composes `INpcModule` instances, sorts them by `Priority`, and applies them only during `Create()` / `CreateLazy()`. Reuse this builder instead of configuring `Actor` objects ad hoc.
- Lazy actors require a custom pre-init hook. The project does **not** use `LazyActor.ActorSpawnEvent`; `LazyActorPreInitRegistry.cs` + `Patches/ActorStartInitAsyncPatch.cs` exist because IL2CppInterop cannot marshal Drova’s non-blittable spawn args.
- External NPC definitions come from JSON under `Mods/Modding_API/NpcPlacement/*.json`; the wizard owns `wizard_placements.json`. See `Systems/Spawning/External/ExternalNpcPlacementSystem.cs` and `ExternalNpcModuleRegistry.cs` before changing spawn persistence.
- Dialogue edits are stored as serialized graph bytes, not bespoke DTOs. `Systems/Dialogues/Store/DialogueStore.cs` patches live `DialogueTree` instances in memory at main-menu load so vanilla references keep working.
- Localization is file-based. `LocalizationAccess.CreateLocalizationEntriesFromFolder()` copies files from `Mods/Modding_API/Localization/<Language>/...` into Drova’s localization folders at startup.

## Build / verify workflow
- Verified working command on this machine:
  - `dotnet build "C:\Users\fpabs\source\repos\Drova Modding API\Drova Modding API.sln" -c Debug`
- There are no automated tests yet (`Drova Modding API/Tests/` is empty), so use targeted `dotnet build` plus in-game validation for affected systems.

## Repo-specific conventions / gotchas
- `EnableDefaultCompileItems=false` in the `.csproj`: adding a new `.cs` file is not enough; you must add it to the correct `<Compile Include=...>` item group manually.
- Many editor/debug utilities are intentionally `#if DEBUG` or only included in the Debug item group (dialogue editors, global var inspector, dev dialogue audio helpers). Keep new debug-only files in the conditional item group or guard their usages.
- Code style is enforced from `.editorconfig`: 4 spaces, CRLF, block-scoped namespaces, explicit types preferred over `var`, nullable annotations enabled.
- Paths are intentionally tied to a local Drova install (`C:\Program Files (x86)\Steam\steamapps\common\Drova - Forsaken Kin\...`). Do not “simplify” these without checking build/package targets and developer expectations.
- Runtime data roots derive from `Utils.SavePath`, which is based on the built assembly location. That is why dialogue JSON, external NPC JSON, and localization folders all live under `Mods/Modding_API` next to the deployed DLL.
- You mostly never need to use Reflection, since il2cpp makes everything accessible.

## Useful entry points when editing
- `Core.cs` - mod bootstrap and scene transitions
- `Systems/SystemInit.cs` - gameplay vs AI scene system creation
- `Access/ProviderAccess.cs` - cached access to Drova managers/databases
- `Access/OptionMenuAccess.cs` + `UI/Builder/OptionUIBuilder.cs` - option panel injection pattern
- `Systems/Spawning/NpcCreator.cs` + `Systems/Spawning/Modules/*` - NPC composition model
- `Systems/Spawning/External/ExternalNpcPlacementSystem.cs` - JSON-driven NPC pipeline
- `Systems/SaveGame/*` - custom save integration
- `Systems/Dialogues/*` and `Patches/*` - graph editing/runtime patch points

## Debugging notes
- README-documented dev bindings still matter: `F6` opens the global var inspector in debug builds, and backquote/caret toggles cheat functionality depending on context. For gameplay issues, reproduce in a Debug build first.

## Documentation
- Always add documentation when appropriated, unless its public than always.
- Dont create unrequested *.md files without asking first.