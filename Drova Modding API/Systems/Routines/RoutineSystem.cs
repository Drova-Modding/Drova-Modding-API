using Il2CppDrova;
using Il2CppDrova.QuestSystem;
using Il2CppDrova.Routine;
using Il2CppDrova.Utilities.LazyLoading;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Routines
{
    /// <summary>
    /// Creates and stores runtime routine objects for lazy actors.
    /// </summary>
    public static class RoutineSystem
    {
        /// <summary>
        /// Parent object used to group routine places created by the modding API.
        /// </summary>
        public static GameObject? RoutineRoot { get; internal set; }

        /// <summary>
        /// Creates a routine place for the provided lazy actor and fills it with wait points.
        /// </summary>
        /// <param name="lazyActor">Owner actor used by the routine place; must not be null.</param>
        /// <param name="entityInfo">Entity info used to initialize routine ownership metadata.</param>
        /// <param name="points">Point positions used to generate child <see cref="WaitRoutinePoint"/> objects.</param>
        /// <returns>The created routine place, or null when creation cannot proceed.</returns>
        public static DefaultRoutinePlace? CreateRoutinePlace(LazyActor? lazyActor, EntityInfo entityInfo, params Vector2[] points)
        {
            if (lazyActor == null)
            {
                MelonLogger.Warning("Attempted to create a routine place for a null lazy actor.");
                return null;
            }
            if (RoutineRoot == null)
            {
                MelonLogger.Warning("RoutineRoot is null, cannot create routine place.");
                return null;
            }
            GameObject newRoutine = new("Routine");
            newRoutine.SetActive(false);
            newRoutine.transform.SetParent(RoutineRoot.transform);
            var guidComponent = newRoutine.AddComponent<GuidComponent>();
            guidComponent.CreateGuid();
            var defaultRoutinePlace = newRoutine.AddComponent<DefaultRoutinePlace>();
            defaultRoutinePlace._guidComponent = guidComponent;
            defaultRoutinePlace._runningActor  = lazyActor;
            defaultRoutinePlace._entity = entityInfo;
            defaultRoutinePlace._conditionModule = new RoutineConditionModule
            {
                _conditionType = RoutineConditionModule.ConditionType.GVar
            };
            var questCondition = new QuestRoutineCondition
            {
                _conditions = new GlobalConditionModule()
            };
            defaultRoutinePlace._conditionModule._routineCondition = questCondition.Cast<IRoutineCondition>();
            for (int i = 0; i < points.Length; i++)
            {
                GameObject gameObject = new("Point " + i);
                gameObject.transform.SetParent(newRoutine.transform);
                var routinePoint = gameObject.AddComponent<WaitRoutinePoint>();
                routinePoint.SetPosition(points[i]);
            }
            defaultRoutinePlace.SetEntityFromGuid(entityInfo);
            newRoutine.SetActive(true);
            return defaultRoutinePlace;
        }

    }
}