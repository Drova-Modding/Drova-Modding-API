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
        private readonly Dictionary<string, Il2CppSystem.Type> nameToTaskMap = new()
        {
            {"DS_CheckGVarListConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckGVarListConditionTask") },
            {"GBoolConditionTask", Il2CppSystem.Type.GetType("Drova.QuestSystem.Graphs.GBoolConditionTask") },
            {"GIntConditionTask", Il2CppSystem.Type.GetType("Drova.QuestSystem.Graphs.GIntConditionTask") },
            {"DS_HasItems", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_HasItems") },
            {"DS_HasAttribute", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_HasAttribute") },
            {"ConditionList", Il2CppSystem.Type.GetType("NodeCanvas.Framework.ConditionList") },
            {"GQuestStateConditionTask", Il2CppSystem.Type.GetType("Drova.QuestSystem.Graphs.GQuestStateConditionTask") }
        };

        private readonly string[] tooltips =
        [
            "Check if any bool in the list is value",
            "Check if all bools are value",
            "Check if all int are value",
            "Check if the target or player has the items",
            "Check if the player has the attributes",
            "Collection of Condition Tasks",
            "Check if the quest state is the same as value"
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
                KeyValuePair<string, Il2CppSystem.Type> item = nameToTaskMap.ElementAt(i);
                GUIContent content = new(item.Key, tooltips[i]);
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
                if (nameToTaskMap.TryGetValue(name, out Il2CppSystem.Type type))
                {
                    return Il2CppSystem.Activator.CreateInstance(type).TryCast<ConditionTask>();
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
