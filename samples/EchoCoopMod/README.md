# Echo Coop Sample

The smallest mod that uses the API's [networking transport](../../docs/systems/networking.md). The
client sends an `EchoMessage` once connected, the host sends the same sequence back, both log it.

It exists to prove the packaging contract: this project references **`Drova_Modding_API.dll` and
`MelonLoader.dll` only**. There is no LiteNetLib reference anywhere in it, and the resulting DLL
runs unchanged against a plain or a coop-enabled API build — `NetworkAccess.IsSupported` is what
tells the two apart at runtime.

## Build

```sh
dotnet build samples/EchoCoopMod/EchoCoopMod.csproj -c Release
```

The `.csproj` points at the default Steam install path via the `GameDir` property; override it if
your game lives elsewhere:

```sh
dotnet build samples/EchoCoopMod/EchoCoopMod.csproj -c Release -p:GameDir="D:\Games\Drova - Forsaken Kin"
```

## Run

1. Build the API **with coop support** (`-p:Coop=true`) and install it, including
   `UserLibs/LiteNetLib.dll`.
2. Drop `EchoCoopMod.dll` into the game's `Mods` folder.
3. Start the game once so MelonLoader writes `UserData/MelonPreferences.cfg`, then set the
   `EchoCoopSample` category:

   ```ini
   [EchoCoopSample]
   Mode = "host"      # or "client"
   Address = "127.0.0.1"
   Port = 9050
   ```

4. Load a savegame on both instances. The session starts when
   `Scene_Gameplay_Main` loads, and the round trip shows up in the MelonLoader console.

Two instances on one machine work if they use different `Mode` values and the client points at
`127.0.0.1`.
