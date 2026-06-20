using Drova_Modding_API.Systems.Dialogues.Editor.Nodes;
using Il2CppDrova;
using Il2CppDrova.ActorActions;
using Il2CppDrova.Cutscenes;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GlobalVarSystem;
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
                "DS_UnEquipNodeEditor", typeof(DS_UnEquipNodeEditor)
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
        };

        private readonly Dictionary<string, Func<DialogueTree, DTNode>> _nodeCreators = new()
        {
            {
                "DS_StatementNode", tree => CreateNode<DS_StatementNode>(tree)
            },
            {
                "DS_MultipleChoiceNode", tree => CreateNode<DS_MultipleChoiceNode>(tree)
            },
            {
                "DS_GiveExp", tree => CreateNode<DS_GiveExp>(tree)
            },
            {
                "DS_ChangeStanceNode", tree => CreateNode<DS_ChangeStanceNode>(tree)
            },
            {
                "DS_DebugNode", tree => CreateNode<DS_DebugNode>(tree)
            },
            {
                "DS_HideDialogWindow", tree => CreateNode<DS_HideDialogWindow>(tree)
            },
            {
                "DS_SetFactionNode", tree => CreateNode<DS_SetFactionNode>(tree)
            },
            {
                "DS_SetFirstChapter", tree => CreateNode<DS_SetFirstChapter>(tree)
            },
            {
                "DS_InteractAABaseNode", tree => CreateNode<DS_InteractAABaseNode>(tree)
            },
            {
                "DS_GiveItemNode", tree => CreateNode<DS_GiveItemNode>(tree)
            },
            {
                "DS_RevisitMultipleChoiceNode", tree => CreateNode<DS_RevisitMultipleChoiceNode>(tree)
            },
            {
                "DS_DefineActiveActors", tree => CreateNode<DS_DefineActiveActors>(tree)
            },
            {
                "MultipleConditionNode", tree => CreateNode<MultipleConditionNode>(tree)
            },
            {
                "DS_SetGBoolNode", tree => CreateGBoolNode(tree)
            },
            {
                "ConditionNode", tree => CreateNode<ConditionNode>(tree)
            },
            {
                "DS_HubNode", tree => CreateNode<DS_HubNode>(tree)
            },
            {
                "DS_HubJumpNode", tree => CreateNode<DS_HubJumpNode>(tree)
            },
            {
                "FinishNode", tree => CreateNode<FinishNode>(tree)
            },
            {
                "SubDialogueTree", tree => CreateNode<SubDialogueTree>(tree)
            },
            {
                "DS_OverrideLookAtSpeaker", tree => CreateNode<DS_OverrideLookAtSpeaker>(tree)
            },
            {
                "DS_OverrideFixCamPos", tree => CreateNode<DS_OverrideFixCamPos>(tree)
            },
            {
                "CS_CutsceneActionNode", tree => CreateNode<CS_CutsceneActionNode>(tree)
            },
            {
                "DS_SetDialogueRequirementsNode", tree => CreateNode<DS_SetDialogueRequirementsNode>(tree)
            },
            {
                "DS_OpenTradeWindowNode", tree => CreateNode<DS_OpenTradeWindowNode>(tree)
            },
            {
                "DS_SetTextSpeedNode", tree => CreateNode<DS_SetTextSpeedNode>(tree)
            },
            {
                "DS_CanTeachAnything", tree => CreateNode<DS_CanTeachAnything>(tree)
            },
            {
                "DS_HealActor", tree => CreateNode<DS_HealActor>(tree)
            },
            {
                "DS_EquipNode", tree => CreateNode<DS_EquipNode>(tree)
            },
            {
                "DS_UseItem", tree => CreateNode<DS_UseItem>(tree)
            },
            {
                "DS_SetSpellToActiveAbiSlot", tree => CreateNode<DS_SetSpellToActiveAbiSlot>(tree)
            },
            {
                "DS_UnEquipNodeEditor", tree => CreateNode<DS_UnEquipNode>(tree)
            },
            {
                "DS_KatsaSilverMine", tree => CreateNode<DS_KatsaSilverMine>(tree)
            },
            {
                "DS_LearnStatNode", tree => CreateNode<DS_LearnStatNode>(tree)
            },
            {
                "DS_LearnTalentNode", tree => CreateNode<DS_LearnTalentNode>(tree)
            },
            {
                "DS_ReleaseActiveActors", tree => CreateNode<DS_ReleaseActiveActors>(tree)
            },
            {
                "DS_RestartNode", tree => CreateNode<DS_RestartNode>(tree)
            },
            {
                "DS_LearnAttributeNode_Single", tree => CreateNode<DS_LearnAttributeNode_Single>(tree)
            },
            {
                "DS_TeachStatsNode", tree => CreateNode<DS_TeachStatsNode>(tree)
            },
            {
                "DS_PlaySfx", tree => CreateNode<DS_PlaySfx>(tree)
            },
            {
                "DS_DelayedTeach_Statement", tree => CreateNode<DS_DelayedTeach_Statement>(tree)
            },
            {
                "ProbabilitySelector", tree => CreateNode<ProbabilitySelector>(tree)
            },
            {
                "DS_SetGIntNode", tree => CreateNode<DS_SetGIntNode>(tree)
            },
#if DEBUG
            {
                "DS_TeachTalentNode", tree => CreateNode<DS_TeachTalentNode>(tree)
            },
#endif
        };
#region Creation of Nodes
        private static DTNode CreateNode<T>(DialogueTree tree) where T : DTNode
        {
            T node = tree.AddNode<T>();
            node.TryGenerateUID();
            return node;
        }

        private static DS_SetGBoolNode CreateGBoolNode(DialogueTree tree)
        {
            var node = CreateNode<DS_SetGBoolNode>(tree).Cast<DS_SetGBoolNode>();
            node.Variable = new BBParameter<GBool>();
            node.Value = new BBParameter<bool>();
            return node;
        }
#endregion

        /// <summary>
        /// Get all the node names
        /// </summary>
        /// <returns></returns>
        public string[] GetNodeNames()
        {
            return [.. nameToNodeMap.Keys];
        }

        /// <summary>
        /// Add a <see cref="DrawNodeEditor"/> type to the factory
        /// </summary>
        /// <param name="name">Name of the il2cpp type<see cref="Il2CppSystem.Reflection.MemberInfo.Name"/></param>
        /// <param name="type">DrawNodEditor type</param>
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
        /// <param name="name">The Name of the Node</param>
        /// <returns></returns>
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
        /// Creates a new dialogue node from a known node name.
        /// </summary>
        public DTNode? CreateNodeByName(DialogueTree tree, string name)
        {
            if (!_nodeCreators.TryGetValue(name, out Func<DialogueTree, DTNode> creator))
            {
                return null;
            }

            try
            {
                return creator.Invoke(tree);
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error creating dialogue node {0}: {1}", name, e);
                return null;
            }
        }

        /// <summary>
        /// Get a DrawNodeEditor by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual DrawNodeEditor? GetDrawNodeEditorFromType(Il2CppSystem.Type type)
        {
            try
            {
                if (nameToNodeMap.TryGetValue(type.Name, out Type v))
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
    }
}