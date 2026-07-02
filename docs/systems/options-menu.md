# Options Menu

**What it does:** lets your mod add its own panel to Drova's in-game options window and fill it
with native-looking controls — titles, sliders, switches, dropdowns, buttons, and key-rebind
rows. Control values are persisted to the game's config file automatically, so you read them
back later with [Config](./config.md).

Entry points:
- `Drova_Modding_API.Access.OptionMenuAccess` — singleton (`OptionMenuAccess.Instance`) that
  manages panels and fires open/close events.
- `Drova_Modding_API.UI.Builder.OptionUIBuilder` — the fluent builder you use to create the
  rows in a panel.

> **Localization:** control labels take a `LocalizedString`, not a raw string. Create them with
> `LocalizationAccess.GetLocalizedString("MyMod", "Key")` and ship the text via the
> [Localization](./localization.md) system. The examples below assume you've done that.

## Quick example

Add a panel to the shared "Modding" tab and put a slider in it. Build the UI when the option
window opens:

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.UI.Builder;
using MelonLoader;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        OptionMenuAccess.Instance.OnOptionMenuOpen += BuildOptions;
    }

    private void BuildOptions()
    {
        // Get a builder for your section of the shared "Modding" panel.
        OptionUIBuilder? builder = OptionMenuAccess.Instance.GetBuilder("MyMod");
        if (builder == null) return; // already added this frame, or panel unavailable

        builder
            .CreateTitle(LocalizationAccess.GetLocalizedString("MyMod", "Title"))
            .CreateSlider(LocalizationAccess.GetLocalizedString("MyMod", "Volume"),
                          optionKey: "MyMod_Volume", min: 0, max: 100, defaultValue: 50)
            .Build();
    }
}
```

`optionKey` is the string under which the value is stored in the game config — choose a unique,
prefixed key per option, and use the same key when you read it back with
[`ConfigAccessor`](./config.md).

## How do I…?

### Create the different control types

All builder methods return the builder, so you chain them and finish with `Build()`:

```csharp
OptionMenuAccess.Instance.GetBuilder("MyMod")!
    .CreateTitle(L("Title"))
    .CreateHeader(L("SectionHeader"))
    .CreateDisclaimer(L("SomeExplanatoryText"))
    .CreateSwitch(L("EnableFeature"), L("On"), L("Off"), optionKey: "MyMod_Enabled", defaultValue: true)
    .CreateSlider(L("FloatValue"), optionKey: "MyMod_Scale", min: 0f, max: 2f, defaultValue: 1f, wholeNumbers: false)
    .CreateButton(L("ResetLabel"), L("ResetButton"), onClick: () => MelonLogger.Msg("clicked!"))
    .Build();

// helper to keep examples short:
static LocalizedString L(string key) => LocalizationAccess.GetLocalizedString("MyMod", key);
```

There are two `CreateSlider` overloads: an **integer** one (`int min/max/default`) and a
**float** one (`float min/max/default` plus `wholeNumbers`). The compiler picks based on whether
you pass `int` or `float` literals.

### Create an enum dropdown

`CreateDropdown<E>` maps enum values to localized labels:

```csharp
enum Quality { Low, Medium, High }

OptionMenuAccess.Instance.GetBuilder("MyMod")!
    .CreateDropdown(
        L("QualityLabel"),
        optionKey: "MyMod_Quality",
        dropdownOptions: new Dictionary<Quality, LocalizedString>
        {
            { Quality.Low,    L("QualityLow") },
            { Quality.Medium, L("QualityMedium") },
            { Quality.High,   L("QualityHigh") },
        },
        defaulValue: Quality.Medium)
    .Build();
```

Read the selected value back with `ConfigAccessor.TryGetConfigValue<Quality>("MyMod_Quality", out var q)`.

### React when a slider/switch changes

`Build()` returns the created `GameObject`s in the order you created them, so you can wire up
listeners. The layout of a slider row contains a Unity `Slider` and a `GUI_ConfigOption_Slider`:

```csharp
using UnityEngine.UI;
using Il2CppDrova.GUI.Options;

List<GameObject> built = OptionMenuAccess.Instance.GetBuilder("MyMod")!
    .CreateTitle(L("Title"))                                   // index 0
    .CreateSlider(L("Min"), "MyMod_Min", 1, 120, 30)           // index 1
    .CreateSlider(L("Max"), "MyMod_Max", 2, 360, 60)           // index 2
    .Build();

if (built.Count < 3) return;

