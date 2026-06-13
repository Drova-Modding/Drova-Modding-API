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
            {"DS_IsUniqueConditionTask", typeof(DS_IsUniqueConditionTaskEditor) },
            {"DS_HasOpenReactionType", typeof(DS_HasOpenReactionTypeTaskEditor) },
            {"DS_HasItemEquippedTask", typeof(DS_HasItemEquippedTaskEditor) },
            {"DS_HasTalents", typeof(DS_HasTalentsTaskEditor) },
            {"DS_CanAtoneCrimeConditionTask", typeof(DS_CanAtoneCrimeConditionTaskEditor) },
            {"DS_CanLearnAttributeConditionTask", typeof(DS_CanLearnAttributeConditionTaskEditor) },
            {"DS_CanLearnTalentConditionTask", typeof(DS_CanLearnTalentConditionTaskEditor) },
            {"DS_CheckAndResetGBoolConditionTask", typeof(DS_CheckAndResetGBoolConditionTaskEditor) },
            {"DS_CheckAndResetGlobalBBVarTask", typeof(DS_CheckAndResetGlobalBBVarTaskEditor) },
            {"DS_CheckBbIntegerConditionTask", typeof(DS_CheckBbIntegerConditionTaskEditor) },
            {"DS_CheckForGlobalVariableCondition", typeof(DS_CheckForGlobalVariableConditionTaskEditor) },
            {"DS_CheckCrimeStrength", typeof(DS_CheckCrimeStrengthTaskEditor) },
            {"DS_CheckForInstigatorConditionTask", typeof(DS_CheckForInstigatorConditionTaskEditor) },
            {"DS_CheckLootInventoryEmptyConditionTask", typeof(DS_CheckLootInventoryEmptyConditionTaskEditor) },
            {"DS_FactionCheckConditionTask", typeof(DS_FactionCheckConditionTaskEditor) },
            {"DS_HasNextMPCNodeValidChoices", typeof(DS_HasNextMPCNodeValidChoicesTaskEditor) },
            {"DS_HasPlayerActiveCrimes", typeof(DS_HasPlayerActiveCrimesTaskEditor) },
            {"DS_JudgeCanAtoneCrimeConditionTask", typeof(DS_JudgeCanAtoneCrimeConditionTaskEditor) },
            {"DS_WitnessedCrimeConditionTask", typeof(DS_WitnessedCrimeConditionTaskEditor) },
            {"DS_WitnessedOpenReactionCrimeConditionTask", typeof(DS_WitnessedOpenReactionCrimeConditionTaskEditor) },
            {"DS_WitnessedSpecificCrimeConditionTask", typeof(DS_WitnessedSpecificCrimeConditionTaskEditor) }
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
