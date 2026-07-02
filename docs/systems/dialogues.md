# Dialogues

**What it does:** lets you create and edit Drova dialogue trees — the branching conversations NPCs
have. There are two ways in:

1. **The in-game dialogue editor** (visual, recommended) — build/edit graphs by clicking, in a
   debug build.
2. **`DialogueNodeCreator`** (code) — assemble dialogue nodes programmatically when you need
   dialogue generated or wired up from a mod.

Edits are stored as serialized graph bytes and re-applied onto the live `DialogueTree`
ScriptableObjects at a main-menu load, so vanilla references keep working.

Entry points:
- The in-game editor (see controls below).
- `Drova_Modding_API.Systems.Dialogues.DialogueNodeCreator` — node builder.
- `DialogueModule` (in `…Spawning.Modules`) — attach a dialogue tree to a spawned NPC.

## Option A — the in-game dialogue editor (recommended)

In a `Debug` build with cheat mode on:

1. Open the cheats menu and **left-click an actor** in the world. A small menu appears
   bottom-left with an option to enter the dialogue editor for that actor.
2. In the editor:

| Action                      | Control                          |
|-----------------------------|----------------------------------|
| Open the graph context menu | Right-click the graph background |
| Move a node                 | Left-click and drag the node     |
| Pan the graph               | Middle-click and drag            |
| Zoom                        | Scroll wheel                     |
| Open a subgraph             | Double-click a subgraph node     |

Your edits are saved and re-applied to that dialogue tree on future loads.

## Option B — build dialogue in code

`DialogueNodeCreator` wraps a `DialogueTree` and creates typed nodes for you. Construct it with the
tree and the player actor parameter, create nodes, then connect them on the tree.

```csharp
using Drova_Modding_API.Systems.Dialogues;
using Il2CppNodeCanvas.DialogueTrees;

// `tree` is a DialogueTree, `playerParam` the player's ActorParameter.
var creator = new DialogueNodeCreator(tree, playerParam);

DS_StatementNode line = creator.CreateStatementNode(myStatement, npcActorParameter);
DS_GiveExp xp        = creator.CreateExpNode(500);

tree.ConnectNodes(line, xp); // wire nodes in order
```

> Working in code means handling game types (`DS_Statement`, `ActorParameter`, `EntityInfo`, …)
> directly. The visual editor is far easier for authoring; reach for the code path when you need to
> generate dialogue dynamically.

### Available node creators

`DialogueNodeCreator` (all methods `virtual`, returning the created node type):

| Method                                                                                                          | Creates                                                        |
|-----------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------|
| `CreateStatementNode(DS_Statement, ActorParameter)`                                                             | A spoken line.                                                 |
| `CreateMultipleChoices(ChoiceCreationData[])`                                                                   | A player choice node.                                          |
| `CreateInteractionNode(AA_ABase prefab, ActorParameter, bool waitForFinish, float waitTime)`                    | An actor action/animation.                                     |
| `CreateDialogWindowHideNode(Coroutine?)`                                                                        | Hides the dialogue window.                                     |
| `CreateChangeStanceNode(EInteractionMode, ActorParameter)`                                                      | Changes an actor's stance.                                     |
| `CreateExpNode(int)`                                                                                            | Grants experience.                                             |
| `CreateGiveItemNode(ICollection<DialogItemsExchange>, ActorParameter, bool equip, bool equipInEmptyActiveSlot)` | Transfers items.                                               |
| `CreateWaitNode(float)`                                                                                         | Waits.                                                         |
| `CreateSetFirstChapterNode()`                                                                                   | Sets the first chapter.                                        |
| `CreateSetFactionNode(Faction)`                                                                                 | Sets a faction.                                                |
| `CreateRevisitMultipleChoiceNode(string tag, int repeats)`                                                      | A revisitable choice node.                                     |
| `CreateDefineActiveActorsNode(EntityInfo[])`                                                                    | Declares the active actors.                                    |
| `CreateMultipleConditionNode(ConditionTask[])`                                                                  | A condition gate.                                              |
| `CreateSetGBoolNode(GBool, bool)`                                                                               | Sets a global bool (see [Global Variables](./global-vars.md)). |
| `CreateFinishNode()`                                                                                            | Ends the dialogue.                                             |
| `CreateHubJumpNode(string hubTag)` / `CreateHubNode(string tag, ChoiceCreationData[])`                          | Hub navigation.                                                |
| `CreateSubDialogueTree(DialogueTree)`                                                                           | Embeds a sub-dialogue.                                         |

### Attach dialogue to a spawned NPC

Use the `DialogueModule` with [`NpcCreator`](./spawning.md):

```csharp
using Drova_Modding_API.Systems.Spawning;
using Drova_Modding_API.Systems.Spawning.Modules;

new NpcCreator("Talker", position)
    .WithModule(new DialogueModule(/* tree / configuration */))
    .CreateLazy(saveToLazyActorStore: true);
```

## Notes & gotchas

- **Edits persist as serialized graph bytes**, re-applied to the live `DialogueTree` instances at
  a main-menu load — vanilla references to those trees keep working.
- **The visual editor is debug-only** (cheat mode). It's the practical way to author dialogue; the
  code API is for generation/automation.
- **Dialogue audio is separate.** To give lines spoken audio, see [Audio](./audio.md); IDs there are
  derived from the dialogue tree and node.
- A `TraderModule`/`TeacherModule` only sets the NPC up as a merchant/trainer — the **dialogue**
  still needs the corresponding node (e.g., a trade-window node) to open that UI in-game.
