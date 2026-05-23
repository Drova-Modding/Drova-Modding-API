using Drova_Modding_API.Systems.Routines;
using Il2CppDrova;
using Il2CppDrova.Routine;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Configures and attaches routine behavior to an NPC and cleans up routine objects on lazy actor teardown.
    /// </summary>
    public class RoutineModule : INpcModule
    {
        private readonly List<Vector2> _positions = [];
        private readonly Dictionary<IntPtr, List<GameObject>> _routineObjectsByLazyActor = [];

        /// <summary>
        /// Adds one or more routine points that will be created for the NPC.
        /// </summary>
        /// <param name="positions">World positions used for generated routine points.</param>
        /// <returns>This module instance for fluent chaining.</returns>
        public RoutineModule With(params Vector2[] positions)
        {
            _positions.AddRange(positions);
            return this;
        }

        /// <summary>
        /// Applies routine wiring for the spawned NPC and creates a routine place under <see cref="RoutineSystem.RoutineRoot"/>.
        /// </summary>
        /// <param name="context">Creation context containing the NPC and optional lazy actor.</param>
        public void Apply(ModuleContext context)
        {
            var actor = context.GetComponent<Actor>();
            var routineHandler = context.GetComponentInChildren<RoutineHandler>();
            var mapHandler = MapGameHandler.TryGet().RoutineMapHandler;
            var routineModule = actor._routineModule;
            var keyRoutine = new RoutineKeyGenerator();
            
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
            
            var routinePlace = RoutineSystem.CreateRoutinePlace(context.LazyActor, actor._entityInfo, [.. _positions]);
            if (routinePlace != null && context.LazyActor != null)
            {
                IntPtr key = context.LazyActor.Pointer;
                if (!_routineObjectsByLazyActor.TryGetValue(key, out List<GameObject> routineObjects))
                {
                    routineObjects = [];
                    _routineObjectsByLazyActor[key] = routineObjects;
                }
                routineObjects.Add(routinePlace.gameObject);
            }
        }

        /// <summary>
        /// Destroys routine objects previously created for the destroyed lazy actor.
        /// </summary>
        /// <param name="context">Cleanup context containing the destroyed lazy actor.</param>
        public void Cleanup(ModuleContext context)
        {
            if (context.LazyActor == null)
                return;

            IntPtr key = context.LazyActor.Pointer;
            if (!_routineObjectsByLazyActor.TryGetValue(key, out List<GameObject> routineObjects))
                return;

            for (int i = 0; i < routineObjects.Count; i++)
            {
                GameObject routineObject = routineObjects[i];
                if (routineObject != null)
                    UnityEngine.Object.Destroy(routineObject);
            }

            _routineObjectsByLazyActor.Remove(key);
        }
    }
}