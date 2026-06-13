using Il2CppNodeCanvas.Framework;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    /// <summary>
    /// Class that helps create a condition task
    /// </summary>
    public class GUICreateConditionTask
    {
        private static readonly List<(GUICreateConditionTask task, Vector2 position)> PendingOverlays = [];

        private ConditionTask? _selectedTaskThisFlush;

        private readonly Dictionary<string, Il2CppSystem.Type> _nameToTaskMap = new()
        {
            {"DS_CheckGVarListConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckGVarListConditionTask") },
            {"GBoolConditionTask", Il2CppSystem.Type.GetType("Drova.QuestSystem.Graphs.GBoolConditionTask") },
            {"GIntConditionTask", Il2CppSystem.Type.GetType("Drova.QuestSystem.Graphs.GIntConditionTask") },
            {"DS_HasItems", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_HasItems") },
            {"DS_HasAttribute", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_HasAttribute") },
            {"ConditionList", Il2CppSystem.Type.GetType("NodeCanvas.Framework.ConditionList") },
            {"GQuestStateConditionTask", Il2CppSystem.Type.GetType("Drova.QuestSystem.Graphs.GQuestStateConditionTask") },
            {"DS_HasOpenReactionType", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_HasOpenReactionType") },
            {"DS_HasItemEquippedTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_HasItemEquippedTask") },
            {"DS_HasTalents", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_HasTalents") },
            {"DS_CanAtoneCrimeConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CanAtoneCrimeConditionTask") },
            {"DS_CanLearnAttributeConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CanLearnAttributeConditionTask") },
            {"DS_CanLearnTalentConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CanLearnTalentConditionTask") },
            {"DS_CheckAndResetGBoolConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckAndResetGBoolConditionTask") },
            {"DS_CheckAndResetGlobalBBVarTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckAndResetGlobalBBVarTask") },
            {"DS_CheckBbIntegerConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckBbIntegerConditionTask") },
            {"DS_CheckForGlobalVariableCondition", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckForGlobalVariableCondition") },
            {"DS_CheckCrimeStrength", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckCrimeStrength") },
            {"DS_CheckForInstigatorConditionTask", Il2CppSystem.Type.GetType("Drova.DialogueNew.DS_CheckForInstigatorConditionTask") }
        };

        private readonly string[] _tooltips =
        [
            "Check if any bool in the list is value",
            "Check if all bools are value",
            "Check if all int are value",
            "Check if the target or player has the items",
            "Check if the player has the attributes",
            "Collection of Condition Tasks",
            "Check if the quest state is the same as value",
            "Check for open reaction type",
            "Check if player has items equipped",
            "Check if Player has learned defined Attributes",
            "Check if owner witnessed crime of player",
            "Check if Attribute, defined in following TeachStatsNode can be learned.",
            "Check if Talents, defined in following TeachTalentNode can be learned",
            "Check then Reset a variable in global variables",
            "Check then Reset a variable in global variables",
            "Check for a variable in global variables",
            "Checks fighting strength + 30 is greater than players fighting strength",
            "Checks if EntityInfo is INSTIGATOR"
        ];

        /// <summary>
        /// Size of the selection
        /// </summary>
        public Vector2Int Size => new(220, 20 + (_nameToTaskMap.Count * 20));

        /// <summary>
        /// Draws all deferred condition task creation overlays.
        /// </summary>
        public static void FlushOverlays()
        {
            (GUICreateConditionTask task, Vector2 position)[] toFlush = [.. PendingOverlays];
            PendingOverlays.Clear();

            foreach ((GUICreateConditionTask task, Vector2 position) in toFlush)
            {
                task._selectedTaskThisFlush = task.DrawOverlay(position);
            }
        }

        /// <summary>
        /// Draw the condition task selection
        /// </summary>
        /// <param name="position">position of the selection</param>
        public ConditionTask? Draw(Vector2 position)
        {
            ConditionTask? selectedTask = _selectedTaskThisFlush;
            _selectedTaskThisFlush = null;

            PendingOverlays.Add((this, position));

            return selectedTask;
        }

        private ConditionTask? DrawOverlay(Vector2 position)
        {
            Rect rect = new(position.x, position.y, 220, 20);
            GUI.Label(rect, "Select a condition task");
            rect.y += 20;

            for (int i = 0; i < _nameToTaskMap.Count; i++)
            {
                KeyValuePair<string, Il2CppSystem.Type> item = _nameToTaskMap.ElementAt(i);
                GUIContent content = new(item.Key, _tooltips[i]);
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
                if (_nameToTaskMap.TryGetValue(name, out Il2CppSystem.Type type))
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
