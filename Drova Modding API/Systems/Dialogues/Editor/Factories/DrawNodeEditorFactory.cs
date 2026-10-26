using Drova_Modding_API.Systems.Dialogues.Editor.Nodes;
using Drova_Modding_API.Systems.Routines.Editor;
using Il2CppDrova.Cutscenes;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.DialogueSystem.DialogueTrees;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.QuestSystem;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using Il2CppNodeCanvas.Framework;
using MelonLoader;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Factories
{
    /// <summary>
    /// Factory class that helps creating DrawNodeEditors
    /// </summary>
    public class DrawNodeEditorFactory
    {
        private readonly Dictionary<string, Type> nameToNodeMap = new()
        {
            {
                "DS_StatementNode", typeof(DS_StatementNodeEditor)
            },
            {
                "DS_MultipleChoiceNode", typeof(DS_MultipleChoiceNodeEditor)
            },
            {
                "DS_GiveExp", typeof(DS_GiveExpNodeEditor)
            },
            {
                "DS_ChangeStanceNode", typeof(DS_ChangeStanceNodeEditor)
            },
            {
                "DS_DebugNode", typeof(DS_DebugNodeEditor)
            },
            {
                "DS_HideDialogWindow", typeof(DS_HideDialogWindowNodeEditor)
            },
            {
                "DS_SetFactionNode", typeof(DS_SetFactionNodeEditor)
            },
            {
                "DS_SetFirstChapter", typeof(DS_SetFirstChapterNodeEditor)
            },
            {
                "DS_InteractAABaseNode", typeof(DS_InteractAABaseNodeEditor)
            },
            {
                "DS_GiveItemNode", typeof(DS_GiveItemNodeEditor)
            },
            {
                "DS_RevisitMultipleChoiceNode", typeof(DS_RevisitMultipleChoiceNodeEditor)
            },
            {
                "DS_DefineActiveActors", typeof(DS_DefineActiveActorsNodeEditor)
            },
            {
                "MultipleConditionNode", typeof(MultipleConditionNodeEditor)
            },
            {
                "DS_SetGBoolNode", typeof(DS_SetGBoolNodeEditor)
            },
            {
                "ConditionNode", typeof(ConditionNodeEditor)
            },
            {
                "DS_HubNode", typeof(DS_HubNodeEditor)
            },
            {
                "DS_HubJumpNode", typeof(DS_HubJumpNodeEditor)
            },
            {
                "FinishNode", typeof(FinishNodeEditor)
            },
            {
                "SubDialogueTree", typeof(SubDialogueTreeEditor)
            },
            {
                "DS_OverrideLookAtSpeaker", typeof(DS_OverrideLookAtSpeakerNodeEditor)
            },
            {
                "DS_OverrideFixCamPos", typeof(DS_OverrideFixCamPosNodeEditor)
            },
            {
                "CS_CutsceneActionNode", typeof(CS_CutsceneActionNodeEditor)
            },
            {
                "DS_SetDialogueRequirementsNode", typeof(DS_SetDialogueRequirementsNodeEditor)
            },
            {
                "DS_OpenTradeWindowNode", typeof(DS_OpenTradeWindowNodeEditor)
            },
            {
                "DS_SetTextSpeedNode", typeof(DS_SetTextSpeedNodeEditor)
            },
            {
                "DS_CanTeachAnything", typeof(DS_CanTeachAnythingNodeEditor)
            },
            {
                "DS_HealActor", typeof(DS_CanTeachAnythingNodeEditor)
            },
            {
                "DS_EquipNode", typeof(DS_EquipNodeEditor)
            },
            {
                "DS_UseItem", typeof(DS_UseItemNodeEditor)
            },
            {
                "DS_SetSpellToActiveAbiSlot", typeof(DS_SetSpellToActiveAbiSlotNodeEditor)
            },
            {
                "DS_UnEquipNode", typeof(DS_UnEquipNodeEditor)
            },
            {
                "DS_KatsaSilverMine", typeof(DS_KatsaSilverMineNodeEditor)
            },
            {
                "DS_LearnStatNode", typeof(DS_LearnStatNodeEditor)
            },
            {
                "DS_LearnTalentNode", typeof(DS_LearnTalentNodeEditor)
            },
            {
                "DS_LearnTalentNode_Single", typeof(DS_LearnTalentNode_SingleEditor)
            },
#if DEBUG
            {
                "DS_TeachTalentNode", typeof(DS_TeachTalentNodeEditor)
            },
#endif
            {
                "DS_ReleaseActiveActors", typeof(DS_ReleaseActiveActorsNodeEditor)
            },
            {
                "DS_RestartNode", typeof(DS_RestartNodeEditor)
            },
            {
                "DS_LearnAttributeNode_Single", typeof(DS_LearnAttributeNode_SingleNodeEditor)
            },
            {
                "DS_TeachStatsNode", typeof(DS_TeachStatsNodeEditor)
            },
            {
                "DS_PlaySfx", typeof(DS_PlaySfxNodeEditor)
            },
            {
                "DS_DelayedTeach_Statement", typeof(DS_DelayedTeach_StatementNodeEditor)
            },
            {
                "ProbabilitySelector", typeof(ProbabilitySelectorNodeEditor)
            },
            {
                "DS_SetGIntNode", typeof(DS_SetGIntNodeEditor)
            },
            {
                "DS_SetQuestStateNode", typeof(DS_SetQuestStateNodeEditor)
            },
            {
                "ActionNode", typeof(BTActionNodeEditor)
            },
        };

        private readonly Dictionary<string, Func<Graph, Node>> _nodeCreators = new()
        {
            {
                "DS_StatementNode", graph => CreateNode<DS_StatementNode>(graph)
            },
            {
                "DS_MultipleChoiceNode", graph => CreateNode<DS_MultipleChoiceNode>(graph)
            },
            {
                "DS_GiveExp", graph => CreateNode<DS_GiveExp>(graph)
            },
            {
                "DS_ChangeStanceNode", graph => CreateNode<DS_ChangeStanceNode>(graph)
            },
            {
                "DS_DebugNode", graph => CreateNode<DS_DebugNode>(graph)
            },
            {
                "DS_HideDialogWindow", graph => CreateNode<DS_HideDialogWindow>(graph)
            },
            {
                "DS_SetFactionNode", graph => CreateNode<DS_SetFactionNode>(graph)
            },
            {
                "DS_SetFirstChapter", graph => CreateNode<DS_SetFirstChapter>(graph)
            },
            {
                "DS_InteractAABaseNode", graph => CreateNode<DS_InteractAABaseNode>(graph)
            },
            {
                "DS_GiveItemNode", graph => CreateNode<DS_GiveItemNode>(graph)
            },
            {
                "DS_RevisitMultipleChoiceNode", graph => CreateNode<DS_RevisitMultipleChoiceNode>(graph)
            },
            {
                "DS_DefineActiveActors", graph => CreateNode<DS_DefineActiveActors>(graph)
            },
            {
                "MultipleConditionNode", graph => CreateNode<MultipleConditionNode>(graph)
            },
            {
                "DS_SetGBoolNode", graph => CreateGBoolNode(graph)
            },
            {
                "ConditionNode", graph => CreateNode<ConditionNode>(graph)
            },
            {
                "DS_HubNode", graph => CreateNode<DS_HubNode>(graph)
            },
            {
                "DS_HubJumpNode", graph => CreateNode<DS_HubJumpNode>(graph)
            },
            {
                "FinishNode", graph => CreateNode<FinishNode>(graph)
            },
            {
                "SubDialogueTree", graph => CreateNode<SubDialogueTree>(graph)
            },
            {
                "DS_OverrideLookAtSpeaker", graph => CreateNode<DS_OverrideLookAtSpeaker>(graph)
            },
            {
                "DS_OverrideFixCamPos", graph => CreateNode<DS_OverrideFixCamPos>(graph)
            },
            {
                "CS_CutsceneActionNode", graph => CreateNode<CS_CutsceneActionNode>(graph)
            },
            {
                "DS_SetDialogueRequirementsNode", graph => CreateNode<DS_SetDialogueRequirementsNode>(graph)
            },
            {
                "DS_OpenTradeWindowNode", graph => CreateNode<DS_OpenTradeWindowNode>(graph)
            },
            {
                "DS_SetTextSpeedNode", graph => CreateNode<DS_SetTextSpeedNode>(graph)
            },
            {
                "DS_CanTeachAnything", graph => CreateNode<DS_CanTeachAnything>(graph)
            },
            {
                "DS_HealActor", graph => CreateNode<DS_HealActor>(graph)
            },
            {
                "DS_EquipNode", graph => CreateNode<DS_EquipNode>(graph)
            },
            {
                "DS_UseItem", graph => CreateNode<DS_UseItem>(graph)
            },
            {
                "DS_SetSpellToActiveAbiSlot", graph => CreateNode<DS_SetSpellToActiveAbiSlot>(graph)
            },
            {
                "DS_UnEquipNode", graph => CreateNode<DS_UnEquipNode>(graph)
            },
            {
                "DS_KatsaSilverMine", graph => CreateNode<DS_KatsaSilverMine>(graph)
            },
            {
                "DS_LearnStatNode", graph => CreateNode<DS_LearnStatNode>(graph)
            },
            {
                "DS_LearnTalentNode", graph => CreateNode<DS_LearnTalentNode>(graph)
            },
            {
                "DS_ReleaseActiveActors", graph => CreateNode<DS_ReleaseActiveActors>(graph)
            },
            {
                "DS_RestartNode", graph => CreateNode<DS_RestartNode>(graph)
            },
            {
                "DS_LearnAttributeNode_Single", graph => CreateNode<DS_LearnAttributeNode_Single>(graph)
            },
            {
                "DS_TeachStatsNode", graph => CreateNode<DS_TeachStatsNode>(graph)
            },
            {
                "DS_PlaySfx", graph => CreateNode<DS_PlaySfx>(graph)
            },
            {
                "DS_DelayedTeach_Statement", graph => CreateNode<DS_DelayedTeach_Statement>(graph)
            },
            {
                "ProbabilitySelector", graph => CreateNode<ProbabilitySelector>(graph)
            },
            {
                "DS_SetGIntNode", graph => CreateNode<DS_SetGIntNode>(graph)
            },
            {
                "DS_SetQuestStateNode", graph => CreateQuestStateNode(graph)
            },
            {
                "ActionNode", graph => CreateNode<ActionNode>(graph)
            },
#if DEBUG
            {
                "DS_TeachTalentNode", graph => CreateNode<DS_TeachTalentNode>(graph)
            },
#endif
        };
        /// <summary>
        /// Creates the factory for dialogue trees.
        /// </summary>
        public DrawNodeEditorFactory() { }

        /// <summary>
        /// Creates a factory for another graph type, replacing the dialogue node set.
        /// </summary>
        /// <param name="editors">Il2cpp node type name to <see cref="DrawNodeEditor"/> type</param>
        /// <param name="creators">Node name shown in the create menu to the function adding that node to a graph</param>
        protected DrawNodeEditorFactory(Dictionary<string, Type> editors, Dictionary<string, Func<Graph, Node>> creators)
        {
            nameToNodeMap = editors;
            _nodeCreators = creators;
        }

#region Creation of Nodes
        /// <summary>
        /// Adds a node of type <typeparamref name="T"/> to <paramref name="graph"/> and gives it a UID.
        /// </summary>
        protected static Node CreateNode<T>(Graph graph) where T : Node
        {
            T node = graph.AddNode<T>();
            node.TryGenerateUID();
            return node;
        }

        private static DS_SetGBoolNode CreateGBoolNode(Graph graph)
        {
            var node = CreateNode<DS_SetGBoolNode>(graph).Cast<DS_SetGBoolNode>();
            node.Variable = new BBParameter<GBool>();
            node.Value = new BBParameter<bool>();
            return node;
        }

        private static DS_SetQuestStateNode CreateQuestStateNode(Graph graph)
        {
            var node = CreateNode<DS_SetQuestStateNode>(graph).Cast<DS_SetQuestStateNode>();
            node.QuestState = new BBParameter<GQuestState>();
            node.Value = new BBParameter<QuestState>();
            return node;
        }
#endregion

        /// <summary>
        /// Get the names of all nodes this factory can create
        /// </summary>
        public string[] GetNodeNames()
        {
            return [.. _nodeCreators.Keys];
        }

        /// <summary>
        /// Registers a <see cref="DrawNodeEditor"/> type for the il2cpp node type named <paramref name="name"/>.
        /// </summary>
        public void AddNodeEditorType(string name, Type type)
        {
            if (type.IsSubclassOf(typeof(DrawNodeEditor)))
            {
                nameToNodeMap.TryAdd(name, type);
            }
            else
            {
                MelonLogger.Error("Type {0} is not a subclass of DrawNodeEditor", type.ToString());
            }
        }

        /// <summary>
        /// Get a DrawNodeEditor by name
        /// </summary>
        public DrawNodeEditor? GetDrawNodeEditorByName(string name)
        {
            try
            {
                if (nameToNodeMap.TryGetValue(name, out Type v))
                {
                    return (DrawNodeEditor)Activator.CreateInstance(v);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error creating DrawNodeEditor from type: " + e);
                return null;
            }
        }

        /// <summary>
        /// Creates a new node from a known node name.
        /// </summary>
        public Node? CreateNodeByName(Graph graph, string name)
        {
            if (!_nodeCreators.TryGetValue(name, out Func<Graph, Node> creator))
            {
                return null;
            }

            try
            {
                return creator.Invoke(graph);
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error creating dialogue node {0}: {1}", name, e);
                return null;
            }
        }

        /// <summary>
        /// Get a DrawNodeEditor by type. Nodes without a dedicated editor get a <see cref="GenericNodeEditor"/>.
        /// </summary>
        public virtual DrawNodeEditor? GetDrawNodeEditorFromType(Il2CppSystem.Type type)
        {
            try
            {
                return nameToNodeMap.TryGetValue(type.Name, out Type v)
                    ? (DrawNodeEditor)Activator.CreateInstance(v)
                    : new GenericNodeEditor();
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error creating DrawNodeEditor from type: " + e);
                return new GenericNodeEditor();
            }
        }
    }
}