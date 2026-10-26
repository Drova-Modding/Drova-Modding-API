using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;

namespace Drova_Modding_API.Extensions
{
    /// <summary>
    /// Extensions for any IL2CPP object.
    /// </summary>
    public static class Il2CppObjectExtensions
    {
        /// <summary>
        /// <c>TryCast</c> that reuses the interop's pooled wrapper instead of allocating one, for casts made every frame.
        /// </summary>
        public static T? PooledCast<T>(this Il2CppObjectBase value) where T : Il2CppObjectBase
        {
            IntPtr pointer = value.Pointer;
            if (!IL2CPP.il2cpp_class_is_assignable_from(Il2CppClassPointerStore<T>.NativeClassPtr, IL2CPP.il2cpp_object_get_class(pointer))) return null;

            return Il2CppObjectPool.Get<T>(pointer);
        }
    }
}
