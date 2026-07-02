# Addressables

**What it does:** Drova loads most of its content as Unity **Addressables**, referenced by GUID
and loaded on demand. `AddressableAccess` ships ready-made `AssetReference`s for hundreds of
game assets — creatures, bandits, items, entity infos, NPC templates and interactions, and the
options-menu UI prefabs — so you don't have to dig up GUIDs yourself.

Entry point: `Drova_Modding_API.Access.AddressableAccess` (static class with nested groups).

## The groups

| Group                           | Contains                                                                     | Reference type                 |
|---------------------------------|------------------------------------------------------------------------------|--------------------------------|
| `AddressableAccess.Creatures`   | Every creature prefab (foxes, spiders, golems, bosses…).                     | `AssetReferenceGameObject`     |
| `AddressableAccess.Bandits`     | Bandit prefabs.                                                              | `AssetReferenceGameObject`     |
| `AddressableAccess.Items`       | Item assets (and `Items.CritterInteractions`).                               | `AssetReferenceT<Item>` / refs |
| `AddressableAccess.EntityInfos` | `EntityInfo` assets describing actor identity/behaviour.                     | `AssetReference`               |
| `AddressableAccess.NPCs`        | `NPCs.Human_Template`, plus `NPCs.BaseInteractions` and `NPCs.Interactions`. | `AssetReferenceGameObject`     |
| `AddressableAccess.GUIOptions`  | Options-menu row prefabs (used internally by the UI builder).                | `AssetReferenceGameObject`     |

> These are the references used throughout the API itself — e.g. [`NpcCreator`](./spawning.md)
> spawns `NPCs.Human_Template` and defaults its entity info to `EntityInfos.EntityInfo_Bandit`.

## Quick example

Most of the time you hand an `AssetReference` straight to an API that knows how to load it — for
example, spawning a creature as a [lazy actor](./spawning.md):

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Spawning;
using UnityEngine;

var wolf = LazyActorCreator.CreateLazyActor(
    name: "Wolf",
    actor: AddressableAccess.Creatures.Bear,            // an AssetReferenceGameObject
    position: new Vector2(10, 20),
    entityInfoRef: AddressableAccess.EntityInfos.EntityInfo_NPC_Bear,
    customEntityInfo: null,
    noSave: false,
    isNpc: false);
```

## How do I…?

### Load an asset directly

If you need the underlying asset (not just to pass the reference along), load it via Unity
Addressables. Because this is IL2CPP, prefer the **non-generic** `LoadAsset` + `TryCast<T>` over
the generic `LoadAssetAsync<T>` — the generic path can throw a `NullReferenceException` when an
asset is missing.

```csharp
using UnityEngine.AddressableAssets;
using Il2CppDrova.Items;

// Synchronous load + safe cast:
var handle = Addressables.LoadAssetAsync<Item>(AddressableAccess.Items.SomeItem);
Item item = handle.WaitForCompletion(); // OK when you know the asset exists

// Safer when an asset might be absent — non-generic load, then TryCast:
var obj = Addressables.LoadAssetAsync<UnityEngine.Object>(someReference).WaitForCompletion();
Item? maybeItem = obj?.TryCast<Item>();
```

### Instantiate a prefab reference

```csharp
var op = AddressableAccess.NPCs.Human_Template.InstantiateAsync(new Vector2(0, 0), Quaternion.identity);
op.WaitForCompletion();
GameObject npc = op.Result;
```

(`NpcCreator.Create()` does exactly this for you — see [Spawning](./spawning.md).)

### Find the reference you need

The groups are large; the names mirror the in-game asset names (`EntityInfo_NPC_Bear`,
`Creatures.GreatTusk`, …). Browse `Access/AddressableAccess.cs` or use your IDE's autocomplete on
`AddressableAccess.Creatures.`, `.EntityInfos.`, etc.

## API reference

`AddressableAccess` is a static class; each nested group exposes `static readonly`
`AssetReference*` fields. There are no methods — you read a reference and pass it to a loader or
an API that accepts it.

| Reference type             | Used for                                                             |
|----------------------------|----------------------------------------------------------------------|
| `AssetReferenceGameObject` | Prefabs you instantiate (creatures, bandits, NPC template, UI rows). |
| `AssetReferenceT<Item>`    | Typed item references.                                               |
| `AssetReference`           | Generic references (entity infos and similar).                       |

## Notes & gotchas

- **Prefer non-generic `LoadAsset` + `TryCast<T>`** to avoid the IL2CPP `LoadAsset<T>` NRE on
  missing assets. See [Core Concepts](../concepts.md).
- **`WaitForCompletion()` blocks** the current frame. Fine for a one-off setup; avoid hot paths.
- The GUIDs are tied to the current game version. If an asset is renamed/removed by a game update,
  a reference may stop resolving — handle `null`/missing gracefully.
- You usually don't load these yourself: pass the reference to [`NpcCreator`](./spawning.md) /
  [`LazyActorCreator`](./spawning.md) / the option builder and let them do the loading.
