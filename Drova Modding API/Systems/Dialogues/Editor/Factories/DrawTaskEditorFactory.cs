using Drova_Modding_API.Systems.Dialogues.Editor.Tasks;
using MelonLoader;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Factories
{
    /// <summary>
    /// Factory class that helps creating DrawTaskEditors
    /// </summary>
    public class DrawTaskEditorFactory
    {
        private readonly Dictionary<string, Type> nameToTaskMap = new()
        {
            {"DS_CheckGVarListConditionTask", typeof(DS_CheckGVarListConditionTaskEditor) },
            {"GBoolConditionTask", typeof(GBoolConditionTaskEditor) },
            {"GIntConditionTask", typeof(GIntConditionTaskEditor) },
            {"DS_HasItems", typeof(DS_HasItemsTaskEditor) },
            {"DS_HasAttribute", typeof(DS_HasAttributeTaskEditor) },
            {"ConditionList", typeof(ConditionListTaskEditor) },
            {"GQuestStateConditionTask", typeof(GQuestStateConditionTaskEditor) },
            {"DS_IsUniqueConditionTask", typeof(DS_IsUniqueConditionTaskEditor) }
        };

        /// <summary>
        /// Get a DrawNodeEditor by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual DrawTaskEditor? GetDrawTaskEditorFromType(Il2CppSystem.Type? type)
        {
            // Special case for empty task, i guess its be used, when no other task is true as a fallback or alternative path
            if (type == null)
            {
                return new NullTaskEditor();
            }
            try
            {
                if (nameToTaskMap.TryGetValue(type.Name, out Type v))
                {
                    return (DrawTaskEditor)Activator.CreateInstance(v);
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
