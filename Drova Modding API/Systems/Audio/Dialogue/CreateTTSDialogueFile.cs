using Drova_Modding_API.Systems.Editor;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using System.Linq;
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
        public const char SEPERATOR = '|';
        public const string EMOTION = "Neutral";
        public const string STYLE = "Default";
        public const string FILE_NAME = "DialogueFile.txt";
        public const string MAPPING_FILE_NAME = "mapping.txt";
        private const string NPC = "NPC";
        private const string TEACHER = "TEACHER";
        private const string INSTIGATOR = "INSTIGATOR";
        public static readonly string[] GENERICS_NAMES = [NPC, TEACHER, INSTIGATOR];

        private static DialogueTree[] GatherAllDialogueTrees()
        {
            return Resources.FindObjectsOfTypeAll<DialogueTree>();
        }

        public void CreateDialogueFile()
        {
            EditorManager.InEditor = true;
            Dictionary<string, int> actorMapping = [];
            ReadActorMappings(actorMapping);

            Dictionary<string, DialogueTree> genericDialogues = [];
            StringBuilder sb = new();
            DialogueTree[] dialogueTrees = GatherAllDialogueTrees();
            for (int i = 0; i < dialogueTrees.Length; i++)
            {
                DialogueTree dialogueTree = dialogueTrees[i];
                if (dialogueTree.IsTestOrDebugDialogue)
                {
                    MelonLogger.Msg("Skipping creation for test/debug: " + dialogueTree.name);
                    continue;
                };
                //MelonLogger.Msg("Creating dialogue file for: " + dialogueTree.name);
                dialogueTree.SelfDeserialize();
                dialogueTree.DeserializeIfNotDoneYet(true);

                for (int j = 0; j < dialogueTree.allNodes.Count; j++)
                {
                    Node node = dialogueTree.allNodes[j];
                    DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        if (GENERICS_NAMES.Any(n => n == statement.actorName))
                        {
                            MelonLogger.Msg("Generic NPC in " + dialogueTree.name + " with actor id: " + statement._actorParameterID);
                            if (!genericDialogues.ContainsKey(dialogueTree.name))
                            {
                                genericDialogues.Add(dialogueTree.name, dialogueTree);
                                break;
                            }
                            break;
                        }
                        string loca = statement.GetLocalizedString();
                        // Unused dialogue node
                        if (loca.StartsWith("[LOCA] Key not found")) continue;

                        sb
                            .Append(MapActorNameToNumber(actorMapping, statement.actorName))
                            .Append(SEPERATOR)
                            .Append(loca)
                            .Append(SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatement(dialogueTree, statement))
                            .Append(SEPERATOR)
                            .Append(EMOTION)
                            .Append(SEPERATOR)
                            .Append(STYLE)
                            .Append(SEPERATOR).AppendLine();
                        continue;
                    }

                    DS_MultipleChoiceNode multipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                    if (multipleChoice != null)
                    {
                        for (int k = 0; k < multipleChoice.availableChoices.Count; k++)
                        {
                            DS_MultipleChoiceNode.Choice choice = multipleChoice.availableChoices[k];
                            sb
                               .Append(MapActorNameToNumber(actorMapping, multipleChoice.actorName))
                               .Append(SEPERATOR)
                               .Append(multipleChoice.GetLocalizedString(choice.statement))
                               .Append(SEPERATOR)
                               .Append(AudioManager.GetUniqueIDChoice(dialogueTree, choice))
                               .Append(SEPERATOR)
                               .Append(EMOTION)
                               .Append(SEPERATOR)
                               .Append(STYLE)
                               .Append(SEPERATOR)
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

                MelonLogger.Msg("Executing for " + tree.name + " with " + tree.allNodes.Count);

                for (int k = 0; k < tree.allNodes.Count; k++)
                {
                    Node node = tree.allNodes[k];
                    SubDialogueTree subTree = node.TryCast<SubDialogueTree>();
                    if (subTree != null && subTree.subGraph != null && genericDialogues.ContainsKey(subTree.subGraph.name))
                    {
                        // prevent duplicate handling
                        if (handledSubdialogues.Contains(subTree.subGraph.name)) continue;
                        handledSubdialogues.Add(subTree.subGraph.name);
                        MelonLogger.Msg("Found generic Dialog " + subTree.subGraph.name + " in " + tree.name);
                        subTree.SetParametersMap();
                        subTree.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance(true).TryCast<DialogueTree>();
                        subTree.TryWriteMappedActorParameters();
                        string genericName = "";
                        genericName = GetGenericName(subTree.DLGTree, subTree, genericName);
                        // first check try to recover at this point by searching the main graph
                        if (genericName.Length == 0)
                        {
                            var holdingTree = dialogueTrees
                                 .FirstOrDefault(d => d.allNodes.ToArray().ToList()
                                 .Find(node => node.TryCast<SubDialogueTree>() != null && node.Cast<SubDialogueTree>().subGraph?.name == tree.name
                                 ) != null
                             );
                            if (holdingTree != null)
                            {
                                var subTreeUnderHolding = holdingTree.allNodes.ToArray().ToList()
                                     .Find(node => node.TryCast<SubDialogueTree>() != null && node.Cast<SubDialogueTree>().subGraph?.name == tree.name)
                                     .Cast<SubDialogueTree>();
                                subTreeUnderHolding.SetParametersMap();
                                subTreeUnderHolding.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance(true).TryCast<DialogueTree>();
                                subTreeUnderHolding.TryWriteMappedActorParameters();
                                genericName = GetGenericName(holdingTree, subTreeUnderHolding, genericName);
                            }
                        }

                        // Dialogue which is used by different systems, currently unsupported
                        if (genericName.Length == 0 || GENERICS_NAMES.Any(n => n == genericName))
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
                                // Skip already player generated dialogues
                                if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && playerGeneratedDialogues.Contains(subTree.subGraph.name))
                                {
                                    continue;
                                }
                                if (GENERICS_NAMES.Any(n => n == actorName))
                                {
                                    actorName = genericName;
                                }
                                sb
                                    .Append(MapActorNameToNumber(actorMapping, actorName))
                                    .Append(SEPERATOR)
                                    .Append(statement.GetLocalizedString())
                                    .Append(SEPERATOR)
                                    .Append(AudioManager.GetUniqueIDStatementGeneric(subTree.subGraph, statement, actorName))
                                    .Append(SEPERATOR)
                                    .Append(EMOTION)
                                    .Append(SEPERATOR)
                                    .Append(STYLE)
                                    .Append(SEPERATOR)
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
            var sbGneric = new StringBuilder();

            for (int i = 0; i < genericDialogues.Count; i++)
            {
                KeyValuePair<string, DialogueTree> keyValue = genericDialogues.ElementAt(i);
                if (handledGenerics.Contains(keyValue.Key))
                {
                    MelonLogger.Msg("Skipping generic dialog for " + keyValue.Key + " because it was already generated");
                    continue;
                }
                MelonLogger.Msg("Generic Dialog for " + keyValue.Key + " with " + keyValue.Value.allNodes.Count);
                for (int j = 0; j < keyValue.Value.allNodes.Count; j++)
                {
                    Node node = keyValue.Value.allNodes[j];
                    DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        sbGneric
                            .Append(statement.actorName)
                            .Append(SEPERATOR)
                            .Append(statement.GetLocalizedString())
                            .Append(SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatement(keyValue.Value, statement))
                            .Append(SEPERATOR)
                            .Append(EMOTION)
                            .Append(SEPERATOR)
                            .Append(STYLE)
                            .Append(SEPERATOR)
                            .AppendLine();
                        continue;
                    }
                }
            }

            string path = Utils.SavePath;
            Directory.CreateDirectory(path);
            string savePath = Path.Combine(path, FILE_NAME);
            string genericSavePath = Path.Combine(path, "Generic_" + FILE_NAME);
            try
            {
                File.WriteAllText(savePath, sb.ToString());
                File.WriteAllText(genericSavePath, sbGneric.ToString());
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error saving dialogue file: " + e);
            }



            EditorManager.InEditor = false;
        }

        private void ProcessSubDialogueTree(SubDialogueTree subTree, Dictionary<string, int> actorMapping, StringBuilder sb, string genericName, HashSet<string> playerGeneratedDialogues, HashSet<string> handledGeneric)
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
                        if (actorName.Contains("Player", StringComparison.OrdinalIgnoreCase) && playerGeneratedDialogues.Contains(subTree.subGraph.name))
                        {
                            continue;
                        }
                        if (GENERICS_NAMES.Any(n => n == actorName))
                        {
                            actorName = genericName;
                        }
                        sb
                        .Append(MapActorNameToNumber(actorMapping, actorName))
                        .Append(SEPERATOR)
                        .Append(statement.GetLocalizedString())
                        .Append(SEPERATOR)
                        .Append(AudioManager.GetUniqueIDStatement(subTree.subGraph, statement))
                        .Append(SEPERATOR)
                        .Append(EMOTION)
                        .Append(SEPERATOR)
                        .Append(STYLE)
                        .Append(SEPERATOR)
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
                if (!GENERICS_NAMES.Any(n => n == parameterById2.name))
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

        private static void ReadActorMappings(Dictionary<string, int> actorMapping)
        {
            string pathToMapping = Path.Combine(Utils.SavePath, MAPPING_FILE_NAME);
            if (File.Exists(pathToMapping))
            {
                string[] lines = File.ReadAllLines(pathToMapping);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] split = line.Split(SEPERATOR);
                    if (split.Length != 2)
                    {
                        MelonLogger.Error("Invalid mapping line: " + line);
                        continue;
                    }
                    actorMapping.Add(split[0], int.Parse(split[1]));
                }
            }
            else
            {
                MelonLogger.Error("No mapping file found at: " + pathToMapping);
            }
        }

        private string MapActorNameToNumber(Dictionary<string, int> actorMapping, string actorName)
        {
            if (actorMapping.ContainsKey(actorName))
            {
                return actorMapping[actorName].ToString();
            }
            else
            {
                MelonLogger.Error("No mapping found for actor: " + actorName);
                return "0";
            }
        }


    }
}
