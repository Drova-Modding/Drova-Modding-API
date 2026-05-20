using Il2CppDrova;
using Il2CppDrova.Routine;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using Object = Il2CppSystem.Object;

namespace Drova_Modding_API.Systems.Spawning.Overrides
{
    [RegisterTypeInIl2CppWithInterfaces(typeof(IRoutineKeyGenerator))]
    public class ModdedRoutineKeyGenerator(IntPtr ptr) : Object(ptr)
    {
        private Guid _guid = Guid.NewGuid();

        private readonly String _cachedGuidString;
        
        public ModdedRoutineKeyGenerator() : this(ClassInjector.DerivedConstructorPointer<ModdedRoutineKeyGenerator>()) {
            ClassInjector.DerivedConstructorBody(this);
            this._cachedGuidString = this._guid.ToString();
        }
        
        public Il2CppSystem.String GenerateRoutinePlaceKey(EntityInfo entity, RoutineState state)
        {
            return _cachedGuidString;
        }

        public Il2CppSystem.String GenerateRoutinePlaceKey(Il2CppSystem.String guid, RoutineState state)
        {
            return _cachedGuidString;
        }
    }
}