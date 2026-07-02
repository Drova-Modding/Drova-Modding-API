# Localization

**What it does:** lets your mod ship translated text and reference it as a `LocalizedString`
that the game resolves to the player's current language. You can write `.loc` entries
programmatically or just drop language folders next to your mod, and you can even inject brand-new languages.

Entry point: `Drova_Modding_API.Access.LocalizationAccess` (static).

## Quick example

Register a couple of strings for your mod, then use them anywhere a `LocalizedString` is
expected (option labels, dialogue, UI):

```csharp
using Drova_Modding_API.Access;
using Il2CppCustomFramework.Localization; // ELanguage
using MelonLoader;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        LocalizationAccess.CreateLocalizationEntries(new List<LocalizationAccess.LocalizationEntry>
        {
            new("MyMod_Title",  "My Mod",        ELanguage.English),
            new("MyMod_Title",  "Mein Mod",      ELanguage.German),
            new("MyMod_Volume", "Volume",        ELanguage.English),
            new("MyMod_Volume", "Lautstärke",    ELanguage.German),
        }, modName: "MyMod");
    }
}

// Later, fetch a reference to a localized string:
LocalizedString title = LocalizationAccess.GetLocalizedString("MyMod", "MyMod_Title");
```

`GetLocalizedString(modName, key)` just builds a `LocalizedString(modName, key)` reference — it
resolves to actual text at display time based on the active language.

## How do I…?

### Ship translations as files instead of code

Drop `.loc` files into a language folder under the API's localization directory, and they're
copied into the game on startup. The folder layout is:

```
…/Drova - Forsaken Kin/Mods/Modding_API/Localization/
  English/MyMod_English.loc
  German/MyMod_German.loc
```

A `.loc` file is `key { value }` per entry:

```
MyMod_Title { My Mod }

MyMod_Volume { Volume }
```

On the main-menu scene the API scans `Modding_API/Localization/`, copies each language folder
into the game's own localization folders, and reloads the current language. If a folder is named
after a language the game doesn't know, that language is **injected automatically** (see below).

> `CreateLocalizationEntries(...)` (the code path) writes the same `.loc` format into the game's
> language folders for you and reloads — use whichever fits your workflow.

### Apply a localized string to a UI text component

```csharp
using Il2CppCustomFramework.Localization;

var localized = someGameObject.GetComponent<LocalizedTextMeshPro>();
localized._localizedString = LocalizationAccess.GetLocalizedString("MyMod", "MyMod_Title");
localized.UpdateLocalizedText();
```

### Add a brand-new language

```csharp
// Pick an int value above the highest existing ELanguage value.
LocalizationAccess.InjectLanguageEnum("Pirate", 36);
```

After injection, you can create entries with `(ELanguage)36` and the game will treat it as a
selectable language. (Dropping a `Pirate/` folder under `Localization/` does this injection for
you automatically.)

### Change the active language at runtime

```csharp
LocalizationAccess.SetLanguage(ELanguage.German);
```

### Edit `.loc` files in-game with the `loc_editor`

In a **`Debug`** build, the API ships a visual editor for your mod's localization files. Open the
cheats console (see [Cheat Menu](./cheat-menu.md)) and run:

```
loc_editor
```

This toggles a window that works directly on the files under
`Mods/Modding_API/Localization/<Language>/*.loc`. From it you can:

- **Browse** every `.loc` file found in the localization folder (with a filter box).
- **Edit, add, or delete** entries (`key { value }`) per file.
- **Create a new file** for a chosen language — pick from the known-language dropdown or type a
  custom code, set a base file name, and it writes `<name>_<language>.loc` into the right folder.
- **Apply Changes** — saves dirty files, copies them into the game's localization folders, and
  reloads the current language (the same path as `CreateLocalizationEntriesFromFolder`), so you see
  edits without restarting.
- **Select** an entry to hand its loca path + key off to other dev tools (e.g. autofilling a
  `LocalizedString` field in the dialogue/NPC tooling).

It's a faster authoring loop than editing files by hand and reloading: tweak text, hit **Apply
Changes**, and the active language refreshes live. The editor is debug-only — it isn't present in
`Release` builds.

## API reference

| Member                                                                            | Description                                                                                     |
|-----------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| `LocalizedString GetLocalizedString(string modName, string key)`                  | Build a `LocalizedString` reference for `(modName, key)`. Resolves to text at display time.     |
| `void CreateLocalizationEntries(List<LocalizationEntry> entries, string modName)` | Write entries (grouped by language) to the game's `.loc` files and reload the current language. |
| `void SetLanguage(ELanguage language)`                                            | Load/switch the active game language.                                                           |
| `void InjectLanguageEnum(string name, int value)`                                 | Add a new value to the `ELanguage` enum so a custom language is selectable.                     |
| `record LocalizationEntry(string Key, string Value, ELanguage Language)`          | One translation: key, text, and which language it belongs to.                                   |

## Notes & gotchas

- **Use a unique `modName`.** It becomes the `.loc` filename prefix (`<modName>_<Language>.loc`);
  collisions with another mod overwrite files.
- **File-based localization is loaded at the main menu.** Drop your folders in before launching;
  they're picked up on the `Scene_MainMenu` load.
- **Unknown language folders are auto-injected** as new `ELanguage` values — handy, but pick
  distinctive folder names to avoid clashing with real languages.
- `LocalizedString` is a lightweight reference, not the resolved text. The same reference shows
  different text when the player changes language.
