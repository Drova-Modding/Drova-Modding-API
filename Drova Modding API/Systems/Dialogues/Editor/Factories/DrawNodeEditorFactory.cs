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
            { "CS_CutsceneActionNode", typeof(CS_CutsceneActionNodeEditor) }
        };

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
