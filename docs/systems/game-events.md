# Game Events

**What it does:** turns Drova gameplay moments (kills, deaths, quests, looting, learning) into
plain C# events. Subscribe from anywhere; nothing to register. Handlers run on the main thread,
so you can touch game objects directly. Keep them cheap, they run inside the game's call.

## Quick example

```csharp
using Drova_Modding_API.Access;
using Il2CppDrova;
using MelonLoader;

GameEvents.OnPlayerKilledActor += victim => MelonLogger.Msg($"Player killed {victim.name}");
GameEvents.OnQuestCompleted += questListName => MelonLogger.Msg($"Quest completed: {questListName}");
```

## Events

| Event                                                                          | Fires when                                                                                                |
|--------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| `OnEntityKilled(Entity, HealthChangeArgs)`                                     | any entity kills another. The first argument is the killer; the victim is `args.TargetHealth.OwnerEntity` |
| `OnEntityDied(Entity)`                                                         | any entity dies, with or without a killer (environment, scripts, `SetHealth(0)`)                          |
| `OnPlayerKilledActor(Actor)`                                                   | the player directly kills a real actor                                                                    |
| `OnPlayerDied()`                                                               | the player dies                                                                                           |
| `OnAttributePointsLearned(LearnService, int)`                                  | attribute points are bought at a teacher (int = net points)                                               |
| `OnTalentLearned(Actor, TalentContainer)`                                      | an actor learns a new talent                                                                              |
| `OnQuestStateChanged(string, QuestState)`                                      | a quest state changes (string = the quest's GVarList name)                                                |
| `OnQuestCompleted(string)`                                                     | a quest is completed (string = the quest's GVarList name)                                                 |
| `OnLootInventoryOpened(Interact_Bhvr_LootInventory, bool)`                     | a chest, container or corpse is opened (bool = opened by an NPC)                                          |
| `OnPickupCollected(PickupInteraction)`                                         | a dropped ground pickup (currency, ore) is collected                                                      |
| `OnPickUpOnceLooted(Saveable_PickUp_Once)`                                     | a pick-once world object (berries, mushrooms, loose items) is collected                                   |
| `OnLootedAll(Interact_Bhvr_LootAll)`                                           | a "take everything" interaction runs                                                                      |
| `OnCacheLooted(SpawnFromLootTable)`                                            | a loot cache (breakable, hidden stash) drops its loot                                                     |
| `OnResourceMinigameFinished(Interact_Bhvr_ResourceSpot, MinigameFinishedArgs)` | a mining or fishing minigame finishes                                                                     |
| `OnTraderItemRemoved(TraderActorAdapter, ItemTraderStack)`                     | an item stack leaves a trader's inventory (buy or sell)                                                   |

### Reading a quest state

Use `QuestStateReader.TryRead(gvar, out QuestState state)`. It returns `false` (and
`state = None`) when the value can't be read. Never call `GetValue` on a quest variable directly:
that method may be missing from the game build and crashes the game instead of throwing.

## Gotchas

- **`OnPlayerKilledActor` skips indirect kills.** Summons, damage over time and the environment
  don't count, and neither do props or self-kills. To see every kill, use `OnEntityKilled` and
  filter yourself.
- **`OnAttributePointsLearned` is teacher-only.** Sleep-menu points and permanent potions don't
  fire it.
- **`OnTalentLearned` skips force-granted talents** (`ForceLearnTalent`) and talents the actor
  already knows.
- **Knocked-out NPC loot** also comes through `OnLootInventoryOpened`.
- **Saving cache loot:** in `OnCacheLooted`, the dropped pickups get new ids every load. Save your
  state against the spawner's `GuidComponent` instead.
