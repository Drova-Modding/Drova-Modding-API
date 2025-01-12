using Drova_Modding_API.Systems.Dialogues.Editor.Nodes;
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
            { "DS_StatementNode", typeof(DS_StatementNodeEditor) },
            { "DS_MultipleChoiceNode", typeof(DS_MultipleChoiceNodeEditor) },
            { "DS_GiveExp", typeof(DS_GiveExpNodeEditor) },
            { "DS_ChangeStanceNode", typeof(DS_ChangeStanceNodeEditor) },
            { "DS_DebugNode", typeof(DS_DebugNodeEditor) },
            { "DS_HideDialogWindow", typeof(DS_HideDialogWindowNodeEditor) },
            { "DS_SetFactionNode", typeof(DS_SetFactionNodeEditor) },
            { "DS_SetFirstChapter", typeof(DS_SetFirstChapterNodeEditor) },
            { "DS_InteractAABaseNode", typeof(DS_InteractAABaseNodeEditor) },
            { "DS_GiveItemNode", typeof(DS_GiveItemNodeEditor) },
            { "DS_RevisitMultipleChoiceNode", typeof(DS_RevisitMultipleChoiceNodeEditor) },
            { "DS_DefineActiveActors", typeof(DS_DefineActiveActorsNodeEditor) },
            { "MultipleConditionNode", typeof(MultipleConditionNodeEditor) },
            { "DS_SetGBoolNode", typeof(DS_SetGBoolNodeEditor) },
            { "ConditionNode", typeof(ConditionNodeEditor) },
            { "DS_HubNode", typeof(DS_HubNodeEditor) },
            { "DS_HubJumpNode", typeof(DS_HubJumpNodeEditor) },
            { "FinishNode", typeof(FinishNodeEditor) },
            { "SubDialogueTree", typeof(SubDialogueTreeEditor) },
            { "DS_OverrideLookAtSpeaker", typeof(DS_OverrideLookAtSpeakerNodeEditor) },
            { "DS_OverrideFixCamPos", typeof(DS_OverrideFixCamPosNodeEditor) },
            { "CS_CutsceneActionNode", typeof(CS_CutsceneActionNodeEditor) },
            { "DS_SetDialogueRequirementsNode", typeof(DS_SetDialogueRequirementsNodeEditor) },
            { "DS_OpenTradeWindowNode", typeof(DS_OpenTradeWindowNodeEditor) },
            { "DS_SetTextSpeedNode", typeof(DS_SetTextSpeedNodeEditor) },
            { "DS_CanTeachAnything", typeof(DS_CanTeachAnythingNodeEditor) },
            { "DS_HealActor", typeof(DS_CanTeachAnythingNodeEditor) },
            { "DS_UnEquipNodeEditor", typeof(DS_UnEquipNodeEditor) },
            { "DS_KatsaSilverMine", typeof(DS_KatsaSilverMineNodeEditor) },
            { "DS_LearnStatNode", typeof(DS_LearnStatNodeEditor) },
            { "DS_LearnTalentNode", typeof(DS_LearnTalentNodeEditor) },
            { "DS_ReleaseActiveActors", typeof(DS_ReleaseActiveActorsNodeEditor) },
            { "DS_RestartNode", typeof(DS_RestartNodeEditor) },
            { "DS_LearnAttributeNode_Single", typeof(DS_LearnAttributeNode_SingleNodeEditor) },
            { "DS_PlaySfx", typeof(DS_PlaySfxNodeEditor) },
            { "DS_DelayedTeach_Statement", typeof(DS_DelayedTeach_StatementNodeEditor) },
            { "ProbabilitySelector", typeof(ProbabilitySelectorNodeEditor) },
            { "DS_SetGIntNode", typeof(DS_SetGIntNodeEditor) },
        };

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
