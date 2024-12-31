using Drova_Modding_API.Systems.Dialogues.Editor.Nodes;
using Drova_Modding_API.Systems.Dialogues.Editor.Tasks;
using Il2CppDrova.DialogueNew;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Factories
{
    /// <summary>
    /// Factory class that helps creating DrawTaskEditors
    /// </summary>
    public class DrawTaskEditorFactory
    {
        private readonly Dictionary<string, Type> nameToTaskMap = new()
        {
            {"DS_CheckGVarListConditionTask", typeof(DS_CheckGVarListConditionTask) },
            {"GBoolConditionTask", typeof(GBoolConditionTaskEditor) },
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
