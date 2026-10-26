# Quests

Ship custom quests as JSON, without code. A Drova quest is a quest GVarList (its `QuestState` plus
bools and ints), journal entries, and dialogues that read and write those variables. The API adds
journal entries from JSON, creatures and items placed in the world, kill tracking, areas the player
must reach, custom items, and world objects that only exist while game variables have certain values.

A complete quest is a handful of files in `Mods/Modding_API/`:

```
GlobalVars/<mod>.json          the quest GVarList (state + bools)   → Global Variables
Quests/<mod>.json              journal entries, kill targets, areas  → this page
CreaturePlacement/<mod>.json   creatures placed in the world         → this page
ItemPlacement/<mod>.json       items lying in the world              → this page
Items/<mod>.json (+ PNGs)      custom items                          → this page
NpcPlacement/<mod>.json        a new quest giver                     → External NPCs
NpcDialogues/<tree>.json       new dialogues, or overrides of vanilla → Dialogues
Localization/en/<mod>_en.loc   every text                            → Localization
```

`samples/QuestShowcase` is a full example with three quests (a kill quest, an item quest on a vanilla
NPC, and a "go there and take it" quest). `tests/scenarios/quest_showcase.scn` plays them in-game.

## Example: a kill quest

**1. The state.** `GlobalVars/QuestShowcase.json`. `IsQuestVarList` adds the `QuestState` variable
(`None`, `IsAvailable`, `IsRunning`, `IsCompleted`, `HasFailed`):

```json
{
  "Version": 1,
  "Lists": [
    {
      "Guid": "6f1d2c5e-8a3b-4c7d-9e21-0b5a4f3c2d10",
      "Name": "Q_Showcase_RipperHunt",
      "IsQuestVarList": true,
      "Vars": [ { "Id": "RipperDead", "Name": "RipperDead", "Type": "GBool", "InitialValue": "false" } ]
    }
  ]
}
```

**2. The creature.** `CreaturePlacement/QuestShowcase.json` places a ripper until it is killed:

```json
{
  "Version": 1,
  "Creatures": [
    { "Id": "showcase_ripper", "Creature": "8074729ab27467d42bbb9c783c2f9758", "EntityInfo": "EntityInfo_NPC_Ripper", "X": -2132, "Y": -5900 }
  ]
}
```

**3. The quest.** `Quests/QuestShowcase.json` binds journal entries and the kill to the list:

```json
{
  "Version": 1,
  "Quests": [
    {
      "Id": "showcase_ripper_hunt",
      "GVarList": "Q_Showcase_RipperHunt",
      "Topic": { "LocaPath": "QuestShowcase", "LocaKey": "RipperHunt_Title", "Group": "TopicGroup_World" },
      "Entries": [
        { "Id": "start",  "LocaKey": "RipperHunt_Start",  "OnState": "IsRunning" },
        { "Id": "killed", "LocaKey": "RipperHunt_Killed", "OnGBool": "RipperDead" },
        { "Id": "done",   "LocaKey": "RipperHunt_Done",   "Exp": 200, "OnState": "IsCompleted" }
      ],
      "Kills": [
        { "Id": "ripper", "Actors": [ "showcase_ripper" ], "SetGBool": "RipperDead" }
      ]
    }
  ]
}
```

**4. The dialogue.** Built in the [dialogue editor](./dialogues.md) from vanilla nodes:

- `QuestState == IsCompleted` (`GQuestStateConditionTask`) → "Thanks again."
- `QuestState == IsRunning` and `RipperDead == true` (`GBoolConditionTask`) → reward:
  `DS_SetQuestStateNode` → `IsCompleted`, `DS_GiveItemNode` (`VoidToPlayer`).
- Otherwise → offer the quest; on "yes", `DS_SetQuestStateNode` → `IsRunning`.

The journal follows on its own: `IsRunning` adds "start", the kill adds "killed", `IsCompleted` adds
"done" with 200 exp and moves the quest to the finished quests. The kill counts even if the ripper
died before the quest started.

## How do I…?

### Make an item quest

