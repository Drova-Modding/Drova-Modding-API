using Drova_Modding_API.Systems.Spawning.Overrides;
using Il2CppDrova;
using Il2CppDrova.Routine;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    public class RoutineModule : INpcModule
    {
        private readonly List<Vector2> _positions = new();

        public RoutineModule With(params Vector2[] positions)
        {
            _positions.AddRange(positions);
            return this;
        }

        public void Apply(ModuleContext context)
        {
            var actor = context.GetComponent<Actor>();
            var routineHandler = context.GetComponentInChildren<RoutineHandler>();
            var mapHandler = MapGameHandler.TryGet().RoutineMapHandler;
            var routineModule = actor._routineModule;
            var keyRoutine = new ModdedRoutineKeyGenerator();
            
            routineModule._mapHandler = mapHandler;
            routineModule._routineHandler = routineHandler;
            routineModule._keyGenerator = keyRoutine.Cast<IRoutineKeyGenerator>();
            routineModule.CreateRoutineKeys();
            routineModule._isDirty = true;
            
            mapHandler.OnRoutineDirtyEvent.AddEventListener(new Action(routineModule.OnDirtyEventListener));
            
            if(routineHandler) {
                routineModule._routineHandler.OnRoutineStateChangedEvent.AddEventListener(new Action<RoutineState>((routineState) => actor._routineModule.RoutineStateChangedEventListener(routineState)));
                routineHandler.SetActor(actor);
            }
        }
    }
}