# Spawning NPCs

**What it does:** spawns custom characters into the world and configures them from composable
**modules** (health, equipment, cosmetics, talents, dialogue, trader, routine, …). Two builders
do the heavy lifting:

- `NpcCreator` — the general builder: name + position, then `With…` calls, then `Create()` or
  `CreateLazy()`.
- `BanditCreator` — ready-made bandit factories with difficulty-scaled gear and talents.

For NPCs, you want to **persist across save/load**, use **lazy actors** (`CreateLazy(...)` or
`LazyActorCreator`). For purely transient spawns, `Create()` is fine.

> See also: [External NPCs](./external-npcs.md) to place NPCs from JSON/an in-game wizard with no
> code, and [Save Game](./save-game.md) for how lazy actors persist.

## Quick example

A friendly NPC with a cosmetic and a weapon, persisted as a lazy actor:

```csharp
using Drova_Modding_API.Systems.Spawning;
using UnityEngine;
using MelonLoader;

public class Core : MelonMod
{
    private void SpawnGuy(Vector2 position)
    {
        var lazy = new NpcCreator("Friendly Bob", position)
            .WithCosmetic("hair_short_spiky")   // cosmetic by readable id
            .WithItem("weapon_sword_longsword") // inventory/equipment item by readable id
            .IsPlayerFriendly(true)
            .CreateLazy(saveToLazyActorStore: true); // survives save/reload
    }
}
```

Readable IDs come from `Drova_Modding_API.GlobalFields.ItemReadableIds` (and `TalentReadableIds`,
`StatReadableIds`). Use those constants instead of hard-coding strings.

## How do I…?

### Spawn a bandit

`BanditCreator` has one factory per loadout; each scales gear, talents, health, and XP by
`BanditDifficulty` (`Easy`, `Normal`, `Hard`) and returns a lazy actor:

```csharp
using Drova_Modding_API.Systems.Spawning.Templates;

BanditCreator.CreateSwordShieldBanditLazy("Tough Bandit", new Vector2(0, 0),
    BanditDifficulty.Hard, saveToLazyActorStore: true);

// other variants: CreateDaggerBanditLazy, CreateSwordBanditLazy, CreateAxeBanditLazy,
// CreateSpearBanditLazy, CreateSpearShieldBanditLazy, CreateBowBanditLazy,
// CreateSpearSlingshotBanditLazy, CreateSwordSlingshotBanditLazy,
// CreateRandomBanditLazy (picks one at random)
```

### Spawn a plain creature (not an NPC)

For creatures (wolves, bears, …) with optional custom health, use `LazyActorCreator`:

```csharp
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Spawning;

var bear = LazyActorCreator.CreateLazyActorCreature(new LazyActorCreator.LazyActorParams
{
    AssetReference = AddressableAccess.Creatures.Bear,
    EntityInfo     = AddressableAccess.EntityInfos.EntityInfo_NPC_Bear,
    Position       = new Vector2(10, 20),
    HasCustomHealth = true,
    MaxHealth = 500,
    CurrentHealth = 500,
});
```

Or the lower-level `CreateLazyActor(name, actorRef, position, entityInfoRef, customEntityInfo,
noSave, isNpc)` when you want full control.

### Make an NPC a trader

Attach a `TraderModule` (a built-in `INpcModule`):

```csharp
using Drova_Modding_API.Systems.Spawning.Modules;

new NpcCreator("Merchant", position)
    .WithModule(new TraderModule()
        .WithMoney(500)
        .WithItem("consumable_potion_health", amount: 5)
        .WithItem("weapon_sword_broadsword"))
    .CreateLazy(saveToLazyActorStore: true);
```

> A `TraderModule` only fills `Actor._tradeActor`. To actually open the trade window in-game, the
> NPC's dialogue needs a trade-window node — see [Dialogues](./dialogues.md).

### Set health, stats, talents, alignment

These are all modules under `Drova_Modding_API.Systems.Spawning.Modules`:

```csharp
new NpcCreator("Elite", position)
    .WithModule(new HealthPresetModule().With(1500))
    .WithModule(new CustomStatsModule().WithExpOnDead(180))
    .WithModule(new TalentPresetModule().With(TalentReadableIds.Sword.Talent00))
    .IsPlayerFriendly(false)   // applies the wild-human (enemy) alignment
    .CreateLazy(saveToLazyActorStore: true);
```

| Module                  | Purpose                                                                |
|-------------------------|------------------------------------------------------------------------|
| `HealthPresetModule`    | Sets max health (and a scaled insane-mode value).                      |
| `EquipmentPresetModule` | Adds inventory/equipment items. (`WithItem` on the creator uses this.) |
| `CosmeticsPresetModule` | Adds cosmetics. (`WithCosmetic` on the creator uses this.)             |
| `CustomStatsModule`     | Custom stats, e.g. XP on death.                                        |
| `TalentPresetModule`    | Grants talents.                                                        |
| `FlowPresetModule`      | Configures AI behaviour/spell preset.                                  |
| `AlignmentModule`       | Sets faction/alignment (used by `IsPlayerFriendly`).                   |
| `RoutineModule`         | Gives the NPC a waypoint routine (see [Routines](./routines.md)).      |
| `DialogueModule`        | Attaches a dialogue tree (see [Dialogues](./dialogues.md)).            |
| `TeacherModule`         | Makes the NPC a trainer.                                               |
| `TraderModule`          | Makes the NPC a merchant.                                              |
| `EntityInfoModule`      | Sets the NPC's `EntityInfo`.                                           |

