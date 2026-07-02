# Input

**What it does:** lets your mod register its own input actions, read button/axis state (keyboard
and controller alike), rebind keys, and show native rebind rows in the options menu. Custom
actions are saved and restored automatically.

Entry points:
- `Drova_Modding_API.Register.InputActionRegister` — singleton (`InputActionRegister.Instance`)
  that owns the mod input-action asset and persists it.
- `Drova_Modding_API.Access.InputAccess` — static helpers for reading input and rebinding.

## Quick example

Register an action, then poll it each frame:

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.Register;
using MelonLoader;
using UnityEngine.InputSystem;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        // The register may not exist yet at OnInitializeMelon; subscribe to the load event.
        InputActionRegister.OnInputActionsLoaded += () =>
        {
            InputAction? action = InputActionRegister.Instance.AddActionToGameplayMap("MyMod_Honk");
            action?.AddBinding("<Keyboard>/h");
        };
    }

    public override void OnUpdate()
    {
        InputAction action = InputActionRegister.Instance?["MyMod_Honk"];
        if (action != null && action.WasReleasedThisFrame())
            MelonLogger.Msg("Honk!");
    }
}
```

Gameplay-map actions are automatically **enabled in gameplay** and **disabled in the main menu
and options menu**, so your bindings don't fire while the player is typing in a menu.

## How do I…?

### Read the game's existing actions

`InputAccess` wraps the game's input system, so you can query named actions without knowing
whether the player is on keyboard or controller:

```csharp
float move      = InputAccess.GetAxis("MoveHorizontal");
bool attackDown = InputAccess.IsActionClicked("Attack");
bool blockHeld  = InputAccess.IsActionPressed("Block");
bool jumpUp     = InputAccess.IsActionReleased("Jump");
bool anything   = InputAccess.AnyButtonDown();

var device = InputAccess.CurrentDevice; // keyboard vs controller
```

### Show a button prompt that adapts to the device

```csharp
// Returns an icon glyph for controller/mouse, or the key name for keyboard.
string prompt = InputAccess.GetLocalizedAnimText("Interact");
// e.g. set a tooltip: "Press {prompt} to open"
```

### Add your own action map (not just single actions)

```csharp
using UnityEngine.InputSystem;

var map = new InputActionMap("MyModMap");
map.AddAction("Special", binding: "<Keyboard>/k");
InputActionRegister.Instance.RegisterInputActionMap(map);
```

> Actions in the **gameplay** map are toggled with the game (disabled in menus). Actions in your
> own map are not auto-toggled — enable/disable them yourself.

### Let players rebind your action in the options menu

Add a rebound section with `OptionUIBuilder.CreateInputActionSection`, pairing a label with your
action name:

```csharp
using Drova_Modding_API.UI.Builder;

OptionMenuAccess.Instance.GetBuilder("MyMod")!
    .CreateInputActionSection(new List<OptionUIBuilder.InputActionTemplate>
    {
        new(LocalizationAccess.GetLocalizedString("MyMod", "HonkLabel"), "MyMod_Honk"),
    })
    .Build();
```

### Rebind a key from code

```csharp
InputAccess.RebindKeyboardInputAction("MyMod_Honk");
// The next key the player presses becomes the new binding; it's saved automatically.
```

### Disable gameplay input temporarily

```csharp
InputAccess.ToggleGameplayActionMaps(false); // e.g. while showing a custom UI
// ...
InputAccess.ToggleGameplayActionMaps(true);
```

## API reference

### `InputActionRegister` (singleton: `InputActionRegister.Instance`)

| Member                                             | Description                                                                                            |
|----------------------------------------------------|--------------------------------------------------------------------------------------------------------|
| `static event Action OnInputActionsLoaded`         | Raised once the register's action asset is created/loaded. Register actions here.                      |
| `InputAction? AddActionToGameplayMap(string name)` | Add an action to the auto-toggled gameplay map. Returns `null` if it already exists or `name` is null. |
| `void RegisterInputActionMap(InputActionMap map)`  | Add a whole action map (not auto-toggled).                                                             |
| `InputAction this[string actionName]`              | Index the gameplay map by action name.                                                                 |
| `const string GAMEPLAY_ACTION_MAP_NAME`            | `"gameplay"` — the auto-toggled map's name.                                                            |

### `InputAccess` (static)

| Member                                                                           | Description                                                      |
|----------------------------------------------------------------------------------|------------------------------------------------------------------|
| `EDevice CurrentDevice`                                                          | The current input device (keyboard/controller).                  |
| `bool AnyButtonDown()`                                                           | True if any input is down.                                       |
| `float GetAxis(string actionName)`                                               | Axis value for an action.                                        |
| `bool IsActionClicked(string actionName)`                                        | True the frame the action was pressed.                           |
| `bool IsActionPressed(string actionName)`                                        | True while held.                                                 |
| `bool IsActionReleased(string actionName)`                                       | True the frame it was released.                                  |
| `string GetLocalizedAnimText(string actionName)`                                 | Device-appropriate prompt (icon glyph or key name).              |
| `void ToggleGameplayActionMaps(bool state)`                                      | Enable/disable the gameplay input maps.                          |
| `void RebindKeyboardInputAction(string actionName, Pole?, AxisRange, Action<…>)` | Start a rebind; the next input becomes the binding and is saved. |

## Notes & gotchas

- **`InputActionRegister.Instance` may be `null` at `OnInitializeMelon`.** The register is
  created during late init; subscribe to `OnInputActionsLoaded` (or null-check) before adding
  actions.
- **Custom actions and rebinds persist** to `Modding_API/actions.json` and `rebindings.json`
  (next to the API DLL). They're saved when the options menu closes and reloaded on startup.
- **Gameplay-map actions are auto-disabled in menus.** That's by design so bindings don't fire
  while a menu is focused. Put always-on actions in your own map.
- Use the same action name string in `AddActionToGameplayMap`, when polling via `InputAccess`,
  and in an `InputActionTemplate` rebind row.
