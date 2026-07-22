# Game Events

**What it does:** bridges Drova's gameplay moments out as plain C# events, so mods can subscribe
instead of re-discovering (and re-patching) the same game methods. Every event is a Harmony hook
onto a deliberately chosen chokepoint — usually *not* the obvious method — so you get one reliable
callback per real occurrence.

All events fire on the **main thread**, and the hooks are installed once at melon init. Just
subscribe from anywhere; there is nothing to register or initialize.

## Quick example

```csharp
using Drova_Modding_API.Access;
using Il2CppDrova;

GameEvents.OnPlayerKilledActor += (Actor victim) =>
{
    MelonLoader.MelonLogger.Msg($"Player killed {victim.name}");
};

GameEvents.OnQuestCompleted += questListName =>
{
    MelonLoader.MelonLogger.Msg($"Quest completed: {questListName}");
};
```

## Choosing the right event

- **Count only the player's kills?** Use `OnPlayerKilledActor` — it already excludes props,
  self-kills, NPC-vs-NPC, and indirect kills (DoT/summons carry a non-player source).
- **React to any kill?** `OnEntityKilled` gives you the killer `Entity` and the
  `HealthChangeArgs`; the victim is `args.TargetHealth.OwnerEntity`.
- **Detect teacher learning but not level-ups?** `OnAttributePointsLearned` fires only from the
  teacher menu's commit (`LearnService.ApplyData`); sleep-menu allocation and perma-potions use
  other paths by design.
- **Read a quest state safely?** Use `QuestStateReader.TryRead(gvar, out state)` — never call
  the typed `AGVar<QuestState>.GetValue`, its closed-generic body may not exist in the AOT
  build and can hard-crash (see gotchas).
- **Tell chest loot from plant picking?** Chests/corpses → `OnLootInventoryOpened`; scene-placed
  pick-once objects (plants, loose items) → `OnPickUpOnceLooted`; runtime drops (currency, ore)
  → `OnPickupCollected`; breakable caches → `OnCacheLooted`; mining/fishing minigames →
  `OnResourceMinigameFinished`.

## API reference

| Event | Fires when |
|---|---|
| `OnEntityKilled(Entity, HealthChangeArgs)` | any entity kills another (killer-side callback; victim = `args.TargetHealth.OwnerEntity`) |
| `OnPlayerKilledActor(Actor)` | the player directly kills a real actor (props, self-kills and indirect kills excluded) |
| `OnPlayerDied()` | the player dies |
| `OnAttributePointsLearned(LearnService, int)` | attribute points bought at a teacher (int = net points, undo clicks cancel out) |
| `OnTalentLearned(Actor, TalentContainer)` | an actor genuinely learns a talent (no-ops filtered, `ForceLearnTalent` excluded) |
| `OnQuestStateChanged(string, QuestState)` | a quest state variable changes (string = owning GVarList name) |
| `OnQuestCompleted(string)` | a quest reaches `IsCompleted` (string = owning GVarList name) |
| `OnLootInventoryOpened(Interact_Bhvr_LootInventory, bool)` | a chest/container/corpse inventory opens (bool = opened by an NPC) |
| `OnPickupCollected(PickupInteraction)` | a runtime-dropped ground pickup (currency, ore) is collected |
| `OnPickUpOnceLooted(Saveable_PickUp_Once)` | a scene-placed pick-once object (berries, mushrooms, loose items) is collected |
| `OnLootedAll(Interact_Bhvr_LootAll)` | a generic "take everything" loot-all interaction runs |
| `OnCacheLooted(SpawnFromLootTable)` | a loot-table cache (breakable, hidden stash) drops its loot |
| `OnResourceMinigameFinished(Interact_Bhvr_ResourceSpot, MinigameFinishedArgs)` | a resource-spot minigame (mining vein, fishing spot) finishes |
| `OnTraderItemRemoved(TraderActorAdapter, ItemTraderStack)` | an item stack leaves a trader's inventory (buy or sell) |

### Reading quest state — `QuestStateReader`

| Member | Description |
|---|---|
| `bool TryRead(AGVarBase? gvar, out QuestState state)` | Read a quest state through the safe, always-compiled virtual path. `false` (and `state = None`) when it can't be read. |
| `bool HasNativeBody(MethodBase? proxyMethod, out string detail)` | Preflight: is a closed-generic proxy method's native body actually AOT-compiled and safe to patch/invoke? |

## Notes & gotchas

- **Everything is main-thread.** Handlers run on Drova's main thread, so you can touch Unity /
  Il2Cpp objects directly — but keep handlers cheap; they run inside the game's call.
- **`OnPlayerKilledActor` undercounts, never misattributes.** Kills dealt indirectly (summons,
  damage-over-time, environment) carry a non-player source and do not fire. If you need every
  death, use `OnEntityKilled` and filter yourself.
- **Quest state is AOT-fragile.** `OnQuestStateChanged` prefers a hook on
  `AGVar<QuestState>.SetValue`, but only after `QuestStateReader.HasNativeBody` proves the
  closed-generic body was compiled — patching a body-less generic *hard-crashes* rather than
  throwing. When the preflight fails it falls back to `GQuestStateOperation.OperateIntern`, which
  still catches quest-graph and dialogue-driven writes. For reads, always go through
  `QuestStateReader.TryRead`, never the typed `GetValue`.
- **`OnPlayerDied` uses a postfix, not the game's event.** Subscribing to `PlayerActorDiedEvent`
  directly is impossible: its `EntityGameHandler.EventArgs<Actor>` argument is a non-blittable
  struct that Il2CppInterop's delegate marshaling rejects. The postfix needs no marshaling.
- **Corpse loot funnels through `OnLootInventoryOpened`.** `Interact_Bhvr_LootKnockout`
  (knocked-out NPC loot) inherits the same method and overrides nothing.
- **Persist cache loot against the spawner.** In `OnCacheLooted`, the spawned pickups carry
  runtime `SaveRoot_Dynamic` ids; persist against the *spawner's* `GuidComponent`, never the
  pickups' own ids.
- **Talent learning excludes force-grants.** `OnTalentLearned` fires only for genuine learns from
  the teacher menu, dialogue, and `LearnUtil`; `ForceLearnTalent` (mods granting talents
  directly) never fires, and already-known talents are filtered out.
