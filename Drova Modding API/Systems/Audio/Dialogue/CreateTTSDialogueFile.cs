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
    /// TODO: Add the necessary python script to convert the dialogue file to gametts format
    /// TODO: Add the mapping file for actor names to numbers
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
            Dictionary<string, int> actorMapping = [];
            ReadActorMappings(actorMapping);

            Dictionary<string, DialogueTree> genericDialogues = [];
            StringBuilder sb = new();
            DialogueTree[] dialogueTrees = GatherAllDialogueTrees();
            for (int i = 0; i < dialogueTrees.Length; i++)
            {
                var dialogueTree = dialogueTrees[i];
                if (dialogueTree.name.Contains("Test"))
                {
                    MelonLogger.Msg("Skipping creation for: " + dialogueTree.name);
                    continue;
                };
                MelonLogger.Msg("Creating dialogue file for: " + dialogueTree.name);
                dialogueTree.SelfDeserialize();
                dialogueTree.DeserializeIfNotDoneYet(true);

                for (int j = 0; j < dialogueTree.allNodes.Count; j++)
                {
                    var node = dialogueTree.allNodes[j];
                    var statement = node.TryCast<DS_StatementNode>();
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
                        var loca = statement.GetLocalizedString();
                        // Unused dialogue node
                        if (loca.StartsWith("[LOCA] Key not found")) continue;

                        sb.AppendLine()
                            .Append(MapActorNameToNumber(actorMapping, statement.actorName))
                            .Append(SEPERATOR)
                            .Append(loca)
                            .Append(SEPERATOR)
                            .Append(GetUniqueIDStatement(dialogueTree, node))
                            .Append(SEPERATOR)
                            .Append(EMOTION)
                            .Append(SEPERATOR)
                            .Append(STYLE)
                            .Append(SEPERATOR);
                        continue;
                    }

                    var multipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                    if (multipleChoice != null)
                    {
                        for (int k = 0; k < multipleChoice.availableChoices.Count; k++)
                        {
                            var choice = multipleChoice.availableChoices[k];
                            sb.AppendLine()
                               .Append(MapActorNameToNumber(actorMapping, multipleChoice.actorName))
                               .Append(SEPERATOR)
                               .Append(multipleChoice.GetLocalizedString(choice.statement))
                               .Append(SEPERATOR)
                               .Append(GetUniqueIDChoice(dialogueTree, choice))
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
                var keyValue = genericDialogues.ElementAt(i);
                MelonLogger.Msg("Generic Dialog for " + keyValue.Key + " with " + keyValue.Value.allNodes.Count);
            }

            List<string> playerGeneratedDialogues = [];

            for (int j = 0; j < dialogueTrees.Length; j++)
            {
                var tree = dialogueTrees[j];

                if (genericDialogues.ContainsKey(tree.name)) continue;

                MelonLogger.Msg("Executing for " + tree.name + " with " + tree.allNodes.Count);

                for (int k = 0; k < tree.allNodes.Count; k++)
                {
                    var node = tree.allNodes[k];
                    var subTree = node.TryCast<SubDialogueTree>();
                    if (subTree != null && subTree.subGraph != null && genericDialogues.ContainsKey(subTree.subGraph.name))
                    {
                        MelonLogger.Msg("Found generic Dialog " + subTree.subGraph.name + " in " + tree.name);
                        subTree.SetParametersMap();
                        subTree.currentInstance = subTree.TryCast<IGraphAssignable>().CheckInstance(true).TryCast<DialogueTree>();
                        subTree.TryWriteMappedActorParameters();
                        //var stringBuilder = new StringBuilder();
                        var npcName = "";
                        foreach (var item in subTree.GetParametersMap())
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

                            //stringBuilder.AppendLine().Append(item.key).Append(' ').Append(parameterById1.name).Append(" : ").Append(item.value).Append(' ').Append(parameterById2.name);
                        }

                        // Dialogue which is used by different systems, currently unsupported
                        if (npcName.Length == 0)
                        {
                            MelonLogger.Error("No NPC name found for " + subTree.subGraph.name);
                            continue;
                        }

                        for (int i = 0; i < subTree.subGraph.allNodes.Count; i++)
                        {
                            var subGraphNode = subTree.subGraph.allNodes[i];
                            var statement = subGraphNode.TryCast<DS_StatementNode>();
                            if (statement != null)
                            {
                                var actorName = statement.actorName;
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
                                    .Append(GetUniqueIDStatementGeneric(subTree.subGraph, subGraphNode, actorName))
                                    .Append(SEPERATOR)
                                    .Append(EMOTION)
                                    .Append(SEPERATOR)
                                    .Append(STYLE)
                                    .Append(SEPERATOR);
                                continue;
                            }
                        }
                        playerGeneratedDialogues.Add(subTree.subGraph.name);
                        //MelonLogger.Msg(stringBuilder.ToString());
                    }

                }

            }

            string path = Utils.SavePath;
            Directory.CreateDirectory(path);
            var savePath = Path.Combine(path, FILE_NAME);
            try
            {
                File.WriteAllText(savePath, sb.ToString());
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error saving dialogue file: " + e);
            }
        }

        private static void ReadActorMappings(Dictionary<string, int> actorMapping)
        {
            var pathToMapping = Path.Combine(Utils.SavePath, MAPPING_FILE_NAME);
            if (File.Exists(pathToMapping))
            {
                var lines = File.ReadAllLines(pathToMapping);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var split = line.Split(SEPERATOR);
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

        private static string GetUniqueIDStatement(DialogueTree tree, Node node)
        {
            return $"{tree.name}_{node.UID}";
        }

        private static string GetUniqueIDStatementGeneric(DialogueTree tree, Node node, string actorName)
        {
            return $"{tree.name}_{node.UID}_{actorName}";
        }

        private static string GetUniqueIDChoice(DialogueTree tree, DS_MultipleChoiceNode.Choice choice)
        {
            return $"{tree.name}_choice_{choice.UID}";
        }
    }
}
