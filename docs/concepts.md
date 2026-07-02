# Core Concepts

A short tour of the things that are true across the whole API. Read this once and the
per-system pages will make more sense.

## MelonLoader & IL2CPP in one paragraph

Drova ships as a Unity **IL2CPP** game: its C# was compiled to native code, then MelonLoader +
[Il2CppInterop](https://github.com/BepInEx/Il2CppInterop) generate managed proxy assemblies
(`Il2CppDrova`, `Il2CppCustomFramework`, …) so you can call game types from a normal .NET mod.
Those proxies behave _almost_ like regular C#, with a few sharp edges (see
[gotchas](#il2cpp-gotchas)). This API wraps the most common of those types behind friendly
facades in the `Drova_Modding_API.Access` and `Drova_Modding_API.Systems` namespaces, so you
rarely touch the raw `Il2Cpp*` classes or reflection directly.

## The mod lifecycle

Your `MelonMod` receives lifecycle callbacks. The two you'll use most:

| Callback                        | When                          | Use it for                                                                                  |
|---------------------------------|-------------------------------|---------------------------------------------------------------------------------------------|
| `OnInitializeMelon()`           | Once, before any scene loads. | Registering things that must exist up front (option-menu hooks, save stores, world events). |
| `OnSceneWasLoaded(int, string)` | Every scene load.             | Reacting to entering the main menu or gameplay. Gate your logic on the scene name.          |

The API itself is **scene-driven**: it spins systems up and down as scenes load. Scene names
are available as constants on `Drova_Modding_API.GlobalFields.SceneNames`:

| Constant                     | Value                 | Meaning                                                                  |
|------------------------------|-----------------------|--------------------------------------------------------------------------|
| `SceneNames.MainMenu`        | `Scene_MainMenu`      | Main menu. Localization & custom gvars get loaded here.                  |
| `SceneNames.GameplayMain`    | `Scene_Gameplay_Main` | The player, camera, and `GameManager` live here. Gameplay systems start. |
| `SceneNames.AILogic`         | `Scene_AILogic`       | AI logic scene (routines are parented here).                             |
| `SceneNames.Actor`           | `Scene_Actors`        | Actors scene; spawned NPCs and JSON placements are created here.         |
| `SceneNames.OptionSceneName` | `RuntimeScene_GUI`    | Options window, cheat console, and HUD.                                  |

A typical gameplay session loads `Scene_MainMenu` first, then `Scene_Gameplay_Main` plus the
supporting scenes. Many "why didn't my call work?" issues come down to running too early — see
the timing note below.

### Timing: where to put your code

- **Add option-menu panels / hook `OnOptionMenuOpen`** — set up the subscription in
  `OnInitializeMelon`; the panel is (re)built when the option window opens. See
  [Options Menu](./systems/options-menu.md).
- **Touch the player** — the player doesn't exist in menus and isn't ready for the instant gameplay
  loads. Use `PlayerAccess.OnPlayerFound` instead of grabbing it immediately. See
  [Game Access](./systems/game-access.md).
- **Spawn NPCs into the world** — wait until gameplay/actor scenes are live. Lazy actors (see
  [Spawning](./systems/spawning.md)) are the safe, save-friendly way to do this.
- **Reading game managers** — `ProviderAccess` resolves handlers on demand and may return
  `null` while the game is still bootstrapping. Always null-check.

## Where the API reads and writes files

All runtime data lives in a `Modding_API` folder **next to the deployed API DLL** (derived from
`Utils.SavePath`). In a normal installation that is:

```
…/Drova - Forsaken Kin/Mods/Modding_API/
  Localization/<Language>/…        ← your .loc files (see Localization)
  NpcPlacement/*.json              ← JSON-defined NPCs (see External NPCs)
  GlobalVars/*.json                ← custom global variables (see Global Variables)
  Audio/…                          ← dialogue audio files/bundles (see Audio)
  actions.json / rebindings.json   ← saved input actions & rebinds (see Input)
```

Mod-authored content (localization, NPC placement, gvars, audio) is **discovered by scanning
these folders at startup**, so shipping content is often just a matter of dropping a file in
the right place. Each per-system page documents its own folder and file format.

## IL2CPP gotchas

A few things that will bite you if you don't know them:

- **Casting:** use `TryCast<T>()` (returns `null` on mismatch) or `Cast<T>()` (throws) instead
  of C# pattern casts when converting between IL2CPP types.
- **Loading addressable assets:** prefer the **non-generic** `LoadAsset` + `TryCast<T>` over the
  generic `LoadAsset<T>` / `Cast<T>`, which can throw a `NullReferenceException` on a missing
  asset. See [Addressables](./systems/addressables.md).
- **Delegates:** when a game API wants a delegate, wrap your method in the matching
  `Il2CppSystem.Action<…>` / `System.Action<…>` as the surrounding code does. The API hides this
  for you in most places.
- **Nullable structs:** the game sometimes wants `Il2CppSystem.Nullable<T>` rather than C#'s
  `T?`. Helper methods in the API convert where needed.
- **You rarely need reflection** — IL2CPP exposes internal fields (often prefixed with `_`)
  directly, which is why API code reads things like `handler.GameplayConfig._configFile`.

## Naming you'll see a lot

- **Handlers / Managers** — the game's subsystems (`EntityGameHandler`, `DialogueSystemGameHandler`,
  …), reached through [`ProviderAccess`](./systems/game-access.md).
- **Addressables / `AssetReference`** — assets referenced by GUID and loaded on demand;
  the API ships ready-made references in [`AddressableAccess`](./systems/addressables.md).
- **Readable IDs** — human-readable string IDs for items/stats/talents, available as constants
  under `Drova_Modding_API.GlobalFields` (`ItemReadableIds`, `StatReadableIds`, `TalentReadableIds`).
- **Lazy actors** — NPCs that are saved as lightweight metadata and re-spawned on a load; the
  backbone of persistent custom characters.

Ready to build something? Pick a system from the [docs index](./README.md).