Get the item to the player (an `ItemPlacement`, a trader, loot), then in the dialogue: `DS_HasItems`
in a `ConditionNode`, `DS_GiveItemNode` with `PlayerToNPC` to take the items, `DS_SetQuestStateNode`.
Set the item's `Mode` to `Int` to type an amount; the default reads it from a GInt.

### Add a quest to a vanilla NPC

Open the NPC's dialogue tree in the dialogue editor, add a choice to its hub (with a
`GQuestStateConditionTask` so it disappears once the quest is done), and save under the tree's own
name. Return to the hub with a `DS_HubJumpNode`, as vanilla trees do.

### Place an item in the world

`ItemPlacement/<mod>.json`:

```json
{
  "Version": 1,
  "Items": [ { "Id": "showcase_apples", "Item": "item_apple", "Amount": 3, "X": -2000, "Y": -5300 } ]
}
```

Once taken, it is gone in that save.

### Send the player somewhere

An area sets a bool when the player enters it:

```json
"Areas": [
  { "Id": "shrine", "X": -2132, "Y": -5600, "Radius": 80, "SetGBool": "ShrineReached",
    "Conditions": [ { "GVarList": "Q_Showcase_Tracks", "Var": "QuestState", "Value": "IsRunning" } ] }
]
```

The area exists while its conditions hold and its bool is still false.

### Change the world with the quest

NPCs (`NpcPlacement`), creatures, items and areas take `Conditions`: game variables that must all have
the given value. The thing appears when they hold and disappears when they stop holding.

```json
"Conditions": [ { "GVarList": "Q_Showcase_RipperHunt", "Var": "QuestState", "Value": "IsCompleted" } ]
```

- `Var`: a variable id, or `QuestState` for the list's state.
- `Value`: `true`/`false`, a quest state name, or a number. A leading `!` negates (`"!IsCompleted"`).
- A missing variable never matches. Int changes are only seen on the next state/bool change or load.

### Make taking an item count

`TakenGVarList` + `TakenGBool` set a bool when the player picks the item up:

```json
{ "Id": "showcase_supplies", "Item": "item_meat_raw", "Amount": 2, "X": -2100, "Y": -5650,
  "Conditions": [ { "GVarList": "Q_Showcase_Tracks", "Var": "QuestState", "Value": "IsRunning" },
                  { "GVarList": "Q_Showcase_Tracks", "Var": "SuppliesTaken", "Value": "!true" } ],
  "TakenGVarList": "Q_Showcase_Tracks", "TakenGBool": "SuppliesTaken" }
```

The `!true` condition makes the item vanish for co-op teammates too once anyone took it.

### Make a custom item

`Items/<mod>.json` adds copies of vanilla items with your own name, price, icon and ability values:

```json
{
  "Version": 1,
  "Items": [
    {
      "ReadableId": "showcase_spezi",
      "Base": "item_salve_healing",
      "LocaPath": "QuestShowcase",
      "BuyValue": 60,
      "SellValue": 12,
      "EffectStacks": 9,
      "Ability": { "ABI_HealthValue": 3 },
      "Icon": "spezi.png"
    }
  ]
}
```

Name and description are the keys `showcase_spezi_name` and `showcase_spezi_description` in
`LocaPath`. The icon is a 26x26 PNG next to the file. Weapons and armor keep the base item's look.
A custom item works anywhere a vanilla one does: dialogues, traders, NPC equipment, item placements,
the `additem` console command, and saves.

To find ability keys and values, use the Debug test agent: `questshowcase item <id>` prints an item's
ability values, `questshowcase abi <name>` those of an ability.

### Make a spell

A spell is a custom item based on a vanilla spell (`flow_*`). Its recipe is a custom item based on a
vanilla recipe (`recipe_flow_*`) with `LearnSpell`. As in vanilla, the player learns the spell when
holding the recipe and a runestone.

```json
{ "ReadableId": "showcase_swift_missile", "Base": "flow_energyProjectile", "LocaPath": "QuestShowcase",
  "Ability": { "Move_ForwardForce": 540, "ABI_Lifetime": 6,
               "ABI_EnergyProjectile_Explosion/Dmg_Properties[0]": 500 } },
{ "ReadableId": "showcase_recipe_swift_missile", "Base": "recipe_flow_energyProjectile",
  "LocaPath": "QuestShowcase", "BuyValue": 400, "LearnSpell": "showcase_swift_missile" }
```

