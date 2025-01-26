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
        public const char SEPERATOR = '|';
        public const string EMOTION = "Neutral";
        public const string STYLE = "Default";
        public const string FILE_NAME = "DialogueFile.txt";
        public const string MAPPING_FILE_NAME = "mapping.txt";

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
                MelonLogger.Msg("Creating dialogue file for: " + dialogueTree.name);
                dialogueTree.SelfDeserialize();
                dialogueTree.DeserializeIfNotDoneYet(true);

                for (int j = 0; j < dialogueTree.allNodes.Count; j++)
                {
                    Node node = dialogueTree.allNodes[j];
                    DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                    if (statement != null)
                    {
                        if (statement.actorName == "NPC")
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

                        sb.AppendLine()
                            .Append(MapActorNameToNumber(actorMapping, statement.actorName))
                            .Append(SEPERATOR)
                            .Append(loca)
                            .Append(SEPERATOR)
                            .Append(AudioManager.GetUniqueIDStatement(dialogueTree, statement))
                            .Append(SEPERATOR)
                            .Append(EMOTION)
                            .Append(SEPERATOR)
                            .Append(STYLE)
                            .Append(SEPERATOR);
                        continue;
                    }

                    DS_MultipleChoiceNode multipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                    if (multipleChoice != null)
                    {
                        for (int k = 0; k < multipleChoice.availableChoices.Count; k++)
                        {
                            DS_MultipleChoiceNode.Choice choice = multipleChoice.availableChoices[k];
                            sb.AppendLine()
                               .Append(MapActorNameToNumber(actorMapping, multipleChoice.actorName))
                               .Append(SEPERATOR)
                               .Append(multipleChoice.GetLocalizedString(choice.statement))
                               .Append(SEPERATOR)
                               .Append(AudioManager.GetUniqueIDChoice(dialogueTree, choice))
                               .Append(SEPERATOR)
                               .Append(EMOTION)
                               .Append(SEPERATOR)
                               .Append(STYLE)
                               .Append(SEPERATOR);
                        }
                    }
                }
            }
            for (int i = 0; i < genericDialogues.Count; i++)
            {
                KeyValuePair<string, DialogueTree> keyValue = genericDialogues.ElementAt(i);
                MelonLogger.Msg("Generic Dialog for " + keyValue.Key + " with " + keyValue.Value.allNodes.Count);
            }

            List<string> playerGeneratedDialogues = [];

            for (int j = 0; j < dialogueTrees.Length; j++)
            {
                DialogueTree tree = dialogueTrees[j];

                if (genericDialogues.ContainsKey(tree.name)) continue;

                MelonLogger.Msg("Executing for " + tree.name + " with " + tree.allNodes.Count);

                for (int k = 0; k < tree.allNodes.Count; k++)
                {
                    Node node = tree.allNodes[k];
                    SubDialogueTree subTree = node.TryCast<SubDialogueTree>();
                    if (subTree != null && subTree.subGraph != null && genericDialogues.ContainsKey(subTree.subGraph.name))
                    {
                        MelonLogger.Msg("Found generic Dialog " + subTree.subGraph.name + " in " + tree.name);
                        subTree.SetParametersMap();
                        subTree.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance(true).TryCast<DialogueTree>();
                        subTree.TryWriteMappedActorParameters();
                        string npcName = "";
                        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, string> item in subTree.GetParametersMap())
                        {
                            ActorParameter parameterById1 = subTree.currentInstance.GetParameterByID(item.key);
                            ActorParameter parameterById2 = subTree.DLGTree.GetParameterByID(item.Value);
                            if (item.key == "NPC" && parameterById1.name != "NPC")
                            {
                                npcName = parameterById1.name;
                            }
                            else if (item.value == "NPC" && parameterById2.name != "NPC")
                            {
                                npcName = parameterById2.name;
                            }
                        }

                        // Dialogue which is used by different systems, currently unsupported
                        if (npcName.Length == 0)
                        {
                            MelonLogger.Error("No NPC name found for " + subTree.subGraph.name);
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
                                if (actorName == "NPC")
                                {
                                    actorName = npcName;
                                }
                                sb.AppendLine()
                                    .Append(MapActorNameToNumber(actorMapping, actorName))
                                    .Append(SEPERATOR)
                                    .Append(statement.GetLocalizedString())
                                    .Append(SEPERATOR)
                                    .Append(AudioManager.GetUniqueIDStatementGeneric(subTree.subGraph, statement, actorName))
                                    .Append(SEPERATOR)
                                    .Append(EMOTION)
                                    .Append(SEPERATOR)
                                    .Append(STYLE)
                                    .Append(SEPERATOR);
                                continue;
                            }
                        }
                        playerGeneratedDialogues.Add(subTree.subGraph.name);
                    }

                }

            }

            string path = Utils.SavePath;
            Directory.CreateDirectory(path);
            string savePath = Path.Combine(path, FILE_NAME);
            try
            {
                File.WriteAllText(savePath, sb.ToString());
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error saving dialogue file: " + e);
            }
            EditorManager.InEditor = false;
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
