using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues;
using Drova_Modding_API.Systems.Dialogues.Store;
using Drova_Modding_API.Systems.Testing;
using Il2CppDrova;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.DialogueSystem.DialogueTrees;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.GlobalVarSystem.Graphs;
using Il2CppDrova.InteractionSystem;
using Il2CppDrova.Items;
using Il2CppDrova.JournalSystem;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.QuestSystem.Graphs;
using Il2CppInterop.Runtime;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using UnityEngine;
using static Il2CppNodeCanvas.DialogueTrees.DialogueTree;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// Generates the dialogues of <c>samples/QuestShowcase</c> in a running game, through the same <see cref="DialogueStore"/> modders save with.
    /// <c>questshowcase hunter|scout &lt;guid&gt;</c> writes that quest giver's dialogue,
    /// <c>questshowcase vanilla &lt;tree&gt;</c> adds the apple quest branch to the hub of a vanilla dialogue tree,
    /// <c>questshowcase category &lt;quest&gt; &lt;entry&gt;</c> answers the journal category the entry's topic sits in,
    /// <c>questshowcase subtrees &lt;tree&gt;</c> lists a tree's sub-dialogue trees, <c>questshowcase marksub &lt;tree&gt;</c> stores a tree whose first line is replaced, to see a stored sub-tree apply,
    /// <c>questshowcase mapadd &lt;prefab&gt; x y</c> / <c>mapremove &lt;id&gt;</c> / <c>mapeditor</c> drive the map editor.
    /// </summary>
    internal static class QuestShowcaseBuilder
    {
        private const string LocaPath = "QuestShowcase";
        private const string HunterList = "Q_Showcase_RipperHunt";
        private const string ApplesList = "Q_Showcase_Apples";
        private const string TracksList = "Q_Showcase_Tracks";

        internal static void Register() => TestAgent.RegisterCommand("questshowcase", Run);

        private static string Run(string rest)
        {
            string[] parts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string arg = parts.Length > 1 ? parts[1] : string.Empty;
            return (parts.Length > 0 ? parts[0] : string.Empty) switch
            {
                "hunter" => BuildQuestGiver(arg, "showcase_hunter", HunterList, ["RipperDead"], "Hunter"),
                "scout" => BuildQuestGiver(arg, "showcase_scout", TracksList, ["ShrineReached", "SuppliesTaken"], "Scout"),
                "vanilla" => ExtendVanilla(arg),
                "category" => Category(arg),
                "subtrees" => SubTrees(arg),
                "marksub" => MarkFirstLine(arg),
                "pickup" => DescribePickup(arg),
                "bundles" => BundlesWith(arg),
                "dynamic" => DynamicPrefabs(),
                "item" => DescribeItem(arg),
                "reloaditems" => ReloadItems(),
                "mapadd" => MapAdd(arg),
                "mapeditor" => ToggleMapEditor(),
                "mapremove" => MapRemove(arg),
                "mapblocker" => MapAdd("- " + arg),
                "maphide" => MapHide(arg),
                "mapunhide" => MapUnhide(),
                "mapactive" => MapActive(arg),
                "mapcopy" => MapCopy(arg),
                "mapcheck" => MapCheck(arg),
                "mapdestroy" => MapDestroy(arg),
                "maptemplates" => MapEditor.MapSceneTemplates.HeldScenes.ToString(),
                "mapcrime" => MapCrime(arg),
                "letters" => Letters(),
                "itemdump" => ItemDump(arg),
                "abitree" => AbilityTree(arg),
                "letterwindow" => LetterWindow(),
                "mapdef" => System.Text.Json.JsonSerializer.Serialize(MapEditor.MapLayoutSystem.Objects.TryGetValue(arg, out MapEditor.MapObjectDefinition? found) ? found : null),
                "maparea" => System.Text.Json.JsonSerializer.Serialize(MapEditor.MapAreas.All.FirstOrDefault(area => area.Id == arg)),
                "mapids" => string.Join(",", MapEditor.MapLayoutSystem.Objects.Keys.OrderBy(id => id)) + " hidden=" + MapEditor.MapLayoutSystem.RemovedCount + " areas=" + MapEditor.MapAreas.All.Count,
                "weatherarea" => Il2CppDrova.Weather.WeatherGameHandler.TryGet(out Il2CppDrova.Weather.WeatherGameHandler? weather) && weather != null ? weather.CurAreaType.ToString() : "none",
                "components" => Components(arg),
                "mapdynamic" => MapDynamic(arg),
                "mapstamp" => MapStamp(arg),
                "mapterrain" => MapTerrain(arg),
                "mapgate" => MapGate(arg),
                "mapgap" => MapGap(arg),
                "abi" => DescribeAbilities(arg),
                _ => throw new InvalidOperationException("usage: questshowcase hunter|scout|vanilla|category|subtrees|marksub|pickup|bundles|dynamic <arg>")
            };
        }

        /// <summary>
        /// A quest giver: offers the quest, pays once every bool is set, and pays at once when they were set before the offer.
        /// </summary>
        private static string BuildQuestGiver(string actorGuid, string treeName, string listName, string[] bools, string prefix)
        {
            Actor actor = ProviderAccess.GetEntityGameHandler()?.GetActorFromGuid(actorGuid) ?? throw new InvalidOperationException("actor not loaded: " + actorGuid);
            GQuestState quest = QuestStateOf(listName);
            GBool[] goals = bools.Select(id => GBoolOf(listName, id)).ToArray();

            DialogueTree tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.name = treeName;
            tree.actorParameters = new Il2CppCollections.List<ActorParameter>();
            ActorParameter giver = new() { _keyName = actor.name, ActorGuid = actor.GetGuidComponent()._guidString, _id = Il2CppSystem.Guid.NewGuid().ToString() };
            ActorParameter player = ActorParameterHelper.GetPlayerActorParameters();
            tree.actorParameters.Add(giver);
            tree.actorParameters.Add(player);
            DialogueNodeCreator nodes = new(tree, player);

            ConditionNode isDone = Condition(tree, QuestIs(quest, QuestState.IsCompleted), 0, 0);
            ConditionNode isRunning = Condition(tree, QuestIs(quest, QuestState.IsRunning), 1, 1);
            ConditionNode doneWhileRunning = Condition(tree, BoolsAreSet(goals), 2, 2);
            ConditionNode doneBeforeQuest = Condition(tree, BoolsAreSet(goals), 2, 0);

            DS_StatementNode thanks = Say(nodes, giver, prefix + "_Thanks", 1, -1);
            DS_StatementNode waiting = Say(nodes, giver, prefix + "_Waiting", 3, 3);
            DS_StatementNode reward = Say(nodes, giver, prefix + "_Reward", 3, 2);
            DS_StatementNode already = Say(nodes, giver, prefix + "_AlreadyDone", 3, 0);
            DS_StatementNode offer = Say(nodes, giver, prefix + "_Offer", 3, -1);
            DS_MultipleChoiceNode answer = nodes.CreateMultipleChoices(
            [
                new ChoiceCreationData { Statement = Line("Player_Accept") },
                new ChoiceCreationData { Statement = Line("Player_Decline"), IsEndNode = true }
            ]);
            Place(answer, 4, -1);
            DS_SetQuestStateNode start = SetQuest(tree, quest, QuestState.IsRunning, 5, -1);
            DS_SetQuestStateNode completeLate = SetQuest(tree, quest, QuestState.IsCompleted, 4, 2);
            DS_SetQuestStateNode completeEarly = SetQuest(tree, quest, QuestState.IsCompleted, 4, 0);
            DS_GiveItemNode payLate = Pay(nodes, giver, 5, 2);
            DS_GiveItemNode payEarly = Pay(nodes, giver, 5, 0);

            tree.ConnectNodes(isDone, thanks);
            tree.ConnectNodes(isDone, isRunning);
            tree.ConnectNodes(isRunning, doneWhileRunning);
            tree.ConnectNodes(isRunning, doneBeforeQuest);
            tree.ConnectNodes(doneWhileRunning, reward);
            tree.ConnectNodes(doneWhileRunning, waiting);
            tree.ConnectNodes(reward, completeLate);
            tree.ConnectNodes(completeLate, payLate);
            tree.ConnectNodes(doneBeforeQuest, already);
            tree.ConnectNodes(doneBeforeQuest, offer);
            tree.ConnectNodes(already, completeEarly);
            tree.ConnectNodes(completeEarly, payEarly);
            tree.ConnectNodes(offer, answer);
            tree.ConnectNodes(answer, start);
            tree.primeNode = isDone;

            return Save(tree.name, tree);
        }

        private static string ExtendVanilla(string treeName)
        {
            DialogueTree tree = FindTree(treeName);
            DS_MultipleChoiceNode hub = FindFirst<DS_HubNode>(tree) ?? FindFirst<DS_MultipleChoiceNode>(tree) ?? throw new InvalidOperationException("no choice node in " + treeName);
            ActorParameter npc = FirstNpc(tree);
            ActorParameter player = ActorParameterHelper.GetPlayerActorParameters();
            DialogueNodeCreator nodes = new(tree, player);
            GQuestState quest = QuestStateOf(ApplesList);
            Item apple = ProviderAccess.ItemDatabase?.GetItemByReadableId("item_apple") ?? throw new InvalidOperationException("no item_apple");

            hub.availableChoices.Add(new DS_MultipleChoiceNode.Choice
            {
                statement = Line("Player_AskHelp"),
                condition = QuestIs(quest, QuestState.IsCompleted, false),
                UID = Il2CppSystem.Guid.NewGuid().ToString()
            });

            ConditionNode isRunning = Condition(tree, QuestIs(quest, QuestState.IsRunning), 20, 0);
            DS_HasItems hasApplesTask = new() { Items = new Il2CppCollections.List<DialogItems>() };
            hasApplesTask.Items.Add(new DialogItems { Item = apple, Mode = DialogItems.ValueMode.Int, Amount = 3 });
            ConditionNode hasApples = Condition(tree, hasApplesTask, 21, 0);
            DS_StatementNode ask = Say(nodes, npc, "Npc_WantsApples", 21, 2);
            DS_SetQuestStateNode start = SetQuest(tree, quest, QuestState.IsRunning, 22, 2);
            DS_StatementNode noApples = Say(nodes, npc, "Npc_NoApplesYet", 22, 1);
            DS_GiveItemNode take = nodes.CreateGiveItemNode(
                [new DialogItemsExchange { Item = apple, Exchange = DialogItemsExchange.ExchangeDirection.PlayerToNPC, Mode = DialogItems.ValueMode.Int, Amount = 3 }], npc);
            Place(take, 22, 0);
            DS_SetQuestStateNode complete = SetQuest(tree, quest, QuestState.IsCompleted, 23, 0);
            DS_StatementNode thanks = Say(nodes, npc, "Npc_ApplesThanks", 24, 0);

            tree.ConnectNodes(hub, isRunning);
            tree.ConnectNodes(isRunning, hasApples);
            tree.ConnectNodes(isRunning, ask);
            tree.ConnectNodes(hasApples, take);
            tree.ConnectNodes(hasApples, noApples);
            tree.ConnectNodes(ask, start);
            tree.ConnectNodes(take, complete);
            tree.ConnectNodes(complete, thanks);
            BackToHub(tree, nodes, hub, start, 23, 2);
            BackToHub(tree, nodes, hub, noApples, 23, 1);
            BackToHub(tree, nodes, hub, thanks, 25, 0);

            return Save(tree.name, tree);
        }

        /// <summary>
        /// Vanilla trees return to their hub through a jump node on the hub's tag.
        /// </summary>
        private static void BackToHub(DialogueTree tree, DialogueNodeCreator nodes, DS_MultipleChoiceNode hub, Node from, int column, int row)
        {
            if (string.IsNullOrEmpty(hub.tag))
            {
                tree.ConnectNodes(from, hub);
                return;
            }

            DS_HubJumpNode jump = nodes.CreateHubJumpNode(hub.tag);
            Place(jump, column, row);
            tree.ConnectNodes(from, jump);
        }

        private static string SubTrees(string treeName)
        {
            DialogueTree tree = FindTree(treeName);
            List<string> names = [];
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                SubDialogueTree? node = tree.allNodes[i].TryCast<SubDialogueTree>();
                if (node != null && node.subGraph != null)
                {
                    names.Add(node.subGraph.name);
                }
            }

            return names.Count == 0 ? "none" : string.Join(";", names);
        }

        private static string MarkFirstLine(string treeName)
        {
            // A copy is changed and stored, so the live tree only changes when the stored file is applied to it.
            DialogueTree copy = Object.Instantiate(FindTree(treeName));
            copy.DeserializeIfNotDoneYet(true);
            DS_StatementNode node = FindFirst<DS_StatementNode>(copy) ?? throw new InvalidOperationException("no statement in " + treeName);
            node.statement = Line("Npc_SubTreeOverride");
            return Save(treeName, copy);
        }

        private static string DescribePickup(string name)
        {
            GameObject root = GameObject.Find(name) ?? throw new InvalidOperationException("no object " + name);
            List<string> parts = [$"scene={root.scene.name} active={root.activeInHierarchy}"];
            foreach (InteractableTrigger trigger in root.GetComponentsInChildren<InteractableTrigger>(true))
            {
                parts.Add($"trigger={trigger.name} active={trigger.isActiveAndEnabled} count={trigger._interactionContainer?._interactions?.Count}");
                Il2CppCollections.List<Interaction>? list = trigger._interactionContainer?._interactions;
                for (int i = 0; list != null && i < list.Count; i++)
                {
                    Interaction? interaction = list[i];
                    InteractionCategory? category = interaction != null ? interaction.InteractionCategory() : null;
                    parts.Add(interaction == null ? "null" : $"{interaction.name} inRoot={interaction.transform.IsChildOf(root.transform)} active={interaction.isActiveAndEnabled} category={(category != null ? category.name + "/" + category.Stance + "/" + category.AdditionalStance : "null")}");
                }
            }

            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                parts.Add($"sprite {renderer.name}={(renderer.sprite != null ? renderer.sprite.name : "null")} active={renderer.gameObject.activeInHierarchy}");
            }

            return string.Join("; ", parts);
        }

        private static string BundlesWith(string fragment)
        {
            List<string> found = [];
            foreach (AssetBundle bundle in Resources.FindObjectsOfTypeAll<AssetBundle>())
            {
                foreach (string asset in bundle.GetAllAssetNames())
                {
                    if (asset.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    {
                        found.Add(bundle.name + ":" + asset);
                    }
                }
            }

            return found.Count == 0 ? "none" : string.Join("; ", found.Take(20));
        }

        private static string DescribeItem(string readableId)
        {
            Item item = ProviderAccess.ItemDatabase?.GetItemByReadableId(readableId) ?? throw new InvalidOperationException("no item " + readableId);
            List<string> parts = [$"name={item.GetLocalizedItemName()} guid={item.Guid} buy={item.BuyValue} sell={item.SellValue}"];
            if (item.Icon != null)
            {
                parts.Add($"icon={item.Icon.name} {item.Icon.rect.width}x{item.Icon.rect.height} ppu={item.Icon.pixelsPerUnit}");
            }

            for (int i = 0; i < item._itemBehaviours.Count; i++)
            {
                ItemBhvr_ActorAction_FromPrefab? action = item._itemBehaviours[i].TryCast<ItemBhvr_ActorAction_FromPrefab>();
                Il2CppDrova.ActorActions.AA_Trigger_StatusEffect? trigger = action != null && action._actionPrefab != null
                    ? action._actionPrefab.GetComponentInChildren<Il2CppDrova.ActorActions.AA_Trigger_StatusEffect>(true)
                    : null;
                if (trigger == null || trigger._statusEffect == null)
                {
                    continue;
                }

                Il2CppDrova.StatusEffects.StatusEffect? saved = Il2CppDrova.GameDatabase.Instance.StatusEffectDatabase.GetStatusEffectByGuid(trigger._statusEffect.GUID);
                parts.Add($"action={action!._actionPrefab.name} effect={trigger._statusEffect.name} stacks={trigger._statusEffect.StackAmount} custom={trigger._hasCustomStacks}/{trigger._stacksToAdd} saved={(saved != null ? saved.name : "none")}");
                for (int b = 0; b < trigger._statusEffect._effectsBhvrs.Count; b++)
                {
                    Il2CppDrova.StatusEffects.StatusEffectBhvr_SpawnAbility? spawn = trigger._statusEffect._effectsBhvrs[b].TryCast<Il2CppDrova.StatusEffects.StatusEffectBhvr_SpawnAbility>();
                    for (int a = 0; spawn != null && spawn._abilities != null && a < spawn._abilities.Count; a++)
                    {
                        parts.Add(DescribeAbility(spawn._abilities[a]));
                    }
                }
            }

            return string.Join("; ", parts);
        }

        private static string MapAdd(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            return MapEditor.MapLayoutSystem.Add(parts[0] == "-" ? string.Empty : parts[0], new Vector2(x, y)).Id;
        }

        private static string MapHide(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Vector2 world = new(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
            Transform? target = MapEditor.MapEditorWindow.PickVanilla(world);
            if (target == null)
            {
                throw new InvalidOperationException("nothing vanilla at " + world);
            }

            for (int up = parts.Length > 2 ? int.Parse(parts[2]) : 0; up > 0 && target.parent != null; up--)
            {
                target = target.parent;
            }

            MapEditor.MapRemovalDefinition removal = MapEditor.MapLayoutSystem.Hide(target.gameObject);
            return removal.Scene + "|" + removal.Path;
        }

        private static string MapCopy(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] source = parts[0].Split('|');
            return MapEditor.MapLayoutSystem.Add(new MapEditor.MapObjectDefinition
            {
                Scene = source[0],
                Path = source[1],
                X = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                Y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                Seed = "test_" + System.IO.Path.GetFileName(source[1]),
                Locked = false,
                Loot = parts.Length > 3
                    ? parts.Skip(3).Select(stack => stack.Split(':')).Select(stack => new MapEditor.MapLootDefinition { Item = stack[0], Amount = stack.Length > 1 ? int.Parse(stack[1]) : 1 }).ToList()
                    : null
            }).Id;
        }

        private static string Components(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Vector2 near = new(float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
            GameObject? best = null;
            foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidate != null && candidate.name == parts[0] && candidate.gameObject.scene.IsValid()
                    && (best == null || Vector2.Distance(candidate.position, near) < Vector2.Distance(best.transform.position, near)))
                {
                    best = candidate.gameObject;
                }
            }

            if (best == null)
            {
                throw new InvalidOperationException("not found");
            }

            List<string> lines = [];
            foreach (Transform node in best.GetComponentsInChildren<Transform>(true))
            {
                List<string> names = [];
                foreach (Component component in node.GetComponents<Component>())
                {
                    Behaviour? behaviour = component.TryCast<Behaviour>();
                    Il2CppDrova.InteractionSystem.Interaction? interaction = component.TryCast<Il2CppDrova.InteractionSystem.Interaction>();
                    if (interaction != null)
                    {
                        names.Add("category=" + (interaction._interactionCategory != null ? interaction._interactionCategory.name : "null"));
                    }

                    SpriteRenderer? sprite = component.TryCast<SpriteRenderer>();
                    string detail = sprite == null ? string.Empty
                        : "(" + (sprite.sprite != null ? sprite.sprite.name + "@" + (sprite.sprite.texture != null ? sprite.sprite.texture.name : "notex") : "nosprite") + (sprite.enabled ? "" : ",off") + ")";
                    names.Add(component.GetIl2CppType().Name + (behaviour != null && !behaviour.enabled ? "(off)" : string.Empty) + detail);
                }

                lines.Add(node.name + (node.gameObject.activeSelf ? "" : "(inactive)") + ": " + string.Join(",", names));
            }

            return string.Join(" ; ", lines);
        }

        private static string ItemDump(string readableId)
        {
            Item? item = ProviderAccess.ItemDatabase?.GetItemByReadableId(readableId);
            if (item == null) throw new InvalidOperationException("no item " + readableId);
            List<string> lines = ["sub=" + item.Category.SubCategory];
            for (int i = 0; i < item._itemBehaviours.Count; i++)
            {
                lines.Add("bhvr " + item._itemBehaviours[i].GetIl2CppType().Name);
                Il2CppDrova.Items.ItemBhvr_ActorAction_FromPrefab? action = item._itemBehaviours[i].TryCast<Il2CppDrova.Items.ItemBhvr_ActorAction_FromPrefab>();
                if (action != null && action._actionPrefab != null)
                {
                    lines.Add("action " + action._actionPrefab.name);
                    foreach (Il2CppDrova.ActorActions.AA_Trigger_Ability trigger in action._actionPrefab.GetComponentsInChildren<Il2CppDrova.ActorActions.AA_Trigger_Ability>(true))
                    {
                        for (int c = 0; trigger._chargeLevels != null && c < trigger._chargeLevels.Count; c++)
                        {
                            lines.Add("ability " + (trigger._chargeLevels[c] != null ? trigger._chargeLevels[c].name : "null"));
                        }
                    }
                }

                Il2CppDrova.Items.ItemBhvr_Instant_LearnSpell? learn = item._itemBehaviours[i].TryCast<Il2CppDrova.Items.ItemBhvr_Instant_LearnSpell>();
                if (learn != null)
                {
                    lines.Add("learns " + (learn._spellToLearn != null ? learn._spellToLearn.ReadableId : "null") + " runestone=" + learn._needsRunestone);
                }
            }

            return string.Join(" ; ", lines);
        }

        private static string Letters()
        {
            if (!PlayerMetaDataGameHandler.TryGet(out PlayerMetaDataGameHandler handler) || handler == null) throw new InvalidOperationException("no journal yet");
            List<string> paths = [];
            foreach (Il2CppCollections.KeyValuePair<AJournalTopic, Journal_RuntimeTopic> pair in handler.GetJournal()._topicLookUp)
            {
                if (pair.Key != null && pair.Key.TryCast<JournalLetterTopic>() != null)
                {
                    paths.Add(pair.Key.GetResourcesPath());
                }
            }

            return paths.Count == 0 ? "none" : string.Join(";", paths.OrderBy(path => path));
        }

        private static string LetterWindow()
        {
            foreach (Il2CppDrova.GUI.GUI_Window_Letter window in Object.FindObjectsOfType<Il2CppDrova.GUI.GUI_Window_Letter>())
            {
                Il2CppTMPro.TextMeshProUGUI? text = window.GetComponentInChildren<Il2CppTMPro.TextMeshProUGUI>(true);
                return text != null ? text.text : "no text";
            }

            return "none";
        }

        private static string MapCrime(string id)
        {
            if (!MapEditor.MapLayoutSystem.TryGetInstance(id, out GameObject? instance))
            {
                throw new InvalidOperationException(id + " is not loaded");
            }

            Interact_Bhvr_Crime? crime = instance.GetComponentInChildren<Interact_Bhvr_Crime>(true);
            return crime == null ? "none" : crime.Crime.IsCrimeDisabled ? "off" : "on";
        }

        private static string MapDestroy(string id)
        {
            if (!MapEditor.MapLayoutSystem.TryGetInstance(id, out GameObject? instance))
            {
                throw new InvalidOperationException(id + " is not loaded");
            }

            Object.Destroy(instance);
            return "destroyed";
        }

        private static string MapCheck(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int step = int.Parse(parts[0]);
            int offset = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            int ready = 0;
            int pending = 0;
            List<string> missing = [];
            for (int i = offset; i < MapEditor.MapScenePropCatalog.Props.Length; i += step)
            {
                (string name, string _, string scene, string path) = MapEditor.MapScenePropCatalog.Props[i];
                if (!MapEditor.MapSceneTemplates.TryGet(scene, path, out GameObject? template))
                {
                    pending++;
                }
                else if (template == null)
                {
                    missing.Add(name);
                }
                else
                {
                    ready++;
                }
            }

            return $"ready={ready} pending={pending} missing={missing.Count} {string.Join(",", missing)}";
        }

        private static string MapStamp(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] source = parts[0].Split('|');
            return MapEditor.MapLayoutSystem.Add(new MapEditor.MapObjectDefinition
            {
                Prefab = source.Length == 1 ? parts[0] : string.Empty,
                Scene = source.Length == 2 ? source[0] : string.Empty,
                Path = source.Length == 2 ? source[1] : string.Empty,
                SortingOrder = source.Length == 2 ? -16 : null,
                Stamp = true,
                X = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                Y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                Terrain = parts.Length > 3 ? parts[3] : null,
                Color = parts.Length > 4 ? parts[4] : null
            }).Id;
        }

        private static string MapGate(string rest)
        {
            float[] v = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => float.Parse(part, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return MapEditor.MapLayoutSystem.Add(new MapEditor.MapObjectDefinition
            {
                Scene = "Scene_Teleporters",
                Path = "Teleporter_Atlanterdungeon_Enter",
                X = v[0],
                Y = v[1],
                TargetX = v[2],
                TargetY = v[3]
            }).Id;
        }

        private static string MapTerrain(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Il2CppDrova.Terrain.Terrain2D? terrain = Object.FindObjectOfType<Il2CppDrova.Terrain.Terrain2D>();
            if (terrain == null) throw new InvalidOperationException("no terrain");
            Vector2 at = new(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
            return terrain.GetTerrainAt(at).ToString();
        }

        private static string MapDynamic(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return MapEditor.MapLayoutSystem.Add(new MapEditor.MapObjectDefinition
            {
                Dynamic = parts[0],
                X = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                Y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)
            }).Id;
        }

        private static string MapActive(string key)
        {
            string[] parts = key.Split('|');
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(parts[0]);
            GameObject? target = scene.IsValid() ? MapEditor.MapLayoutSystem.Find(scene, parts[1]) : null;
            return target == null ? "missing" : target.activeSelf ? "1" : "0";
        }

        private static string MapUnhide()
        {
            IReadOnlyList<MapEditor.MapRemovalDefinition> removed = MapEditor.MapLayoutSystem.Removed;
            if (removed.Count > 0)
            {
                MapEditor.MapLayoutSystem.Unhide(removed[^1]);
            }

            return MapEditor.MapLayoutSystem.Removed.Count + " hidden";
        }

        private static string MapGap(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Vector3 center = new(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), 0f);
            Vector3 nearest = Il2Cpp.AstarPath.active != null ? (Vector3)Il2Cpp.AstarPath.active.GetNearest(center).position : center;
            return Vector2.Distance(center, nearest).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string MapRemove(string id)
        {
            MapEditor.MapLayoutSystem.Remove(id);
            return MapEditor.MapLayoutSystem.Objects.Count + " prop(s) left";
        }

        private static string ToggleMapEditor()
        {
            MapEditor.MapEditorWindow.Toggle();
            return "toggled";
        }

        private static string ReloadItems()
        {
            Items.CustomItemSystem.Reload();
            return Items.CustomItemSystem.Items.Count + " custom item(s)";
        }

        private static string DescribeAbilities(string fragment)
        {
            List<string> parts = [];
            foreach (Il2CppDrova.ABI.ABI_Ability ability in Resources.FindObjectsOfTypeAll<Il2CppDrova.ABI.ABI_Ability>())
            {
                if (ability != null && ability.name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add(DescribeAbility(ability));
                }
            }

            return parts.Count == 0 ? "none" : string.Join("; ", parts);
        }

        private static string AbilityTree(string name)
        {
            Il2CppDrova.ABI.ABI_Ability? root = Resources.FindObjectsOfTypeAll<Il2CppDrova.ABI.ABI_Ability>().FirstOrDefault(ability => ability != null && ability.name == name);
            if (root == null) throw new InvalidOperationException("no ability " + name);
            List<string> lines = [];
            HashSet<IntPtr> seen = [];
            void Walk(Il2CppDrova.ABI.ABI_Ability ability, int depth)
            {
                if (depth > 4 || !seen.Add(ability.Pointer)) return;
                string pad = new('-', depth);
                lines.Add(pad + DescribeAbility(ability) + " prefab=" + (ability.AbilityPrefab != null ? ability.AbilityPrefab.name : "null"));
                if (ability.AbilityPrefab == null) return;
                foreach (Component component in ability.AbilityPrefab.GetComponentsInChildren<Component>(true))
                {
                    Il2CppSystem.Type type = component.GetIl2CppType();
                    lines.Add(pad + " " + component.gameObject.name + ":" + type.Name);
                    for (Il2CppSystem.Type? current = type; current != null && current.Name != "MonoBehaviour"; current = current.BaseType)
                    {
                        foreach (Il2CppSystem.Reflection.FieldInfo field in current.GetFields(Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.DeclaredOnly))
                        {
                            Il2CppSystem.Object? value;
                            try
                            {
                                value = field.GetValue(component);
                            }
                            catch
                            {
                                continue;
                            }

                            Il2CppDrova.ABI.ABI_Ability? single = value?.TryCast<Il2CppDrova.ABI.ABI_Ability>();
                            if (single != null)
                            {
                                lines.Add(pad + "  ." + field.Name);
                                Walk(single, depth + 1);
                            }

                            Il2CppCollections.List<Il2CppDrova.ABI.ABI_Ability>? list = value?.TryCast<Il2CppCollections.List<Il2CppDrova.ABI.ABI_Ability>>();
                            for (int k = 0; list != null && k < list.Count; k++)
                            {
                                if (list[k] == null) continue;
                                lines.Add(pad + "  ." + field.Name + "[" + k + "]");
                                Walk(list[k], depth + 1);
                            }
                        }
                    }
                }
            }

            Walk(root, 0);
            return string.Join(" ; ", lines);
        }

        private static string DescribeAbility(Il2CppDrova.ABI.ABI_Ability ability)
        {
            List<string> values = [];
            Il2CppCollections.List<Il2CppDrova.ABI.ABIBaseData> properties = ability.GetABIDataContainer()._abilityProperties;
            for (int i = 0; properties != null && i < properties.Count; i++)
            {
                values.Add(properties[i].Key.Name + "=" + AbilityDataAccess.Describe(properties[i]));
            }

            return ability.name + "{" + string.Join(",", values) + "}";
        }

        private static string DynamicPrefabs()
        {
            List<string> found = [];
            Il2CppCollections.List<Il2CppDrova.Saveables.SubDatabase_DynamicSaveObjects.Entry> entries = Il2CppDrova.GameDatabase.Instance.DynamicObjects._data;
            for (int i = 0; i < entries.Count; i++)
            {
                Il2CppDrova.Saveables.SaveRoot_Dynamic prefab = entries[i].Prefab;
                bool loot = prefab != null && prefab.GetComponentInChildren<Interact_Bhvr_LootAll>(true) != null;
                found.Add(entries[i].AssetId + "=" + (prefab != null ? prefab.name : "null") + (loot ? "[loot]" : ""));
            }

            return string.Join("; ", found);
        }

        private static string Category(string rest)
        {
            string[] ids = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            JournalEntry entry = QuestSystem.GetJournalEntry(ids[0], ids[1]) ?? throw new InvalidOperationException("no entry " + rest);
            if (!PlayerMetaDataGameHandler.TryGet(out PlayerMetaDataGameHandler handler) || handler == null) throw new InvalidOperationException("no journal yet");

            Journal_RuntimeTopic? topic = handler.GetJournal().GetRuntimeTopic(entry.GetRelatedTopic());
            return topic == null ? "none" : topic.Category.name;
        }

        private static string Save(string id, DialogueTree tree)
        {
            tree.Serialize(null);
            return DialogueStore.SaveDialogue(id, tree) ? "saved " + DialogueStore.GetDialogueFilePath(id) : throw new InvalidOperationException("save failed for " + id);
        }

        private static ConditionNode Condition(DialogueTree tree, ConditionTask condition, int column, int row)
        {
            ConditionNode node = tree.AddNode<ConditionNode>();
            node._condition = condition;
            node.TryGenerateUID();
            Place(node, column, row);
            return node;
        }

        private static DS_StatementNode Say(DialogueNodeCreator nodes, ActorParameter actor, string key, int column, int row)
        {
            DS_StatementNode node = nodes.CreateStatementNode(Line(key), actor);
            Place(node, column, row);
            return node;
        }

        private static DS_SetQuestStateNode SetQuest(DialogueTree tree, GQuestState quest, QuestState value, int column, int row)
        {
            DS_SetQuestStateNode node = tree.AddNode<DS_SetQuestStateNode>();
            node.QuestState = new BBParameter<GQuestState>();
            node.QuestState.value = quest;
            node.Value = new BBParameter<QuestState>();
            node.Value.value = value;
            node.TryGenerateUID();
            Place(node, column, row);
            return node;
        }

        private static DS_GiveItemNode Pay(DialogueNodeCreator nodes, ActorParameter actor, int column, int row)
        {
            Item reward = ProviderAccess.ItemDatabase?.GetItemByReadableId("item_beer") ?? throw new InvalidOperationException("no reward item");
            DS_GiveItemNode node = nodes.CreateGiveItemNode(
                [new DialogItemsExchange { Item = reward, Exchange = DialogItemsExchange.ExchangeDirection.VoidToPlayer, Mode = DialogItems.ValueMode.Int, Amount = 3 }], actor);
            Place(node, column, row);
            return node;
        }

        private static DS_Statement Line(string key) => new() { useGlobalLoca = true, GlobalLocaPath = LocaPath, locaKey = key };

        private static void Place(Node node, int column, int row) => node.position = new Vector2(column * 480f, row * 260f + 600f);

        /// <summary>
        /// The comparer is an enum nested in a generic, which the interop cannot bind, so it is set through IL2CPP reflection.
        /// </summary>
        private static GQuestStateConditionTask QuestIs(GQuestState quest, QuestState value, bool equals = true)
        {
            GraphGQuestCompService service = new()
            {
                Variable = new BBParameter<GQuestState>(),
                Value = new BBParameter<QuestState>()
            };
            service.Variable.value = quest;
            service.Value.value = value;

            Il2CppSystem.Reflection.FieldInfo field = Il2CppType.Of<GraphGQuestCompService>().GetField("Comparison",
                Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Public);
            BBParameter comparison = field.GetValue(service)?.Cast<BBParameter>() ?? Il2CppSystem.Activator.CreateInstance(field.FieldType).Cast<BBParameter>();
            comparison.value = Il2CppSystem.Enum.ToObject(comparison.varType, equals ? 0 : 1);
            field.SetValue(service, comparison);

            GQuestStateConditionTask task = new() { Conditions = new Il2CppCollections.List<GraphGQuestCompService>() };
            task.Conditions.Add(service);
            return task;
        }

        private static GBoolConditionTask BoolsAreSet(GBool[] variables)
        {
            GBoolConditionTask task = new() { Conditions = new Il2CppCollections.List<GraphGBoolCompService>() };
            foreach (GBool variable in variables)
            {
                GraphGBoolCompService service = new()
                {
                    Variable = new BBParameter<GBool>(),
                    Comparison = new BBParameter<GBool.Comparer>(),
                    Value = new BBParameter<bool>()
                };
                service.Variable.value = variable;
                service.Value.value = true;
                task.Conditions.Add(service);
            }

            return task;
        }

        private static GVarList ListOf(string name)
            => ProviderAccess.GVarDatabase?.GetGVarListByName(name) ?? throw new InvalidOperationException("no gvar list " + name);

        private static GQuestState QuestStateOf(string list) => ListOf(list).GetQuestState() ?? throw new InvalidOperationException(list + " has no QuestState");

        private static GBool GBoolOf(string list, string id)
            => ListOf(list).GetGVarById(id)?.TryCast<GBool>() ?? throw new InvalidOperationException(list + " has no bool " + id);

        private static DialogueTree FindTree(string name)
        {
            foreach (DialogueTree tree in Resources.FindObjectsOfTypeAll<DialogueTree>())
            {
                if (tree != null && tree.name == name)
                {
                    if (tree.allNodes == null || tree.allNodes.Count == 0)
                    {
                        tree.SelfDeserialize();
                        tree.DeserializeIfNotDoneYet(true);
                    }

                    return tree;
                }
            }

            throw new InvalidOperationException("no dialogue tree " + name);
        }

        private static T? FindFirst<T>(DialogueTree tree) where T : Node
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                T? node = tree.allNodes[i].TryCast<T>();
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        private static ActorParameter FirstNpc(DialogueTree tree)
        {
            for (int i = 0; i < tree.actorParameters.Count; i++)
            {
                if (tree.actorParameters[i]._keyName != "Player")
                {
                    return tree.actorParameters[i];
                }
            }

            throw new InvalidOperationException("no npc actor parameter in " + tree.name);
        }
    }
}
