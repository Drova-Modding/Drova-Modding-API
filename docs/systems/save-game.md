# Save Game

**What it does:** lets your mod store custom data **inside Drova's own save files**. You register
a *store*; the system serializes it to a JSON string and tucks it into the save under a key when
the game saves and hands the string back to you when the game loads. There are also lifecycle
events around saving/loading.

Entry points:
- `Drova_Modding_API.Systems.SaveGame.SaveGameSystem` — singleton (`SaveGameSystem.Instance`).
- `IStorable` / `IStore<T>` (in `…SaveGame.Store`) — the store contracts.

## Quick example

A typed store that persists a list of your own records:

```csharp
using Drova_Modding_API.Systems.SaveGame.Store;
using System.Text.Json;

public sealed class HighScoreStore : IStore<int>
{
    private readonly List<int> _scores = [];

    public string SaveGameKey => "MyMod_HighScores"; // unique per mod

    public string Save() => JsonSerializer.Serialize(_scores);

    public void Load(string result)
    {
        _scores.Clear();
        if (!string.IsNullOrWhiteSpace(result))
            _scores.AddRange(JsonSerializer.Deserialize<List<int>>(result) ?? []);
    }

    public void Add(int item) => _scores.Add(item);
    public void AddRange(int[] items) => _scores.AddRange(items);
    public void Remove(int item) => _scores.Remove(item);
    public void Clear() => _scores.Clear();
    public int Get(int index) => _scores[index];
    public IEnumerable<int> GetAll() => _scores;
}
```

Register it once, up front:

```csharp
using Drova_Modding_API.Systems.SaveGame;

public override void OnInitializeMelon()
{
    SaveGameSystem.Instance.AddStore(new HighScoreStore());
}
```

Then read/write it anywhere:

```csharp
var store = SaveGameSystem.Instance.GetStore<int>();
store.Add(1234);
foreach (int s in store.GetAll()) { /* … */ }
```

When the player saves, your store's `Save()` output is written under `MyMod_HighScores`; when they
load, `Load(...)` is called with that string.

## How do I…?

### Store a blob (not a collection)

If you don't need the collection helpers, implement `IStorable` directly:

```csharp
using Drova_Modding_API.Systems.SaveGame.Store;
using System.Text.Json;

public sealed class SettingsStore : IStorable
{
    public MySettings Current { get; private set; } = new();

    public string SaveGameKey => "MyMod_Settings";
    public string Save() => JsonSerializer.Serialize(Current);
    public void Load(string result) =>
        Current = JsonSerializer.Deserialize<MySettings>(result) ?? new MySettings();
}
```

### React to save/load events

```csharp
SaveGameSystem.BeforeSaveGameSaving += save => { /* flush runtime state into stores */ };
SaveGameSystem.BeforeSaveGameLoaded += save => { /* about to load */ };
SaveGameSystem.AfterSaveGameLoaded  += save => { /* stores are populated; safe to read */ };
```

`AfterSaveGameLoaded` is the right moment to act on freshly loaded data — by then every store's
`Load(...)` has run.

### Look up a store later

```csharp
IStore<int>  byType = SaveGameSystem.Instance.GetStore<int>();
IStorable    byKey  = SaveGameSystem.Instance.GetStore("MyMod_HighScores");
```

## Worked reference: how lazy actors persist

The API's own `LazyActorStore` is an `IStore<LazyActorSaveData>` registered at startup (in
`SystemInit.RegisterStores()`). When you call `NpcCreator.CreateLazy(saveToLazyActorStore: true)`,
a `LazyActorSaveData` row is added to that store; on a load, those rows are restored into live lazy
actors. So "persistent custom NPCs" is just this save system applied to actor metadata — see
[Spawning](./spawning.md).

## API reference

### `SaveGameSystem` (singleton: `SaveGameSystem.Instance`)

| Member                                               | Description                                                       |
|------------------------------------------------------|-------------------------------------------------------------------|
| `void AddStore(IStorable store)`                     | Register a store. Do this once, early (e.g. `OnInitializeMelon`). |
| `IStore<T> GetStore<T>()`                            | Get a registered store by its element type.                       |
| `IStorable GetStore(string saveGameKey)`             | Get a registered store by its key.                                |
| `static event SaveGameDelegate BeforeSaveGameSaving` | Fired before a save is written.                                   |
| `static event SaveGameDelegate BeforeSaveGameLoaded` | Fired before a save is loaded.                                    |
| `static event SaveGameDelegate AfterSaveGameLoaded`  | Fired after a save is loaded (stores populated).                  |

### `IStorable`

| Member                        | Description                                |
|-------------------------------|--------------------------------------------|
| `string SaveGameKey { get; }` | Unique key the blob is stored under.       |
| `string Save()`               | Serialize your state to a string.          |
| `void Load(string result)`    | Restore your state from the stored string. |

### `IStore<T> : IStorable`

| Member                                                                   | Description            |
|--------------------------------------------------------------------------|------------------------|
| `void Add(T)` / `void AddRange(T[])` / `void Remove(T)` / `void Clear()` | Mutate the collection. |
| `T Get(int index)` / `IEnumerable<T> GetAll()`                           | Read the collection.   |

## Notes & gotchas

- **Register stores in `OnInitializeMelon`.** The API registers its own stores there; do the same
  so your store exists before the first save/load.
- **Pick a unique `SaveGameKey`** (prefix it with your mod name) — keys collide across mods
  otherwise, and the last writer wins.
- **`Save()`/`Load()` deal in plain strings.** JSON via `System.Text.Json` is the conventional
  choice, but anything string-serializable works.
- **Read loaded data on/after `AfterSaveGameLoaded`.** Before that, your store may still hold the
  previous session's contents.
- For serializing *game objects* (dialogue trees, items, gvars) into your save data, see
  [Relocators](./relocators.md).
