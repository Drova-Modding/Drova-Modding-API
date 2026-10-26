# Dialogues

**What it does:** create and edit Drova dialogue trees, the branching conversations NPCs have.
Two ways in:

1. **The in-game dialogue editor** (recommended): click to build and edit graphs. Debug build only.
2. **`DialogueNodeCreator`** (code): build nodes from a mod when dialogue has to be generated.

Saved edits replace the game's own tree, so every NPC and quest that already uses that tree
gets your version.

## Option A: the in-game editor

In a `Debug` build with cheat mode on:

1. Open the cheats menu and **left-click an actor**. A small menu appears bottom-left; pick the
   dialogue editor.
2. Edit the graph:

| Action                      | Control                          |
|-----------------------------|----------------------------------|
| Open the graph context menu | Right-click the graph background |
| Move a node                 | Left-click and drag the node     |
| Pan the graph               | Middle-click and drag            |
| Zoom                        | Scroll wheel                     |
| Open a subgraph             | Double-click a subgraph node     |

3. Click **Save Graph**. The status under the buttons says where the file went, or why it was not
   saved. Files land in `Mods/Modding_API/NpcDialogues/`, named after the tree's key (or its name).

- A graph needs a start node to save. If you delete it, pick a new one with **MainNode** in the
  context menu.
- Connections the graph does not allow (a node with no children, a target that already has a
  parent) are refused with a message.
- A node the editor cannot show becomes a red box; the rest stays editable.
- Settings without a dedicated editor are listed by name and editable when they are a number,
  text, true/false or a choice. Settings linked to a blackboard variable are read-only. Flag
  choices (such as times of day) are typed as comma-separated names, for example `Morning, Noon`.

The same editor also edits routine behaviour trees; see [Routines](./routines.md).

## Option B: build dialogue in code

`DialogueNodeCreator` wraps a `DialogueTree` and creates typed nodes. Construct it with the tree
and the player's `ActorParameter`, create nodes, then connect them on the tree:

```csharp
using Drova_Modding_API.Systems.Dialogues;
using Il2CppNodeCanvas.DialogueTrees;

DialogueNodeCreator creator = new(tree, playerParam);

DS_StatementNode line = creator.CreateStatementNode(myStatement, npcActorParameter);
DS_GiveExp xp = creator.CreateExpNode(500);

tree.ConnectNodes(line, xp);
```

This means working with game types (`DS_Statement`, `ActorParameter`, `EntityInfo`, ...) directly.
Use the editor unless you really need generated dialogue.

### Node creators

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

All methods are `virtual`.

## Give a spawned NPC a saved dialogue

`DialogueModule` takes the id of a dialogue saved by the editor (the file name in `NpcDialogues/`)
and puts it on an NPC built with [`NpcCreator`](./spawning.md):

```csharp
using Drova_Modding_API.Systems.Spawning;
using Drova_Modding_API.Systems.Spawning.Modules;

new NpcCreator("Talker", position)
    .WithLazyEntityInfo(entityInfo)
    .WithModule(new DialogueModule("my_dialogue_id"))
    .CreateLazy(saveToLazyActorStore: true);
```

## Gotchas

- **An unconnected choice ends the dialogue.** Never set a choice's `isEndNode` yourself: the game
  only sets it on the "back" choice a hub adds at runtime, and a stored one throws.
- **Quests** use vanilla nodes: `DS_SetQuestStateNode` sets a state, `GQuestStateConditionTask`
  checks one. A state set from dialogue raises `GameEvents.OnQuestStateChanged`. See
  [Quests](./quests.md).
- **Audio is separate.** To give lines spoken audio, see [Audio](./audio.md).
- `TraderModule` / `TeacherModule` only make an NPC a merchant/trainer. The dialogue still needs
  the node that opens the trade or teach window.
