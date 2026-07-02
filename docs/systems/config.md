# Config

**What it does:** reads back the values your [options-menu](./options-menu.md) controls saved.
Every slider/switch/dropdown stores its value in the game's gameplay config under the
`optionKey` you chose; `ConfigAccessor` is how you fetch it later as a strongly typed value.

Entry point: `Drova_Modding_API.Access.ConfigAccessor` (static).

## Quick example

```csharp
using Drova_Modding_API.Access;

if (ConfigAccessor.TryGetConfigValue<int>("MyMod_Volume", out int volume))
{
    // Use the persisted value the player set in the options menu.
    ApplyVolume(volume);
}
else
{
    // Key not present yet (menu never opened, or game still bootstrapping).
    ApplyVolume(50); // your default
}
```

## How do I…?

### Read each supported type

`TryGetConfigValue<T>` supports `float`, `int`, `bool`, `string`, and any `enum`:

```csharp
ConfigAccessor.TryGetConfigValue<bool>("MyMod_Enabled", out bool enabled);
ConfigAccessor.TryGetConfigValue<float>("MyMod_Scale", out float scale);
ConfigAccessor.TryGetConfigValue<string>("MyMod_Name", out string name);

enum Quality { Low, Medium, High }
ConfigAccessor.TryGetConfigValue<Quality>("MyMod_Quality", out Quality quality);
```

The `optionKey` must match exactly the key you passed to the builder's `Create*` method.

### Read after the player closes the menu

The cleanest moment to refresh the runtime state is when the option window closes:

```csharp
OptionMenuAccess.Instance.OnOptionMenuClose += () =>
{
    if (ConfigAccessor.TryGetConfigValue<int>("MyMod_Volume", out int v))
        ApplyVolume(v);
};
```

## API reference

| Member                                               | Description                                                                                                                                                                                                             |
|------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `bool TryGetConfigValue<T>(string key, out T value)` | Reads the config value for `key`. `T` may be `float`, `int`, `bool`, `string`, or an `enum`. Returns `false` (and `value = default`) if the key is missing, the type doesn't match, or the game is still bootstrapping. |

## Notes & gotchas

- **Always check the return value.** It returns `false` while `DrovaResourceProvider` /
  `ConfigGameHandler` aren't ready yet (very early startup), as well as for missing keys. Fall
  back to your own default.
- **Keys are shared with the option builder.** The value only exists once a control with that
  key has been created (or previously saved). See [Options Menu](./options-menu.md).
- Values are stored as strings under the hood; `ConfigAccessor` parses them into `T` for you.
- This is read-only. To change a value, drive it through an options-menu control (which writes
  and persists it).
