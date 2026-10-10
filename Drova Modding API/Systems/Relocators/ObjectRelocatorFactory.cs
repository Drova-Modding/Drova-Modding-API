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
        private static readonly Dictionary<Type, object> RelocatorByType = new(){
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
        public static void RegisterRelocator<T>(IObjectRelocator<T> relocator)
        {
            RelocatorByType[typeof(T)] = relocator;
            
        }
        /// <summary>
        /// Gets the relocator for a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static IObjectRelocator<T>? GetRelocator<T>()
        {
            if (RelocatorByType.TryGetValue(typeof(T), out object? relocator))
            {
                return (IObjectRelocator<T>)relocator;
            }
            return null;
        }
    }
}
