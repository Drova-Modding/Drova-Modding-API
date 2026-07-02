# Cheat Menu

**What it does:** registers custom commands into Drova's developer console/terminal so you (or
your mod's users in dev/debug) can run mod actions by typing them. Useful for testing, spawners,
and debug toggles.

Entry point: `Drova_Modding_API.Access.CheatMenuAccess` (static).

## Quick example

```csharp
using Drova_Modding_API.Access;
using Il2CppCommandTerminal;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        CheatMenuAccess.RegisterCheat(
            name: "mymod_hello",
            action: new Action<Il2CppReferenceArray<CommandArg>>(args =>
            {
                MelonLogger.Msg("Hello from my mod!");
            }),
            minArgs: 0,
            maxArgs: 0,
            help: "Prints a greeting",
            hint: "mymod_hello");
    }
}
```

Open the console (backquote `` ` `` in a debug build) and type `mymod_hello`.

## How do I…?

### Read command arguments

`CommandArg` carries the typed value of each argument the user typed:

```csharp
CheatMenuAccess.RegisterCheat("mymod_give", new Action<Il2CppReferenceArray<CommandArg>>(args =>
{
    // args[0] is the first argument after the command name
    int amount = args[0].Int;       // also: .Float, .Bool, .String
    GiveGold(amount);
}), minArgs: 1, maxArgs: 1, help: "Give gold", hint: "mymod_give <amount>");
```

`minArgs`/`maxArgs` let the terminal validate argument count before your action runs.

### Register before cheat mode is on

`RegisterCheat` is safe to call any time. If cheat mode isn't active yet, the command is
**queued** and registered automatically once cheat mode turns on. The return value tells you
 what happened:

```csharp
bool registeredNow = CheatMenuAccess.RegisterCheat(/* … */);
// true  = registered immediately (cheat mode already on)
// false = queued; will register when cheat mode is enabled
```

So registering in `OnInitializeMelon` works even though cheat mode starts later.

### Turn cheat mode on/off from code

```csharp
CheatMenuAccess.ToggleCheatMenu(true);  // enable
CheatMenuAccess.ToggleCheatMenu(false); // disable
```

### Run a command programmatically

```csharp
CheatMenuAccess.FireCommand("mymod_give 100");
```

## API reference

| Member                                                                                                                                           | Description                                                                                                          |
|--------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `bool RegisterCheat(string name, Action<Il2CppReferenceArray<CommandArg>> action, int minArgs, int maxArgs, string help = "", string hint = "")` | Register a console command. Returns `true` if registered immediately, `false` if queued until cheat mode is enabled. |
| `void ToggleCheatMenu(bool toggle)`                                                                                                              | Enable/disable cheat mode.                                                                                           |
| `void FireCommand(string command)`                                                                                                               | Execute a command string as if typed by the user.                                                                    |

## Notes & gotchas

- **Cheat mode is auto-enabled in debug builds** of the API (and toggled with `` ` ``). In a
  release build, call `ToggleCheatMenu(true)` or rely on the queue and the user enabling it.
- **Queued registration is the norm.** Don't treat a `false` return as failure — your command
  will appear once cheat mode is on.
- Give commands a unique, mod-prefixed `name` to avoid clashing with the game's built-ins or
  other mods.
- For a richer in-game NPC-building UI rather than raw commands, see the NPC wizard in
  [External NPCs](./external-npcs.md).