`Ability` keys can reach:

- abilities the spell spawns: `"ABI_Lifetime": 6`;
- abilities those spawn, by name and a slash: `"ABI_EnergyProjectile_Explosion/Poise_Damage": 20`;
- one damage entry by index: `"Dmg_Properties[0]"` (player), `"Dmg_Properties_NPCs[0]"` (NPC caster).
  Damage is still scaled by the caster's stats.

Find names with `questshowcase itemdump <id>` and `questshowcase abitree <ability>` (Debug test agent).

### Make a morph spell

`Morph` turns a spell into "become this creature for a while". The spell keeps its base's cast
animation, flow cost and flow school (the player can only cast schools they have unlocked); its
abilities are replaced by the game's own (unused) morph ability.

```json
{ "ReadableId": "showcase_ripper_form", "Base": "flow_energyProjectile", "LocaPath": "QuestShowcase",
  "Morph": { "Creature": "8074729ab27467d42bbb9c783c2f9758", "Duration": 30 } }  // Ripper
```

`Creature` is the creature's addressable guid, as listed in `AddressableAccess.Creatures`. The morph ends after
`Duration` seconds (0: never), when the creature dies, or when a mod calls `MorphManager.EndMorph()`
(for example from its own key binding). The player comes back where the creature stood. If the creature
died, the player dies with it, killed by the creature's killer (in co-op the player goes down like from
any lethal blow). While morphed:

- saving is blocked (by the game);
- `PlayerAccess.GetPlayer()` is the creature, `MorphManager.Caster` the hidden player body with its
  inventory and equipment;
