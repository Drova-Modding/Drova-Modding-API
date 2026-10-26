# Input

**What it does:** register your own key/button actions, let players rebind them in the options
menu, and read the game's own actions (keyboard and controller alike). Your actions and their
rebinds are saved and restored automatically.

Two classes:
- `InputActionRegister` (`Drova_Modding_API.Register`): **your mod's actions** (Unity Input System).
- `InputAccess` (`Drova_Modding_API.Access`): **the game's actions** ("Attack", "Jump", ...).

## Quick example

Register an action, then poll it each frame:

```csharp
using Drova_Modding_API.Register;
using MelonLoader;
using UnityEngine.InputSystem;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        InputActionRegister.OnInputActionsLoaded += () =>
        {
            InputAction? action = InputActionRegister.Instance.AddActionToGameplayMap("MyMod_Honk");
            action?.AddBinding("<Keyboard>/h");
        };
    }

    public override void OnUpdate()
    {
        InputAction? action = InputActionRegister.Instance?["MyMod_Honk"];
        if (action != null && action.WasReleasedThisFrame())
            MelonLogger.Msg("Honk!");
    }
}
```

Actions in the gameplay map are switched off in the main menu and the options menu, so they
don't fire while the player is in a menu.

## Let players rebind your action

Add a rebind row to your options page with `CreateInputActionSection`, pairing a label with the
action name:

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.UI.Builder;

OptionMenuAccess.Instance.GetBuilder("MyMod")!
    .CreateInputActionSection(new List<OptionUIBuilder.InputActionTemplate>
    {
        new(LocalizationAccess.GetLocalizedString("MyMod", "HonkLabel"), "MyMod_Honk"),
    })
    .Build();
```

## Your own action map

```csharp
InputActionMap map = new("MyModMap");
map.AddAction("Special", binding: "<Keyboard>/k");
InputActionRegister.Instance.RegisterInputActionMap(map);
```

Unlike the gameplay map, your own map is never switched off for menus. Enable and disable it yourself.

## Read the game's actions

```csharp
float move      = InputAccess.GetAxis("MoveHorizontal");
bool attackDown = InputAccess.IsActionClicked("Attack");
bool blockHeld  = InputAccess.IsActionPressed("Block");
string prompt   = InputAccess.GetLocalizedAnimText("Interact"); // button icon or key name
```

## Block game input while your window is open

```csharp
InputAccess.SetGameplayInputBlocked("MyWindow", true);
// ...
InputAccess.SetGameplayInputBlocked("MyWindow", false);
```

Input stays blocked while any window holds a block, so closing one window doesn't unblock input
another still needs. Prefer this over `ToggleGameplayActionMaps`, which ignores other windows.

## API reference

### `InputActionRegister` (`InputActionRegister.Instance`)

| Member                                             | Description                                                                             |
|----------------------------------------------------|-----------------------------------------------------------------------------------------|
| `static event Action OnInputActionsLoaded`         | Raised once the actions are loaded. Add your actions here.                              |
| `InputAction? AddActionToGameplayMap(string name)` | Add an action to the gameplay map. Returns `null` if the name is taken or `null`.       |
| `void RegisterInputActionMap(InputActionMap map)`  | Add a whole action map.                                                                 |
| `InputAction this[string actionName]`              | Get a gameplay-map action by name.                                                      |
| `const string GAMEPLAY_ACTION_MAP_NAME`            | `"gameplay"`.                                                                           |

### `InputAccess` (static)

| Member                                                                           | Description                                                           |
|----------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| `EDevice CurrentDevice`                                                          | Current input device (keyboard or controller).                        |
| `bool AnyButtonDown()`                                                           | Any input is down.                                                    |
| `float GetAxis(string actionName)`                                               | Axis value.                                                           |
| `bool IsActionClicked(string actionName)`                                        | True the frame the action was pressed.                                |
| `bool IsActionPressed(string actionName)`                                        | True while held.                                                      |
| `bool IsActionReleased(string actionName)`                                       | True the frame it was released.                                       |
| `string GetLocalizedAnimText(string actionName)`                                 | Button icon (controller/mouse) or key name (keyboard).                |
| `void SetGameplayInputBlocked(string owner, bool blocked)`                       | Hold or release a block on game input.                                |
| `void ToggleGameplayActionMaps(bool state)`                                      | Switch game input on/off directly.                                    |
| `void RebindKeyboardInputAction(string actionName, Pole?, AxisRange, Action<…>)` | Rebind a game action to the next key pressed; saved by the game.      |

## Gotchas

- **`InputActionRegister.Instance` is `null` during `OnInitializeMelon`.** Add actions in
  `OnInputActionsLoaded`.
- **`InputAccess` only knows the game's actions.** It can't read or rebind your `MyMod_*` actions;
  use the `InputAction` from `InputActionRegister` and `CreateInputActionSection` for those.
- Your actions and rebinds are saved to `Mods/Modding_API/actions.json` and `rebindings.json`
  when the options menu closes.
