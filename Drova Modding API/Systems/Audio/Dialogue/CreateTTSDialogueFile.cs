using Drova_Modding_API.Systems.Audio.Dialogue.Dynamic;
using Drova_Modding_API.Systems.Audio.Dialogue.Generic;
using Drova_Modding_API.Systems.Editor;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using System.Text;
using UnityEngine;
using static Il2CppNodeCanvas.DialogueTrees.DialogueTree;

namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    /// <summary>
    /// Creates a dialogue file in TTS Format
    /// </summary>
    internal class CreateTTSDialogueFile
    {

        private const string FILE_NAME = "DialogueFile.txt";
        private const string NPC = "NPC";
        private const string TEACHER = "TEACHER";
        private const string INSTIGATOR = "INSTIGATOR";
        private const string DRUID = "DRUID";
        private const string PARTICIPANT = "Participant";
        private static readonly string[] GENERICS_NAMES = [NPC, TEACHER, INSTIGATOR, DRUID, PARTICIPANT];
        private readonly List<IGenericDialogueHandler> genericDialogueHandlers =
        [
            new RedTowerDialogue(),
            new MusicReactionDialogue(),
            new CrimeDialogues(),
            new GateNementonSouthDialogue(),
            new GateNementonNorthDialogue(),
            new GateNementonFireFestivalLukan(),
            new LaudineDialogue(),
            new BertineSpiderDialogue(),
            new GateBrutusMine(),
            new GateRuincampDruidDialogue(),
            new GateMineDialogue(),
            new GateDeepMineEntryDialogue(),
            new CraftingReactionDialogue(),
            new GateArenaDialogue(),
            new GateConorDialogue(),
            new BanditDialogue(),
            new BanditMineHauerDialogue(),
            new CombatDialogues(),
            new GateRuincampSeanDialogue(),
            new GateRuinCampUpperTownIveraDialogue(),
            new GateMineStorageDialogue(),
            new AfterCombatPlayerDialogue(),
            new GreatTuskDialogue(),
            new PlayerOustideSoftCircleDialogue(),
            new ArenaViewerDialogue(),
            new ArenaFightsDialogue(),
            new GateNementonHolyHain(),
            new FrogDialogue(),
            new EsterManipulationDialogue(),
        ];

        private static DialogueTree[] GatherAllDialogueTrees()
        {
            return Resources.FindObjectsOfTypeAll<DialogueTree>();
        }

        private readonly StringBuilder sb = new();
        private readonly StringBuilder sbGneric = new();
        private readonly StringBuilder sbGenericTreeNames = new();
        private readonly StringBuilder sbAllDialogueNames = new();

        readonly Dictionary<string, int> actorMapping = [];

        public void Init()
        {
            EditorManager.InEditor = true;
            DialogueUtils.ReadActorMappings(actorMapping);
        }

        public void CreateDialogueFile()
        {
            Dictionary<string, DialogueTree> genericDialogues = [];
            DialogueTree[] dialogueTrees = GatherAllDialogueTrees();
            for (int i = 0; i < dialogueTrees.Length; i++)
            {
                DialogueTree dialogueTree = dialogueTrees[i];
                if (dialogueTree.IsTestOrDebugDialogue)
                {
                    //MelonLogger.Msg("Skipping creation for test/debug: " + dialogueTree.name);
                    continue;
                };
                sbAllDialogueNames.Append(dialogueTree.name).AppendLine();
                //MelonLogger.Msg("Creating dialogue file for: " + dialogueTree.name);
                dialogueTree.SelfDeserialize();
                dialogueTree.DeserializeIfNotDoneYet(true);

                for (int j = 0; j < dialogueTree.allNodes.Count; j++)
                {
                    Node node = dialogueTree.allNodes[j];

                    DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        if (GENERICS_NAMES.Any(n => n.Contains(statement.actorName, StringComparison.OrdinalIgnoreCase)))
                        {
                            //MelonLogger.Msg("Generic NPC in " + dialogueTree.name + " with actor id: " + statement._actorParameterID);
                            if (!genericDialogues.ContainsKey(dialogueTree.name))
                            {
                                genericDialogues.Add(dialogueTree.name, dialogueTree);
                                break;
                            }
                            break;
                        }
                        if (node.inConnections.Count == 0 && node.outConnections.Count == 0)
                        {
                            MelonLogger.Msg("Skipping node without connections");
                            continue;
                        }
                        string loca = statement.GetLocalizedString();
                        // Unused dialogue node
                        if (loca.StartsWith("[LOCA] Key not found")) continue;

                        sb
                            .Append(DialogueUtils.MapActorNameToNumber(actorMapping, statement.actorName))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(loca)
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatement(dialogueTree, statement))
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(DialogueUtils.EMOTION)
                            .Append(DialogueUtils.SEPERATOR)
                            .Append(DialogueUtils.STYLE)
                            .Append(DialogueUtils.SEPERATOR).AppendLine();
                        continue;
                    }

                    DS_MultipleChoiceNode multipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                    if (multipleChoice != null)
                    {
                        if (node.inConnections.Count == 0 && node.outConnections.Count == 0)
                        {
                            MelonLogger.Msg("Skipping node without connections");
                            continue;
                        }
                        for (int k = 0; k < multipleChoice.availableChoices.Count; k++)
                        {
                            DS_MultipleChoiceNode.Choice choice = multipleChoice.availableChoices[k];
                            sb
                               .Append(DialogueUtils.MapActorNameToNumber(actorMapping, multipleChoice.actorName))
                               .Append(DialogueUtils.SEPERATOR)
                               .Append(multipleChoice.GetLocalizedString(choice.statement))
                               .Append(DialogueUtils.SEPERATOR)
                               .Append(AudioManager.GetUniqueIDChoice(dialogueTree, choice))
                               .Append(DialogueUtils.SEPERATOR)
                               .Append(DialogueUtils.EMOTION)
                               .Append(DialogueUtils.SEPERATOR)
                               .Append(DialogueUtils.STYLE)
                               .Append(DialogueUtils.SEPERATOR)
                               .AppendLine();
                        }
                    }
                }
            }

            HashSet<string> playerGeneratedDialogues = [];
            HashSet<string> handledGenerics = [];

            for (int j = 0; j < dialogueTrees.Length; j++)
            {
                DialogueTree tree = dialogueTrees[j];
                HashSet<string> handledSubdialogues = [];
                if (genericDialogues.ContainsKey(tree.name)) continue;
                if (tree.name.Contains("DT_Dialogue_AfterCombat"))
                {
                    genericDialogues.TryAdd(tree.name, tree);
                    continue;
                }

                //MelonLogger.Msg("Executing for " + tree.name + " with " + tree.allNodes.Count);

                for (int k = 0; k < tree.allNodes.Count; k++)
                {
                    Node node = tree.allNodes[k];
                    SubDialogueTree subTree = node.TryCast<SubDialogueTree>();
                    if (subTree != null && subTree.subGraph != null && genericDialogues.ContainsKey(subTree.subGraph.name))
                    {
                        // prevent duplicate handling
                        if (handledSubdialogues.Contains(subTree.subGraph.name)) continue;
                        handledSubdialogues.Add(subTree.subGraph.name);
                        //MelonLogger.Msg("Found generic Dialog " + subTree.subGraph.name + " in " + tree.name);
                        subTree.SetParametersMap();
                        subTree.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance(true).TryCast<DialogueTree>();
                        subTree.TryWriteMappedActorParameters();
                        string genericName = "";
                        genericName = GetGenericName(subTree.DLGTree, subTree, genericName);
                        // first check try to recover at this point by searching the main graph
                        if (genericName.Length == 0)
                        {
                            DialogueTree holdingTree = dialogueTrees
                                 .FirstOrDefault(d => d.allNodes.ToArray().ToList()
                                 .Find(node => node.TryCast<SubDialogueTree>() != null && node.Cast<SubDialogueTree>().subGraph?.name == tree.name
                                 ) != null
                             );
                            if (holdingTree != null)
                            {
                                SubDialogueTree subTreeUnderHolding = holdingTree.allNodes.ToArray().ToList()
                                     .Find(node => node.TryCast<SubDialogueTree>() != null && node.Cast<SubDialogueTree>().subGraph?.name == tree.name)
                                     .Cast<SubDialogueTree>();
                                subTreeUnderHolding.SetParametersMap();
                                subTreeUnderHolding.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance(true).TryCast<DialogueTree>();
                                subTreeUnderHolding.TryWriteMappedActorParameters();
                                genericName = GetGenericName(holdingTree, subTreeUnderHolding, genericName);
                            }
                        }

                        // Dialogue which is used by different systems, currently unsupported
                        if (genericName.Length == 0 || GENERICS_NAMES.Any(n => n.Contains(genericName, StringComparison.OrdinalIgnoreCase)))
                        {
                            //MelonLogger.Warning("No NPC name found for " + subTree.subGraph.name);
                            continue;
                        }

                        for (int i = 0; i < subTree.subGraph.allNodes.Count; i++)
                        {
                            Node subGraphNode = subTree.subGraph.allNodes[i];
                            DS_StatementNode statement = subGraphNode.TryCast<DS_StatementNode>();
                            if (statement != null)
                            {
                                string actorName = statement.actorName;
                                // Skip already player generated dialogues
                                if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && (playerGeneratedDialogues.Contains(subTree.subGraph.name) || subTree.subGraph.name.Contains("DT_Teach_Generic") || subTree.subGraph.name.Contains("DT_Quest_Generic_Teach") || subTree.subGraph.name.Contains("DT_Dialogue_AfterCombat")))
                                {
                                    continue;
                                }
                                if (GENERICS_NAMES.Any(n => n.Contains(actorName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    actorName = genericName;
                                    if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && (playerGeneratedDialogues.Contains(subTree.subGraph.name) || subTree.subGraph.name.Contains("DT_Teach_Generic") || subTree.subGraph.name.Contains("DT_Quest_Generic_Teach") || subTree.subGraph.name.Contains("DT_Dialogue_AfterCombat")))
                                        continue;
                                }
                                if (subGraphNode.inConnections.Count == 0 && subGraphNode.outConnections.Count == 0)
                                {
                                    MelonLogger.Msg("Skipping node without connections");
                                    continue;
                                }

                                sb
                                    .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(statement.GetLocalizedString())
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(AudioManager.GetUniqueIDStatementGeneric(subTree.subGraph, statement, actorName))
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(DialogueUtils.EMOTION)
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(DialogueUtils.STYLE)
                                    .Append(DialogueUtils.SEPERATOR)
                                    .AppendLine();
                                continue;
                            }
                            SubDialogueTree subTreeSub = subGraphNode.TryCast<SubDialogueTree>();
                            if (subTreeSub != null)
                            {
                                ProcessSubDialogueTree(subTreeSub, actorMapping, sb, genericName, playerGeneratedDialogues, handledGenerics);
                            }
                        }
                        playerGeneratedDialogues.Add(subTree.subGraph.name);
                        handledGenerics.Add(subTree.subGraph.name);
                    }

                }

            }

            for (int i = 0; i < genericDialogues.Count; i++)
            {
                KeyValuePair<string, DialogueTree> keyValue = genericDialogues.ElementAt(i);
                if (handledGenerics.Contains(keyValue.Key))
                {
                    //MelonLogger.Msg("Skipping generic dialog for " + keyValue.Key + " because it was already generated");
                    continue;
                }
                bool isHandledByGenericHandler = false;
                for (int j = 0; j < genericDialogueHandlers.Count; j++)
                {
                    if (genericDialogueHandlers[j].CanHandleDialogue(keyValue.Value))
                    {
                        genericDialogueHandlers[j].HandleDialogue(keyValue.Value, sb, actorMapping);
                        isHandledByGenericHandler = true;
                        break;
                    }
                }
                if (isHandledByGenericHandler) continue;
                //MelonLogger.Msg("Generic Dialog for " + keyValue.Key + " with " + keyValue.Value.allNodes.Count);
                string genericName = "";
                CheckIfLocaPathIncludesNpc(keyValue.Value, actorMapping, ref genericName);
                if (genericName.Length > 0)
                {
                    for (int j = 0; j < keyValue.Value.allNodes.Count; j++)
                    {
                        Node node = keyValue.Value.allNodes[j];
                        DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                        if (statement != null)
                        {
                            sb
                                .Append(genericName)
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(statement.GetLocalizedString())
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(AudioManager.GetUniqueIDStatement(keyValue.Value, statement))
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(DialogueUtils.EMOTION)
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(DialogueUtils.STYLE)
                                .Append(DialogueUtils.SEPERATOR)
                                .AppendLine();
                            continue;
                        }
                    }
                }
                else
                {
                    sbGenericTreeNames.Append(keyValue.Key).AppendLine();
                    CreateGenericDialog(sbGneric, keyValue);
                }
            }

            BrawlDialogue.GenerateDialogue(sb, actorMapping);
            SleepDialogue.GenerateDialogue(sb, actorMapping);

            File.WriteAllText(Path.Combine(Utils.SavePath, "AllDialogueNames.txt"), sbAllDialogueNames.ToString());
        }

        public void GeneratePointDialogues()
        {
            ReadOutDialogRoutinePoint.GenerateDialogues(actorMapping, sb);

        }

        public void Save()
        {
            string path = Utils.SavePath;
            Directory.CreateDirectory(path);
            string savePath = Path.Combine(path, FILE_NAME);
            string genericSavePath = Path.Combine(path, "Generic_" + FILE_NAME);
            string genericSaveDialogueTreeNamesPath = Path.Combine(path, "Generic_ALL_" + FILE_NAME);
            try
            {
                File.WriteAllText(savePath, sb.ToString());
                File.WriteAllText(genericSavePath, sbGneric.ToString());
                File.WriteAllText(genericSaveDialogueTreeNamesPath, sbGenericTreeNames.ToString());
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error saving dialogue file: " + e);
            }
            EditorManager.InEditor = false;
        }

        private static void CreateGenericDialog(StringBuilder sbGneric, KeyValuePair<string, DialogueTree> keyValue)
        {
            for (int j = 0; j < keyValue.Value.allNodes.Count; j++)
            {
                Node node = keyValue.Value.allNodes[j];
                DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                if (statement != null)
                {
                    sbGneric
                        .Append(statement.actorName)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(statement.GetLocalizedString())
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(AudioManager.GetUniqueIDStatement(keyValue.Value, statement))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.EMOTION)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.STYLE)
                        .Append(DialogueUtils.SEPERATOR)
                        .AppendLine();
                    continue;
                }
            }
        }

        private static void CheckIfLocaPathIncludesNpc(DialogueTree tree, Dictionary<string, int> actorMapping, ref string genericName)
        {
            for (int i = 0; i < actorMapping.Count; i++)
            {
                string actorName = actorMapping.Keys.ElementAt(i);
                if (tree.LocaPath.Contains(actorName))
                {
                    genericName = actorName;
                    break;
                }
            }
        }

        private static void ProcessSubDialogueTree(SubDialogueTree subTree, Dictionary<string, int> actorMapping, StringBuilder sb, string genericName, HashSet<string> playerGeneratedDialogues, HashSet<string> handledGeneric)
        {
            if (subTree.subGraph != null)
            {
                for (int i = 0; i < subTree.subGraph.allNodes.Count; i++)
                {
                    Node subGraphNode = subTree.subGraph.allNodes[i];
                    DS_StatementNode statement = subGraphNode.TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        string actorName = statement.actorName;
                        // Skip already player generated dialogues
                        if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && (playerGeneratedDialogues.Contains(subTree.subGraph.name) || subTree.subGraph.name.Contains("DT_Teach_Generic") || subTree.subGraph.name.Contains("DT_Quest_Generic_Teach") || subTree.subGraph.name.Contains("DT_Dialogue_AfterCombat")))
                        {
                            continue;
                        }
                        if (GENERICS_NAMES.Any(n => n.Contains(actorName, StringComparison.OrdinalIgnoreCase)))
                        {
                            actorName = genericName;
                            if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && (playerGeneratedDialogues.Contains(subTree.subGraph.name) || subTree.subGraph.name.Contains("DT_Teach_Generic") || subTree.subGraph.name.Contains("DT_Quest_Generic_Teach") || subTree.subGraph.name.Contains("DT_Dialogue_AfterCombat")))
                                continue;
                        }
                        if (subGraphNode.inConnections.Count == 0 && subGraphNode.outConnections.Count == 0)
                        {
                            MelonLogger.Msg("Skipping node without connections");
                            continue;
                        }
                        sb
                        .Append(DialogueUtils.MapActorNameToNumber(actorMapping, actorName))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(statement.GetLocalizedString())
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(AudioManager.GetUniqueIDStatement(subTree.subGraph, statement))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.EMOTION)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.STYLE)
                        .Append(DialogueUtils.SEPERATOR)
                        .AppendLine();
                    }
                    else
                    {
                        SubDialogueTree subTreeSub = subGraphNode.TryCast<SubDialogueTree>();
                        if (subTreeSub != null)
                        {
                            ProcessSubDialogueTree(subTreeSub, actorMapping, sb, genericName, playerGeneratedDialogues, handledGeneric);
                        }
                    }
                }
                playerGeneratedDialogues.Add(subTree.subGraph.name);
                handledGeneric.Add(subTree.subGraph.name);
            }
        }

        private static string GetGenericName(DialogueTree dLGTree, SubDialogueTree subTree, string genericName)
        {
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, string> item in subTree.GetParametersMap())
            {
                //ActorParameter parameterById1 = subTree.currentInstance.GetParameterByID(item.key);
                ActorParameter parameterById2 = dLGTree.GetParameterByID(item.Value);
                if (!GENERICS_NAMES.Any(n => n.Contains(parameterById2.name, StringComparison.OrdinalIgnoreCase)))
                {
                    genericName = parameterById2.name;
                }
                if (genericName.Length > 0)
                {
                    break;
                }
            }

            return genericName;
        }

    }
}
