using Il2CppDrova.DialogueNew;
using Il2CppDrova.QuestSystem.Graphs;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    /// <summary>
    /// Class that helps creating a condition task
    /// </summary>
    public class GUICreateConditionTask
    {
        private readonly Dictionary<string, Type> nameToTaskMap = new()
        {
            {"DS_CheckGVarListConditionTask", typeof(DS_CheckGVarListConditionTask) },
            {"GBoolConditionTask", typeof(GBoolConditionTask) },
        };

        private readonly string[] tooltips =
        [
            "Check if any bool in the list is value",
            "Check if all bools are value"
        ];

        /// <summary>
        /// Size of the selection
        /// </summary>
        public Vector2Int Size => new(220, 20 + nameToTaskMap.Count * 20);

        /// <summary>
        /// Draw the condition task selection
        /// </summary>
        /// <param name="position">position of the selection</param>
        public ConditionTask? Draw(Vector2 position)
        {
            Rect rect = new(position.x, position.y, 220, 20);
            GUI.Label(rect, "Select a condition task");
            rect.y += 20;

            for (int i = 0; i < nameToTaskMap.Count; i++)
            {
                var item = nameToTaskMap.ElementAt(i);
                var content = new GUIContent(item.Key, tooltips[i]);
                if (GUI.Button(rect, content))
                {
                    return CreateConditionTask(item.Key);
                }
                rect.y += 20;
            }
            return null;
        }

        /// <summary>
        /// Create a condition task by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected virtual ConditionTask? CreateConditionTask(string name)
        {
            try
            {
                if (nameToTaskMap.TryGetValue(name, out Type v))
                {
                    return (ConditionTask)Activator.CreateInstance(v);
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