- the hidden body is carried along, so NPCs spawn and the world streams around the creature;
- the attack keys use the creature's own weapon (a ripper bites and charges). Creatures have no dodge,
  and their special abilities (a ripper's leap or enrage) are AI-only;
- the world treats the creature like one of its kind: rippers, other wild animals and bandits leave it
  alone, villagers, guards and dogs attack it. Attacking one of a friendly kind turns that kind hostile,
  and its pack joins in;
- no looting, opening, using or talking (the creature's pockets are lost when the morph ends). Doors and
  teleporter gates do not let a creature through;
- morphing out next to hostile creatures puts the player in the fight at once.

Only the player can morph. The same spell cast by an NPC or replayed on a co-op teammate's body does
nothing. In co-op, teammates see the morphed player as that creature. A mod that cannot handle the
player switching bodies vetoes the morph with `MorphManager.CanMorph`.

### Write a letter

```json
{ "ReadableId": "showcase_letter", "Base": "item_letterFromEtzel", "LocaPath": "QuestShowcase",
  "Letter": { "TextKey": "showcase_letter_text" } }
```

It opens when first taken and is listed under the journal's letters. Hand it out like any item.

### Track a vanilla creature

Put the creature's `LazyActor` guid in `Actors`. Find it with the test agent's `find <name>` or the
`api_dumpnearby` cheat (Debug builds). All actors of one kill must be dead.

### Extend a vanilla quest

Set `GVarList` to the vanilla quest's list and `Topic.Vanilla` to its topic (e.g.
`Topic_HuntTheRippers`). Your entries are added to that topic.

### Add a journal entry from a dialogue or code

In a dialogue: an `ActionNode` with `TriggerJournalTask`, picking the entry
`Modding_API/Quests/<questId>/<entryId>`. In code:

```csharp
QuestSystem.TriggerEntry("showcase_ripper_hunt", "killed");
```

## File reference

`Quests/*.json` holds `{ "Version": 1, "Quests": [ … ] }`:

| Field                      | Meaning                                                                                                            |
|----------------------------|--------------------------------------------------------------------------------------------------------------------|
| `Id`                       | Unique quest id. Saved in the journal: never rename.                                                               |
| `GVarList`                 | Name of the quest GVarList, custom or vanilla.                                                                     |
| `Topic.LocaPath`           | Localization file for the quest's texts, usually your mod name.                                                    |
| `Topic.LocaKey`            | Key of the quest title.                                                                                            |
| `Topic.Group`              | Journal group (`TopicGroup_World`, `TopicGroup_MainQuest`, `TopicGroup_Tavern`, …). Default `TopicGroup_Unsorted`. |
| `Topic.Vanilla`            | Vanilla topic to add the entries to instead of a new one.                                                          |
| `Entries[].Id`             | Entry id, unique within the quest. Never rename.                                                                   |
| `Entries[].LocaKey`        | Key of the journal line.                                                                                           |
| `Entries[].Exp`            | Experience granted with the line (rounded down to tens).                                                           |
| `Entries[].OnState`        | Add the line when `QuestState` becomes this value.                                                                 |
| `Entries[].OnGBool`        | Add the line when this bool becomes true, once the quest has started (state not `None`).                           |
| `Kills[].Id`               | Kill id.                                                                                                           |
| `Kills[].Actors`           | Creature placement ids or vanilla actor guids that must all be dead.                                               |
| `Kills[].SetGBool`         | Bool set to true once they are.                                                                                    |
| `Areas[].Id`               | Area id, unique within the quest.                                                                                  |
| `Areas[].X`, `Y`, `Radius` | Center and radius in world units (default radius 100).                                                             |
| `Areas[].SetGBool`         | Bool set to true when the player enters.                                                                           |
| `Areas[].Conditions`       | Game variables that must hold for the area to exist.                                                               |

`ItemPlacement/*.json` holds `{ "Version": 1, "Items": [ … ] }`:

| Field                         | Meaning                                                                     |
|-------------------------------|-----------------------------------------------------------------------------|
| `Id`                          | Unique id. The save is keyed by it: never rename.                           |
| `Item`                        | Readable id of the item (`item_apple`, see `GlobalFields.ItemReadableIds`). |
| `Amount`                      | How many the pickup gives. Default 1.                                       |
| `X`, `Y`                      | World position.                                                             |
| `Enabled`                     | `false` skips it.                                                           |
| `Conditions`                  | Game variables that must hold for the item to lie there.                    |
| `TakenGVarList`, `TakenGBool` | Bool set to true when the player takes the item.                            |

`CreaturePlacement/*.json` holds `{ "Version": 1, "Creatures": [ … ] }`:

| Field        | Meaning                                                                                             |
|--------------|-----------------------------------------------------------------------------------------------------|
| `Id`         | Unique id. The save is keyed by it: never rename.                                                   |
| `Creature`   | A field name of `AddressableAccess.Creatures` or `.Bandits` (`Ripper`, `Bear`, …) or an asset guid. |
| `EntityInfo` | A field name of `AddressableAccess.EntityInfos` or an asset guid. Default `EntityInfo_Default`.     |
| `X`, `Y`     | World position.                                                                                     |
| `Enabled`    | `false` skips it.                                                                                   |
| `Conditions` | Game variables that must hold for the creature to exist.                                            |

`Items/*.json` holds `{ "Version": 1, "Items": [ … ] }`:

| Field                              | Meaning                                                                                             |
|------------------------------------|-----------------------------------------------------------------------------------------------------|
| `ReadableId`                       | The item's id. The save is keyed by it: never rename.                                               |
| `Guid`                             | Optional fixed guid, if the readable id has to change.                                              |
| `Base`                             | Readable id of the vanilla item to copy.                                                            |
| `LocaPath`                         | Localization file with `<ReadableId>_name` and `<ReadableId>_description`.                          |
| `BuyValue`, `SellValue`            | Prices; the base item's when left out. Both 0 makes a quest item.                                   |
| `MaxStackSize`                     | Max items per stack; the base item's when left out.                                                 |
| `EffectStacks`                     | Stacks of the base item's status effect: it acts once when applied and once per stack.              |
| `Ability`                          | Ability values by key (`ABI_HealthValue`, `ABI_Lifetime`, …). Bools take 0 or 1.                    |
| `Icon`                             | PNG file next to the item file.                                                                     |
| `Letter.TextKey`                   | Makes the item a letter with this text (`Base` must be a vanilla letter).                           |
| `Letter.TitleKey`                  | The letter's journal title. The item's name when left out.                                          |
| `LearnSpell`                       | Spell a recipe teaches (`Base` must be a vanilla `recipe_flow_*`).                                  |
| `Morph.Creature`, `Morph.Duration` | Makes a spell a morph into that creature for that many seconds (`Base` must be a vanilla `flow_*`). |

## API reference

| Member                                                                                              | Description                                                                                                      |
|-----------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| `QuestSystem.Quests`                                                                                | Loaded quests by id.                                                                                             |
| `QuestSystem.TriggerEntry(questId, entryId)`                                                        | Adds an entry now. False when unknown or already in the journal.                                                 |
| `QuestSystem.GetJournalEntry(questId, entryId)`                                                     | The entry's `JournalEntry`.                                                                                      |
| `QuestSystem.GetEntryName(questId, entryId)`                                                        | The entry's name, as `TriggerJournalTask` finds it.                                                              |
| `CustomItemSystem.Items`                                                                            | Registered custom items by readable id.                                                                          |
| `CustomItemSystem.Register(CustomItemDefinition)`                                                   | Registers a custom item from code, or updates it in place.                                                       |
| `CustomItemSystem.Reload()`                                                                         | Re-reads the `Items` folder.                                                                                     |
| `MorphManager.IsMorphed` / `Caster` / `Body`                                                        | Whether the player is morphed, the hidden player body, the controlled creature.                                  |
| `MorphManager.OnMorphStarted` / `OnMorphEnded`                                                      | Events: a morph started (caster, creature) or ended (caster).                                                    |
| `MorphManager.EndMorph()`                                                                           | Ends a running morph early.                                                                                      |
| `MorphManager.CanMorph`                                                                             | Optional veto, asked when a morph spell is cast.                                                                 |
| `MorphManager.BodyGuid`                                                                             | Addressable guid of the creature during a morph.                                                                 |
| `MorphManager.TryGetCreature(guid, out prefab)`                                                     | A creature prefab, loaded in the background on first ask.                                                        |
| `MorphManager.EquipCreatureWeapon(actor)`                                                           | Binds a creature's attacks to the attack keys while its AI is off.                                               |
| `MorphManager.CreateAbility(template, creature, duration, holder)`                                  | Builds a morph ability from code.                                                                                |
| `GVarConditions.Changed`                                                                            | Event: a quest state or bool changed.                                                                            |
| `GVarConditions.AllMet(conditions)`                                                                 | Whether every condition holds.                                                                                   |
| `GVarConditions.Find(gvarList, var)`                                                                | A variable by list name and id (`QuestState` for the state).                                                     |
| `GVarConditions.TrySetBool(gvarList, var, value)`                                                   | Sets a bool through the game's own setter.                                                                       |
| `ExternalItemPlacementSystem.GetPickupGuid(id)`                                                     | Save guid of a placed item.                                                                                      |
| `ExternalCreaturePlacementSystem.GetActorGuid(id)`                                                  | Actor guid of a placed creature.                                                                                 |
| `AbilityDataAccess.TryGet` / `TryGetFloat` / `TryGetInt` / `TryGetBool` / `TrySet` / `TrySetDamage` | Read and write ability values. Always use these: reading `_value` directly through IL2CPP returns wrong numbers. |

Each system also has `GetFolderPath()` (`GetQuestFolderPath()` for quests).

## Gotchas

- **Files are read at the main menu.** After editing a quest, return to the main menu and load again.
  Items and texts can also be reloaded with **Reload Content** in Modding Tools (Debug builds); a
  changed `Base` needs a restart.
- **Never rename ids** of quests, entries, placed items, creatures or custom items once released:
  saves are keyed by them.
- **Conditional placements appear a moment after the world loads**, once the save's variables are
  restored. Placements without conditions appear with the world.
- **Kills count from any source** (player, NPC, script), and creatures may wander anywhere. A dead
  placed creature is not placed again.
- **Removed custom items** turn into their base item in old saves instead of vanishing.
- **Finished topics** move to the finished quests on `IsCompleted`/`HasFailed`, for your own topics
  only. A vanilla topic stays where its quest puts it.
- **Localization paths** are file names without the language suffix: `QuestShowcase` reads
  `Localization/en/QuestShowcase_en.loc`. See [Localization](./localization.md).
