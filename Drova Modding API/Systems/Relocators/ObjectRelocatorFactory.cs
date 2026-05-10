using Il2CppDrova;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;

namespace Drova_Modding_API.Systems.Editor.Relocators
{
    /// <summary>
    /// Factory for getting and register object relocators.
    /// </summary>
    public class ObjectRelocatorFactory
    {
        private readonly static Dictionary<Type, object> _relocators = new(){
            { typeof(Item), new ItemRelocator() },
            { typeof(DialogueTree), new DialogueTreeRelocator() },
            { typeof(EntityInfo), new EntityInfoRelocator() },
            { typeof(GVarList), new GVarListRelocator() },
            { typeof(GInt), new GVarIntRelocator() },
            { typeof(GBool), new GVarBoolRelocator() },
            { typeof(GQuestState), new GVarQuestStateRelocator() },
        };
        /// <summary>
        /// Registers a relocator for a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="relocator"></param>
        public static void RegisterRelocator<T>(IObjectRelocator<T> relocator)
        {
            _relocators[typeof(T)] = relocator;
        }
        /// <summary>
        /// Gets the relocator for a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IObjectRelocator<T> GetRelocator<T>()
        {
            if (_relocators.TryGetValue(typeof(T), out object relocator))
            {
                return (IObjectRelocator<T>)relocator;
            }
            return null;
        }
    }
}