### Write my own module

Implement `INpcModule`. `Apply` runs when the NPC is created; `Priority` controls order (lower
runs first; equal priorities keep insertion order). Use the `ModuleContext` to resolve cached
components:

```csharp
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;

public sealed class RenameModule(string suffix) : INpcModule
{
    public int Priority => 1000; // run late
    public void Apply(ModuleContext context)
    {
        var actor = context.GetComponent<Actor>();
        if (actor != null)
            actor.gameObject.name += $" [{suffix}]";
    }
    // Cleanup(ModuleContext) is optional; called when a lazy actor is destroyed.
}

// usage:
new NpcCreator("Bob", position).WithModule(new RenameModule("Guard")).Create();
```

### `Create()` vs `CreateLazy()`

|                           | `Create()`                      | `CreateLazy(saveToLazyActorStore)`     |
|---------------------------|---------------------------------|----------------------------------------|
| Returns                   | a live `GameObject` immediately | a `LazyActor` handle                   |
| Modules run               | once, now                       | every time the runtime actor loads     |
| Persists across save/load | no                              | yes, when `saveToLazyActorStore: true` |
| Best for                  | transient/effect spawns         | persistent world characters            |

Lazy actors use a custom pre-init hook (the API can't use `LazyActor.ActorSpawnEvent` due to an
IL2CPP marshaling limitation), so always go through `NpcCreator`/`LazyActorCreator` rather than
wiring lazy actors by hand.

## API reference

### `NpcCreator` (`new NpcCreator(string name, Vector2 position)`)

| Member                                                                                                   | Description                                                              |
|----------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| `NpcCreator WithModule(INpcModule)`                                                                      | Add a module to apply at creation.                                       |
| `NpcCreator WithCosmetic(string readableId)` / `WithCosmetic(AssetReferenceT<Item>)`                     | Add a cosmetic.                                                          |
| `NpcCreator WithItem(string readableId)` / `WithItem(AssetReferenceT<Item>)`                             | Add an inventory/equipment item.                                         |
| `NpcCreator WithLazyEntityInfo(AssetReference)` / `WithLazyEntityInfo(EntityInfo)`                       | Set the entity info used by `CreateLazy`.                                |
| `NpcCreator IsPlayerFriendly(bool)`                                                                      | `false` applies the enemy alignment; `true` keeps the template default.  |
| `GameObject Create()`                                                                                    | Spawn immediately, apply modules once.                                   |
| `LazyActor CreateLazy(bool saveToLazyActorStore = false, string? externalDefinitionId = null)`           | Spawn as a lazy actor; modules re-run on each load.                      |
| `static GameObject CreateNpc(string, Vector2)` / `static LazyActor CreateLazyNpc(string, Vector2, bool)` | Convenience factories with defaults.                                     |
| `static void CacheAlignments()` / `static void SetSpawnRoot(Transform)`                                  | Performance/parenting setup (the API calls these for you on scene load). |

### `LazyActorCreator` (static)

| Member                                                                                                                                                                                                        | Description                                                                                      |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------|
| `LazyActor CreateLazyActor(string name, AssetReferenceGameObject actor, Vector2 position, AssetReference? entityInfoRef = null, EntityInfo? customEntityInfo = null, bool noSave = false, bool isNpc = true)` | Create a generic lazy actor.                                                                     |
| `LazyActor CreateLazyActorCreature(LazyActorParams)`                                                                                                                                                          | Create a creature lazy actor, with optional custom health.                                       |
| `struct LazyActorParams`                                                                                                                                                                                      | `AssetReference`, `EntityInfo`, `Position`, `HasCustomHealth`, `MaxHealth?`, `CurrentHealth?`, … |

### `BanditCreator` (static) & `BanditDifficulty`

One `Create…BanditLazy(name, position, difficulty = Normal, saveToLazyActorStore = false)` per
loadout (dagger, sword, axe, sword+shield, spear, spear+shield, bow, spear+slingshot,
sword+slingshot, random). `BanditDifficulty` is `Easy | Normal | Hard`.

### `INpcModule`

| Member                                | Description                                                       |
|---------------------------------------|-------------------------------------------------------------------|
| `int Priority { get; }`               | Apply order; lower first, ties keep insertion order. Default `0`. |
| `void Apply(ModuleContext context)`   | Apply your logic to the NPC.                                      |
| `void Cleanup(ModuleContext context)` | Optional; called when a lazy actor is destroyed.                  |

`ModuleContext` exposes `GetComponent<T>()` / `GetComponentInChildren<T>()` (cached) and
`LazyActor? LazyActor`.

## Notes & gotchas

- **Spawn during gameplay/actor scenes**, not in menus. The API parents spawned NPCs under its
  own root and configured the spawn root on the actor-scene load.
- **Use `CreateLazy(saveToLazyActorStore: true)` for anything you want to survive a reload.** A
  bare `Create()` NPC vanishes on save/load.
- **`saveToLazyActorStore: true` needs entity info** — either `WithLazyEntityInfo(...)` or custom
  entity info — otherwise it logs a warning and won't record save data.
- **Readable IDs over magic strings.** Pull them from `GlobalFields.ItemReadableIds` /
  `TalentReadableIds`; they're validated against the game databases at applied time (a bad id logs
  a warning and is skipped).
- `WithCosmetic`/`WithItem` reuse a single shared preset module across calls — call them as many
  times as you like.
