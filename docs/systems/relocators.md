# Relocators

**What it does:** relocators turn a live **game object** into a small JSON string you can put in
your [save data](./save-game.md), and turn it back into the live object on a load. Game objects
(items, dialogue trees, entity infos, global variables) can't be serialized directly, so a
relocator stores a stable reference (a GUID/key) and resolves it from the game databases later.

Entry points (namespace `Drova_Modding_API.Systems.Editor.Relocators`):
- `IObjectRelocator<T>` — the contract.
- `ObjectRelocatorFactory` — the registry that maps a type to its relocator.

You typically use relocators when you build a feature that needs to remember "which item/dialogue
tree/gvar" across a save — they pair naturally with a custom [store](./save-game.md).

## Built-in relocators

`ObjectRelocatorFactory` ships relocators for:

| Type           | Relocator                 |
|----------------|---------------------------|
| `Item`         | `ItemRelocator`           |
| `DialogueTree` | `DialogueTreeRelocator`   |
| `EntityInfo`   | `EntityInfoRelocator`     |
| `GVarList`     | `GVarListRelocator`       |
| `GInt`         | `GVarIntRelocator`        |
| `GBool`        | `GVarBoolRelocator`       |
| `GQuestState`  | `GVarQuestStateRelocator` |

## Quick example

Serialize an item to a string for saving and read it back later:

```csharp
using Drova_Modding_API.Systems.Editor.Relocators;
using Il2CppDrova.Items;

IObjectRelocator<Item> itemRelocator = ObjectRelocatorFactory.GetRelocator<Item>();

// Save: store this string in your IStore/IStorable.
string json = itemRelocator.SerializeObjectToJson(someItem);

// Load: turn the string back into the live game object.
Item restored = itemRelocator.DeserializeObjectFromJson(json);
```

Under the hood `ItemRelocator` stores the item's `Guid` and resolves it via
`ProviderAccess.ItemDatabase.GetItemByGuid(...)` on a load — so the reference survives even though
the `Item` instance does not.

## How do I…?

### Persist a referenced game object in my store

Combine a relocator with a [store](./save-game.md): keep the relocator's string in your store's
state.

```csharp
// In your IStorable.Save():
var rel = ObjectRelocatorFactory.GetRelocator<Item>();
string itemJson = rel.SerializeObjectToJson(_currentReward);
return JsonSerializer.Serialize(new State { RewardItem = itemJson });

// In your IStorable.Load(result):
var state = JsonSerializer.Deserialize<State>(result);
_currentReward = ObjectRelocatorFactory.GetRelocator<Item>()
    .DeserializeObjectFromJson(state.RewardItem);
```

### Write a relocator for a new type

Implement `IObjectRelocator<T>` and register it. Store a stable reference (a GUID/key), not the
object itself; subclass `RelocatorSaveData` so your payload carries the discriminating `Key`:

```csharp
using Drova_Modding_API.Systems.Editor.Relocators;
using System.Text.Json;

public class MyThingRelocator : IObjectRelocator<MyThing>
{
    public string Key => "MyThing";

    public class SaveData : RelocatorSaveData { public string ThingGuid; }

    public string SerializeObjectToJson(MyThing thing) =>
        JsonSerializer.Serialize(new SaveData { Key = Key, ThingGuid = thing.Guid });

    public MyThing DeserializeObjectFromJson(string json)
    {
        var data = JsonSerializer.Deserialize<SaveData>(json);
        return LookUpMyThingByGuid(data.ThingGuid);
    }
}

// Register so GetRelocator<MyThing>() finds it:
ObjectRelocatorFactory.RegisterRelocator(new MyThingRelocator());
```

## API reference

### `IObjectRelocator<T>`

| Member                                     | Description                                                                               |
|--------------------------------------------|-------------------------------------------------------------------------------------------|
| `string Key { get; }`                      | Discriminator stored in the payload so a generic record can route to the right relocator. |
| `string SerializeObjectToJson(T obj)`      | Serialize the object to a JSON string (typically a stored GUID).                          |
| `T DeserializeObjectFromJson(string json)` | Re-resolve the live object from the JSON string.                                          |

### `ObjectRelocatorFactory` (static)

| Member                                           | Description                                      |
|--------------------------------------------------|--------------------------------------------------|
| `IObjectRelocator<T> GetRelocator<T>()`          | Get the registered relocator for `T`, or `null`. |
| `void RegisterRelocator<T>(IObjectRelocator<T>)` | Register/replace the relocator for `T`.          |

`RelocatorSaveData` is the base payload class; it carries a `string Key`. Subclass it to add your
GUID/reference fields.

## Notes & gotchas

- **Relocators store references, not data.** Deserialization looks the object up in the game
  databases, so the object must still exist in the game (same/compatible version).
- **`GetRelocator<T>()` can return `null`** if no relocator is registered for `T` — register one
  first.
- This pairs with [Save Game](./save-game.md): a relocator gives you the *string*; a store gives
  you the *place to keep it*.
- Dialogue editing uses this machinery internally to persist references inside saved dialogue
  graphs — see [Dialogues](./dialogues.md).