Slider minSlider = built[1].GetComponentInChildren<Slider>();
minSlider.onValueChanged.AddListener(new Action<float>(value =>
{
    // Run your own logic whenever the user drags the slider.
    ApplyRuntimeChange((int)value);
}));
```

> The value is already persisted by the builder; you only need a listener if you want to react
> live (e.g., clamp two sliders against each other or apply a change immediately).

### Add a dedicated tab instead of using the shared panel

`GetBuilder(string id)` adds to the shared "Modding" tab. To create your own tab with its own
icon, call `AddPanel` and pass the returned `Transform` to the static builder factory:

```csharp
// Name MUST start with "GUI_Button_OptionTab_"
Transform? panel = OptionMenuAccess.Instance.AddPanel(myIconSprite, "GUI_Button_OptionTab_MyMod");
if (panel != null)
{
    OptionUIBuilder builder = OptionMenuAccess.GetBuilder(panel);
    builder.CreateTitle(L("Title")).Build();
}
```

Panels are **not persistent** — they're rebuilt each time the option window opens, which is why
you build them inside an `OnOptionMenuOpen` handler.

### Save your settings when the menu closes

```csharp
OptionMenuAccess.Instance.OnOptionMenuClose += () =>
{
    // Persist your own derived state, refresh runtime values, etc.
};
```

## API reference

### `OptionMenuAccess` (singleton: `OptionMenuAccess.Instance`)

| Member                                               | Description                                                                                                                                         |
|------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| `OptionUIBuilder? GetBuilder(string id)`             | Get a builder for your section of the shared "Modding" panel. Returns `null` if `id` was already added this open-cycle or the panel is unavailable. |
| `static OptionUIBuilder GetBuilder(Transform panel)` | Get a builder targeting a specific panel `Transform` (from `AddPanel`).                                                                             |
| `Transform? AddPanel(Sprite? icon, string name)`     | Add a new tab + panel. `name` must start with `GUI_Button_OptionTab_`. Returns the content `Transform` or `null`.                                   |
| `bool IsMenuOpen { get; }`                           | Whether the options window is currently open.                                                                                                       |
| `event OptionMenuAction OnOptionMenuOpen`            | Fired when the options window opens (pause menu or main menu). Build your UI here.                                                                  |
| `event OptionMenuAction OnOptionMenuClose`           | Fired when the options window closes. Good place to save.                                                                                           |
| `GameObject? GetGUIWindow()`                         | The live options window `GameObject`, or `null`.                                                                                                    |
| `static GameObject GetControlsPanel()`               | The native controls/keybindings panel.                                                                                                              |

### `OptionUIBuilder` (fluent; every `Create*` returns the builder)

| Method                                                                                                                     | Creates                                                                                     |
|----------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| `CreateTitle(LocalizedString)`                                                                                             | A title row.                                                                                |
| `CreateHeader(LocalizedString)`                                                                                            | A centered section header.                                                                  |
| `CreateDisclaimer(LocalizedString)`                                                                                        | A description/disclaimer paragraph.                                                         |
| `CreateSlider(LocalizedString title, string key, int min, int max, int default)`                                           | An integer slider.                                                                          |
| `CreateSlider(LocalizedString title, string key, float min, float max, float default, bool wholeNumbers)`                  | A float slider.                                                                             |
| `CreateSwitch(LocalizedString title, LocalizedString on, LocalizedString off, string key, bool default, bool useGreyText)` | A toggle switch.                                                                            |
| `CreateDropdown<E>(LocalizedString title, string key, Dictionary<E, LocalizedString> options, E default)`                  | An enum dropdown (`E : Enum`).                                                              |
| `CreateButton(LocalizedString title, LocalizedString buttonName, Action onClick)`                                          | A button row.                                                                               |
| `CreateInputActionSection(List<InputActionTemplate>)`                                                                      | A section of key-rebind rows (see [Input](./input.md)).                                     |
| `List<GameObject> Build()`                                                                                                 | Finalizes the panel, saves the config file, and returns the created rows in creation order. |

`record InputActionTemplate(LocalizedString Title, string ActionName)` pairs a rebound row's
label with the input action name it controls.

## Notes & gotchas

- **Build inside `OnOptionMenuOpen`.** Panels are torn down and rebuilt each time the window
  opens; building once in `OnInitializeMelon` won't stick.
- **`GetBuilder(id)` returns `null` on repeat calls** within the same open-cycle — that's the
  dedup guard, not an error. Bail out quietly.
- **Labels are `LocalizedString`s.** Provide the text through [Localization](./localization.md).
- **Values persist automatically.** The builder writes to the game's gameplay config and
  `Build()` saves the file. Read values back with [Config](./config.md).
- The API itself disables gameplay input actions while the options menu is open and re-enables
  them on close — see [Input](./input.md).
