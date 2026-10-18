# Automated in-game testing

Drive one or more real Drova instances from a script and check what their worlds hold: load a save, walk, teleport,
read and set global variables, talk, fight, assert. `TestAgent` runs inside the game (every build of the API, inert
unless the game is started with `--agent-port`); `Drova.TestRunner` launches the games and plays a scenario file
against them.

## Getting started

1. **Get the runner.** Download `Drova_TestRunner_win-x64.zip` or `Drova_TestRunner_linux-x64.zip` from the API
   release (needs the [.NET 8 runtime](https://dotnet.microsoft.com/download/dotnet/8.0); on Linux
   `chmod +x Drova.TestRunner` after unzipping), or run it from source:
   `dotnet run --project "tools/Drova.TestRunner" -- <scenario>`.
2. **Install the API and your mod** in the game as usual. Nothing else is needed in the game.
3. **Game path.** The runner finds Drova through Steam (the registry or the default install on Windows,
   `~/.steam/steam` or `~/.local/share/Steam` on Linux, then every library in `libraryfolders.vdf`). Pass
   `--game <dir>` when it lives elsewhere.
4. **Give each instance a save.** `launch A` starts the game with `--profile A`, which keeps its saves in
   `Sprint_24_A` next to the normal `Sprint_24` folder and its settings in `config_A.j2d`. The save root is
   `%USERPROFILE%\AppData\LocalLow\Just2D\Drova` on Windows and `~/.config/unity3d/Just2D/Drova` on Linux. Copy
   `Sprint_24` (or one `Hero_NN` folder of it) to `Sprint_24_A`, `Sprint_24_B`, ... once. `load 5 0` loads
   `Hero_05/Savegame_00`.
5. **Always `wait saves=separate` before `load`.** It says the profile redirect really applied; without it the
   instance would write your real saves.

## Writing scenarios

One step per line, `#` starts a comment (so never put `#` in a value). Name a file `.scn` when it runs one game and
`.coop` when it runs two or more; shared fragments for `include` are `.part`.

```
launch A
A wait ready=1 timeout=240
A wait saves=separate timeout=1
A load 5 0
A wait world=1 timeout=240
A setgvar Mage_mod ChoseMagePath 1
A expect gvar Mage_mod ChoseMagePath == 1 timeout=5
```

| Step | Does |
|---|---|
| `launch A [game args]` | Starts the game as profile A with its agent port (A 47101, B 47102, ...) and waits for the agent. Extra args go to the game, e.g. `--net-delay 40-120`. |
| `attach A <port>` | Uses a game you started yourself with `--profile X --agent-port <port>`. |
| `A <command>` | Sends an agent command; anything but an `ok` answer fails the scenario. |
| `A ?<command>` | Sends it and ignores a failing answer. |
| `A wait key=value [timeout=s]` | Polls `state` until it holds; `*` means anything but `-`. |
| `A expect <command> == text [timeout=s]` | Polls the command until its answer matches. Also `!=`, `contains`, `!contains`, `>= n`, `<= n` (first number in the answer). Default timeout 30 s. |
| `capture name A <command>` | Keeps the answer as `$name`, to expect it on another game. |
| `sleep <s>` | Waits. Prefer `expect ... timeout=`. |
| `set name value` / `default name value` | A variable; `default` loses to `-D name=value`. `$name` is replaced, `$A.key` is that key from A's last `state`. |
| `include <file>` | That file's lines, relative to this one. |
| `track <guid>` | Records that actor's `ai` readout from every game (see Recordings). |

## Running

```
Drova.TestRunner tests/scenarios/mage.scn       # one scenario
Drova.TestRunner tests/scenarios                # every .scn and .coop in the folder (not .part, not subfolders)
Drova.TestRunner tests/scenarios/SMOKE.txt      # a list
```

A list has one scenario per line, `#` comments, absolute or relative to the list; a name without extension is
tried as `.coop`, then `.scn`. Between scenarios the runner closes the games it launched. A folder or list ends with
a summary (passed/failed, time, the first failing line); the exit code is 0 only when everything passed.

| Option | Does |
|---|---|
| `--game <dir>` | The game folder, when Steam detection does not find it. |
| `--keep` | Leaves the games running at the end. In a folder or list run, stops at the first failure and keeps its games. |
| `--no-record` | No screenshots and state recording. |
| `-D name=value` | Sets a variable; wins over `default`. |
| `--language <code>` | Writes the game's `language.txt` for the run (default `en`) and puts the old content back afterwards, also on failure or Ctrl+C. `keep` leaves it alone. |

## Built-in agent commands

`<target>` is a guid, a name (nearest living match) or `me`. Answers start with `ok` or `err`.

| Command | Does |
|---|---|
| `ping` | `pong`. |
| `state` | `key=value` line: `ready` (menu idle, loads accepted), `world` (world up 2 s), `pos`, `profile`, `saves`, `gameplay`, `loadingscreen`, `pendingload`, `timescale`, `focused`, `cam`, `teleporting`, `actlog`, plus mod keys. |
| `run <console command>` | Runs a cheat-console command; answers what it reported. |
| `log <text>` | Writes to the MelonLoader log. |
| `load <hero> <slot>` | Loads a save; retried until `world=1`. |
| `save <slot>` | Saves into that slot of the current hero (only with `--profile`). For fixture saves. |
| `gvar <List> <Var>` / `setgvar <List> <Var> <value>` | Reads / writes a global variable. |
| `time` | Game seconds and hour of day. |
| `tp <x> <y>` / `tp x,y` | Teleports the player. |
| `tpnear <target> <dx> [dy]` | Teleports next to an actor. |
| `tpinto <volume fragment>` | Teleports into a trigger volume's shape. |
| `walk <dx> <dy> <seconds>` / `walked` | Walks in a direction / distance walked since. Assert `walked >= N`. |
| `walkto <x,y\|target> [seconds] [stop]` | Walks the NPCs' A* path (local graph only: teleport closer first). |
| `near <target> <range>` | Whether it is within range, the distance, whether it is loaded. |
| `find <name>` | Authored actors by spawner name, as `name=guid;`. |
| `actors [range]` | Living bodies nearby, `name@distance:health`. |
| `ai <target>` | AI readout: routine, position, action, target, dialogue state, ko/brawl/dead. |
| `port <target>` | Sends an NPC to its routine place. |
| `dead <target>` / `kill <target>` | Is it dead here / kills it the ordinary way. |
| `hit <target> <amount> [physical\|pure\|magical] [nosource\|striker]` | A blow from the player (aggroes, credits the kill), from nobody, or from the named actor. |
| `health <target> [cur\|max]` | `current/max`, or one number. |
| `effects <target>` / `effect list <frag>` / `effect add <name>` / `effect remove <name>` | Status effects. |
| `talents <target> [frag]` / `talent learn <name>` / `talent list <frag>` | Talents. |
| `critstop on\|off` | The game's no-crit cheat. |
| `give <item> [n]` / `items <item>` / `inventory [frag]` / `itemsearch <frag>` | Items by readable id. |
| `equip <item>` / `unequip` / `equipped` | Gear. |
| `useitem <item> [hold]` | Uses an item as the quick slots do (potion, food, spell). |
| `press <bind> [hold] [delay]` | Presses an action bind (`PrimaryAttack`, `SecondaryAttack`, `Dodge`, ...). |
| `stance <mode>` | `Combat`, `Squat`, `Normal`, ... |
| `look <x,y\|target\|off>` | Faces and aims there. |
| `actlog start <me\|name>` / `actlog read` / `actlog brief` | Records what a body plays, to compare two machines. |
| `use <name frag> [range]` | Interacts like the key: bed, chest, pickup, work spot (minigames skipped). |
| `interactables <frag\|*> [range]` | Interactables nearby. |
| `chest <guid>` / `takeall` | A container's contents / "take all" on the open loot window. |
| `place <item> [range]` | Puts an item on the nearest item place (puzzle plinth). |
| `perceive <frag\|*> [range]` | Makes hidden perception objects usable. |
| `talk <target> [tree]` | Starts a conversation, or a dialogue tree directly. |
| `dialogue` | `in=`, the line said, `choices=i:text \| ...`, `node=`. |
| `choose <index\|text>` / `next` / `skip` / `skip first` / `bye` | Answer, next line, skip to the choices (or to the end, taking answer 0), stop the conversation. |
| `popup [yes\|no]` | A confirm popup's text, or answer it. |
| `scenes [frag] [rearm]` | World-started dialogue observers; `rearm` lets a fired one start again. |
| `journal [frag]` | Journal entries by resource path. |
| `counters [frag]` | Kill-counting quest observers, `name:killed/total`. |
| `inside <volume frag>` | Whether the player is inside that trigger volume. |
| `objects <frag> [list]` | How many active objects match; `list` includes inactive ones as `parent/name=1\|0`. |
| `colliders x,y [radius]` | Colliders at a spot. |
| `camera [reset]` | What the camera follows; `reset` hands it back to the player. |
| `weather` / `foglets [range]` | Weather levels and fog / nearby foglet patches. |
| `worldevent` / `worldevent start <name>` / `worldevent region <A,B>` | World events. |
| `merchant [take\|give <item> [n]\|accept\|close]` | Drives the merchant window. |
| `pause` / `click <GameObject name>` | Opens the pause menu / presses an active UI button (`ModButton_<id>` for `PauseMenuAccess`). |
| `realinput on\|off` | Gives keyboard and mouse back (off while the agent runs). |
| `shot <file.png>` | Screenshot. |
| `netstats` | Sent network traffic per message id. |
| `audioclip <actor> <clip>` / `clipalive` / `unloadassets` | Dialogue clip lookup and Unity's unused-asset sweep. |

## Adding your own commands

In `OnInitializeMelon` (harmless when the agent is off). A handler that throws answers `err <message>`.

```csharp
TestAgent.RegisterCommand("mage_level", rest => MageState.Level.ToString());
TestAgent.RegisterState(() => $"mana={MageState.Mana}");
```

| Member | Purpose |
|---|---|
| `TestAgent.RegisterCommand(name, fn)` | `fn` gets the rest of the line and returns the answer; runs on the main thread. |
| `TestAgent.RegisterState(fn)` | Adds `key=value` pairs (no spaces in values) to `state`. |
| `TestAgent.Report(line)` | Makes a console line readable by `run`. |
| `TestAgent.IsActive` | Whether this game was started with `--agent-port`. |
| `LaunchProfile.Name` / `.Separate` | The `--profile` name, and whether saves really moved. |
| `LaunchProfile.UserDataFolder(name)` | A per-profile UserData folder name, for per-save mod files. |

## Recordings

Every run records each game: a screenshot and `state` every 2 s, and `track`ed actors' `ai` every 4 s. A failed
scenario keeps it in `failed-logs/<run>/record` next to the scenario, together with each game's MelonLoader log; the
last five passed runs are in `recordings/<scenario>`. `--no-record` switches it off.

## Troubleshooting

- **`dialogue` expectations see other text**: the game runs in `language.txt`'s language. The runner sets `en` for
  the run; pick another with `--language`.
- **Input**: while the agent runs, the real keyboard and mouse do nothing in that game (a launched game takes
  focus). `realinput on` gives them back for poking at a `--keep` game.
- **A game stalls**: check `focused` and `timescale` in `state`; the agent keeps unfocused games ticking.
- **Timeouts**: startup and loads take minutes; give `ready` and `world` waits `timeout=240`. A launch whose agent
  does not answer in 3 minutes is restarted once.
- **Agent never answers**: the API is not installed in that game folder, or the game crashed on start (read
  `MelonLoader/Latest.log`). With `attach`, the game must have been started with `--agent-port`.
- **`saves=shared`**: the profile redirect did not apply, or the game was started without `--profile`. Do not load
  anything; check the log for `[Profile]`.
- **A walk goes nowhere**: walls stop it silently. Assert `walked >= N`.
- The agent listens on 127.0.0.1 only and runs requests on the Unity main thread.
