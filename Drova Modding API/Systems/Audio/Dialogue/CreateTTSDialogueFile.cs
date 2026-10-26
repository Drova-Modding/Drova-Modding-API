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
        private const string ACTOR = "ACTOR";
        private static readonly string[] GENERICS_NAMES = [NPC, TEACHER, INSTIGATOR, DRUID, PARTICIPANT, ACTOR];
        private readonly List<IGenericDialogueHandler> _genericDialogueHandlers =
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

        /// <summary>
        /// Runs the full export in German, the language the voice lines are recorded in, and writes the
        /// dialogue files next to the mod.
        /// </summary>
        /// <returns>Line count of the main dialogue file</returns>
        internal static int Export()
        {
            Il2CppCustomFramework.Localization.LocalizationDB.ELanguage previous = Il2CppCustomFramework.Localization.LocalizationDB.Instance.CurrentLanguage;
            Access.LocalizationAccess.SetLanguage(Il2CppCustomFramework.Localization.LocalizationDB.ELanguage.de);
            try
            {
                CreateTTSDialogueFile file = new();
                file.Init();
                file.CreateDialogueFile();
                file.Save();
                DumpAllNodes();
                return file._sb.ToString().Split('\n').Length - 1;
            }
            catch (Exception e)
            {
                MelonLogger.Error("Dialogue export failed: " + e);
                throw;
            }
            finally
            {
                Access.LocalizationAccess.SetLanguage(previous);
            }
        }

        /// <summary>
        /// Writes every statement and choice of every tree without the export's filters, plus a census of
        /// node types, so lines the export skips can be found by diffing against the dialogue file.
        /// Columns: tree|testFlag|nodeId|actor|connected|locaPath|locaKey|text.
        /// </summary>
        private static void DumpAllNodes()
        {
            StringBuilder raw = new();
            Dictionary<string, int> nodeTypes = [];
            foreach (DialogueTree tree in GatherAllDialogueTrees())
            {
                tree.SelfDeserialize();
                tree.DeserializeIfNotDoneYet(true);
                for (int i = 0; i < tree.allNodes.Count; i++)
                {
                    Node node = tree.allNodes[i];
                    string typeName = node.GetIl2CppType().Name;
                    nodeTypes[typeName] = nodeTypes.GetValueOrDefault(typeName) + 1;
                    bool connected = node.inConnections.Count > 0 || node.outConnections.Count > 0;
                    DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        string path = statement.statement.useGlobalLoca ? statement.statement.GlobalLocaPath : tree.LocaPath;
                        raw.Append(tree.name).Append('|').Append(tree.IsTestOrDebugDialogue).Append('|').Append(node.ID).Append('|')
                            .Append(statement.actorName).Append('|').Append(connected).Append('|').Append(path).Append('|')
                            .Append(statement.statement.locaKey).Append('|').Append(statement.GetLocalizedString().Replace('\n', ' ')).AppendLine();
                        continue;
                    }
                    DS_MultipleChoiceNode choices = node.TryCast<DS_MultipleChoiceNode>();
                    if (choices == null) continue;
                    for (int k = 0; k < choices.availableChoices.Count; k++)
                    {
                        DS_MultipleChoiceNode.Choice choice = choices.availableChoices[k];
                        string path = choice.statement.useGlobalLoca ? choice.statement.GlobalLocaPath : tree.LocaPath;
                        raw.Append(tree.name).Append('|').Append(tree.IsTestOrDebugDialogue).Append('|').Append(node.ID).Append('|')
                            .Append(choices.actorName).Append('|').Append(connected).Append('|').Append(path).Append('|')
                            .Append(choice.statement.locaKey).Append('|').Append(choices.GetLocalizedString(choice.statement).Replace('\n', ' ')).AppendLine();
                    }
                }
            }
            File.WriteAllText(Path.Combine(Utils.SavePath, "RawDialogueNodes.txt"), raw.ToString());

            // A talent taught non-explicitly teaches its first unlearned prerequisite instead (LearnUtil.SelectTeachableTalent),
            // and the teacher speaks the feedback of the talent actually learned, so prerequisites count as taught too. A talent
            // missing from the talent graph can never be learned (TalentGraph.CanLearnTalent).
            Il2CppDrova.Talent.Graphs.TalentGraph? talentGraph = Access.ProviderAccess.GetGameDatabase()?.TalentGraph;
            if (talentGraph != null)
            {
                talentGraph.SelfDeserialize();
                talentGraph.DeserializeIfNotDoneYet(true);
            }
            StringBuilder teach = new();
            foreach (Il2CppDrova.GUI.LearnGUI.TeacherConfig config in Resources.FindObjectsOfTypeAll<Il2CppDrova.GUI.LearnGUI.TeacherConfig>())
            {
                for (int i = 0; i < config._stats.Count; i++)
                {
                    teach.Append(config.name).Append("|stat|").Append(config._stats[i].ToString()).Append("|||").AppendLine();
                }
                for (int i = 0; i < config._talents.Count; i++)
                {
                    Il2CppDrova.Talent.TalentContainer root = config._talents[i].GetTalent();
                    if (root == null) continue;
                    List<Il2CppDrova.Talent.TalentContainer> taught = [root];
                    if (!config._talents[i].IsExplicitTalent() && talentGraph != null)
                    {
                        Il2CppSystem.Collections.Generic.List<Il2CppDrova.Talent.TalentContainer> dependencies = talentGraph.GetAllDependenciesOfTalent(root);
                        for (int d = 0; d < dependencies.Count; d++) taught.Add(dependencies[d]);
                    }
                    foreach (Il2CppDrova.Talent.TalentContainer talent in taught)
                    {
                        if (talent._feedbackData == null || talentGraph?.GetNodeFromGuid(talent.GUID) == null) continue;
                        for (int k = 0; k < talent._feedbackData.Count; k++)
                        {
                            Il2CppDrova.GUI.LearnGUI.TeachedFeedbackData? feedback = talent._feedbackData[k].TryCast<Il2CppDrova.GUI.LearnGUI.TeachedFeedbackData>();
                            if (feedback?._text == null) continue;
                            teach.Append(config.name).Append("|talent|").Append(talent.name).Append('|').Append(feedback._text.Path).Append('|')
                                .Append(feedback._text.Key).Append('|').Append(feedback._text.GetLocalizedString(null).Replace('\n', ' ')).AppendLine();
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(Utils.SavePath, "TeacherFeedback.txt"), teach.ToString());

            StringBuilder entities = new();
            foreach (Il2CppDrova.EntityInfo info in Resources.FindObjectsOfTypeAll<Il2CppDrova.EntityInfo>())
            {
                entities.Append(info.name).Append('|').Append(info.GetLocaKey()).Append('|').Append(info._locaName?.GetLocalizedString(null)).AppendLine();
            }
            File.WriteAllText(Path.Combine(Utils.SavePath, "EntityInfoNames.txt"), entities.ToString());
            File.WriteAllLines(Path.Combine(Utils.SavePath, "DialogueNodeTypes.txt"), nodeTypes.OrderByDescending(p => p.Value).Select(p => p.Key + "|" + p.Value));
        }

        /// <summary>
        /// A node without connections is a leftover, unless it is the start node of a one statement tree
        /// (comments such as <c>DT_Comment_GettingInvolved_AxeRequired</c>).
        /// </summary>
        private static bool IsUnused(Node node)
        {
            return node.inConnections.Count == 0 && node.outConnections.Count == 0 && node != node.graph.primeNode;
        }

        private readonly StringBuilder _sb = new();
        private readonly StringBuilder _sbGeneric = new();
        private readonly StringBuilder _sbGenericTreeNames = new();
        private readonly StringBuilder _sbAllDialogueNames = new();
        private readonly List<string> _ignoreDuplicateTrees = ["DT_Quest_GuardTree_NPC_Niala"];


        private bool _isInitialized;

        readonly Dictionary<string, int> _actorMapping = [];


        public void Init()
        {
            EditorManager.InEditor = true;
            DialogueUtils.ReadActorMappings(_actorMapping);
        }

        public void CreateDialogueFile()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Dictionary<string, DialogueTree> genericDialogues = [];
            DialogueTree[] dialogueTrees = GatherAllDialogueTrees();
            HashSet<string> playerGeneratedDialogues = [];
            HashSet<string> handledGenerics = [];
            for (int i = 0; i < dialogueTrees.Length; i++)
            {
                DialogueTree dialogueTree = dialogueTrees[i];
                if (dialogueTree.IsTestOrDebugDialogue && dialogueTree.name != "DT_Main_NPC_Monko" && dialogueTree.name != "DT_Main_NPC_Minira")
                {
                    continue;
                }
                _sbAllDialogueNames.Append(dialogueTree.name).AppendLine();
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
                            genericDialogues.TryAdd(dialogueTree.name, dialogueTree);
                            break;
                        }
                        if (IsUnused(node))
                        {
                            continue;
                        }
                        string loca = statement.GetLocalizedString();
                        // Unused dialogue node
                        if (loca.StartsWith("[LOCA] Key not found")) continue;

                        _sb
                            .Append(DialogueUtils.MapActorNameToNumber(_actorMapping, NarratorActors.Resolve(statement.actorName, loca)))
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
                        if (IsUnused(node))
                        {
                            continue;
                        }
                        for (int k = 0; k < multipleChoice.availableChoices.Count; k++)
                        {
                            DS_MultipleChoiceNode.Choice choice = multipleChoice.availableChoices[k];
                            if (choice.statement.locaKey is "aboutQuests" or "aboutOtherQuests" && multipleChoice.actorName == "Player")
                            {
                                continue;
                            }
                            string choiceLoca = multipleChoice.GetLocalizedString(choice.statement);

                            _sb
                               .Append(DialogueUtils.MapActorNameToNumber(_actorMapping, NarratorActors.Resolve(multipleChoice.actorName, choiceLoca)))
                               .Append(DialogueUtils.SEPERATOR)
                               .Append(choiceLoca)
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
                    SubDialogueTree subTreeSub = node.TryCast<SubDialogueTree>();
                    if (subTreeSub != null && subTreeSub.subGraph != null)
                    {
                        ProcessSubDialogueTree(subTreeSub, _actorMapping, _sb, dialogueTree.actorParameters[0].name , playerGeneratedDialogues, handledGenerics);
                    }

                    
                }
            }

           

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


                for (int k = 0; k < tree.allNodes.Count; k++)
                {
                    Node node = tree.allNodes[k];
                    SubDialogueTree subTree = node.TryCast<SubDialogueTree>();
                    if (subTree != null && subTree.subGraph != null && genericDialogues.ContainsKey(subTree.subGraph.name))
                    {
                        if (!handledSubdialogues.Add(subTree.subGraph.name) && !_ignoreDuplicateTrees.Contains(subTree.subGraph.name))
                        {
                            continue;
                        }
                        subTree.SetParametersMap();
                        subTree.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance().TryCast<DialogueTree>();
                        subTree.TryWriteMappedActorParameters();
                        string genericName = "";
                        genericName = GetGenericName(subTree.DLGTree, subTree, genericName);
                        if (genericName.Length == 0)
                        {
                            DialogueTree holdingTree = dialogueTrees
                                 .FirstOrDefault(d => d.allNodes.ToArray().ToList()
                                 .Find(innerNode => innerNode.TryCast<SubDialogueTree>() != null && innerNode.Cast<SubDialogueTree>().subGraph?.name == tree.name
                                 ) != null
                             );
                            if (holdingTree != null)
                            {
                                SubDialogueTree subTreeUnderHolding = holdingTree.allNodes.ToArray().ToList()
                                     .Find(innerNode => innerNode.TryCast<SubDialogueTree>() != null && innerNode.Cast<SubDialogueTree>().subGraph?.name == tree.name)
                                     .Cast<SubDialogueTree>();
                                subTreeUnderHolding.SetParametersMap();
                                subTreeUnderHolding.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance().TryCast<DialogueTree>();
                                subTreeUnderHolding.TryWriteMappedActorParameters();
                                genericName = GetGenericName(holdingTree, subTreeUnderHolding, genericName);
                            }
                        }

                        // Dialogue which is used by different systems, currently unsupported
                        if (genericName.Length == 0 || GENERICS_NAMES.Any(n => n.Contains(genericName, StringComparison.OrdinalIgnoreCase)))
                        {
                            MelonLogger.Warning("No NPC name found for " + subTree.subGraph.name);
                            continue;
                        }

                        for (int i = 0; i < subTree.subGraph.allNodes.Count; i++)
                        {
                            Node subGraphNode = subTree.subGraph.allNodes[i];
                            DS_StatementNode statement = subGraphNode.TryCast<DS_StatementNode>();
                            if (statement != null)
                            {
                                string actorName = statement.actorName;
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
                                if (IsUnused(subGraphNode))
                                {
                                    continue;
                                }

                                string subLoca = statement.GetLocalizedString();

                                _sb
                                    .Append(DialogueUtils.MapActorNameToNumber(_actorMapping, NarratorActors.Resolve(actorName, subLoca)))
                                    .Append(DialogueUtils.SEPERATOR)
                                    .Append(subLoca)
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
                                ProcessSubDialogueTree(subTreeSub, _actorMapping, _sb, genericName, playerGeneratedDialogues, handledGenerics);
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
                    continue;
                }
                bool isHandledByGenericHandler = false;
                for (int j = 0; j < _genericDialogueHandlers.Count; j++)
                {
                    if (_genericDialogueHandlers[j].CanHandleDialogue(keyValue.Value))
                    {
                        _genericDialogueHandlers[j].HandleDialogue(keyValue.Value, _sb, _actorMapping);
                        isHandledByGenericHandler = true;
                        break;
                    }
                }
                if (isHandledByGenericHandler) continue;
                string genericName = "";
                CheckIfLocaPathIncludesNpc(keyValue.Value, _actorMapping, ref genericName);
                if (genericName.Length > 0)
                {
                    for (int j = 0; j < keyValue.Value.allNodes.Count; j++)
                    {
                        Node node = keyValue.Value.allNodes[j];
                        DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                        if (statement != null)
                        {
                            string genericLoca = statement.GetLocalizedString();

                            _sb
                                .Append(NarratorActors.Resolve(genericName, genericLoca))
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(genericLoca)
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(AudioManager.GetUniqueIDStatementGeneric(keyValue.Value, statement, genericName))
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(DialogueUtils.EMOTION)
                                .Append(DialogueUtils.SEPERATOR)
                                .Append(DialogueUtils.STYLE)
                                .Append(DialogueUtils.SEPERATOR)
                                .AppendLine();
                        }
                    }
                }
                else
                {
                    _sbGenericTreeNames.Append(keyValue.Key).AppendLine();
                    CreateGenericDialog(_sbGeneric, keyValue);
                }
            }

            BrawlDialogue.GenerateDialogue(_sb, _actorMapping);
            SleepDialogue.GenerateDialogue(_sb, _actorMapping);
            DrinkWaterDialogue.GenerateDialogue(_sb, _actorMapping, dialogueTrees);

            File.WriteAllText(Path.Combine(Utils.SavePath, "AllDialogueNames.txt"), _sbAllDialogueNames.ToString());
        }

        public void GenerateWorldDialogues()
        {
            ReadOutDialogRoutinePoint.GenerateDialogues(_actorMapping, _sb);
            ReadOutDialogueHitBehaviour.GenerateHitBvhrDialogues(_actorMapping, _sb);
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
                File.WriteAllText(savePath, _sb.ToString());
                File.WriteAllText(genericSavePath, _sbGeneric.ToString());
                File.WriteAllText(genericSaveDialogueTreeNamesPath, _sbGenericTreeNames.ToString());
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
                    string loca = statement.GetLocalizedString();

                    sbGneric
                        .Append(NarratorActors.Resolve(statement.actorName, loca))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(loca)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(AudioManager.GetUniqueIDStatement(keyValue.Value, statement))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.EMOTION)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(DialogueUtils.STYLE)
                        .Append(DialogueUtils.SEPERATOR)
                        .AppendLine();
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

        private void ProcessSubDialogueTree(SubDialogueTree subTree, Dictionary<string, int> actorMapping, StringBuilder sb, string genericName, HashSet<string> playerGeneratedDialogues, HashSet<string> handledGeneric)
        {
            if (subTree.subGraph != null)
            {
                for (int i = 0; i < subTree.subGraph.allNodes.Count; i++)
                {
                    Node subGraphNode = subTree.subGraph.allNodes[i];
                    DS_StatementNode statement = subGraphNode.TryCast<DS_StatementNode>();
                    bool isGeneric = false;
                    if (statement != null)
                    {
                        string actorName = statement.actorName;
                        if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && (playerGeneratedDialogues.Contains(subTree.subGraph.name) || subTree.subGraph.name.Contains("DT_Teach_Generic") || subTree.subGraph.name.Contains("DT_Quest_Generic_Teach") || subTree.subGraph.name.Contains("DT_Dialogue_AfterCombat")))
                        {
                            continue;
                        }
                        if (GENERICS_NAMES.Any(n => n.Contains(actorName, StringComparison.OrdinalIgnoreCase)))
                        {
                            actorName = genericName;
                            isGeneric = true;
                            if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && (playerGeneratedDialogues.Contains(subTree.subGraph.name) || subTree.subGraph.name.Contains("DT_Teach_Generic") || subTree.subGraph.name.Contains("DT_Quest_Generic_Teach") || subTree.subGraph.name.Contains("DT_Dialogue_AfterCombat")))
                                continue;
                        }
                        if (IsUnused(subGraphNode))
                        {
                            continue;
                        }
                        string loca = statement.GetLocalizedString();

                        sb
                        .Append(DialogueUtils.MapActorNameToNumber(actorMapping, NarratorActors.Resolve(actorName, loca)))
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(loca)
                        .Append(DialogueUtils.SEPERATOR)
                        .Append(isGeneric ? AudioManager.GetUniqueIDStatementGeneric(subTree.subGraph, statement, actorName) :  AudioManager.GetUniqueIDStatement(subTree.subGraph, statement))
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
