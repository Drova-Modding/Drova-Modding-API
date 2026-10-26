using System.Reflection;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.QuestSystem;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.MethodInfo;
using MelonLoader;
using System.Runtime.InteropServices;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Reads a <see cref="QuestState"/> through virtual dispatch, since the typed members of <c>AGVar&lt;QuestState&gt;</c>
    /// may have no AOT-compiled body. Reading <c>AGEnum&lt;T&gt;.Comparer</c>, <c>_operator</c> or <c>_compare</c> throws.
    /// </summary>
    public static class QuestStateReader
    {
        /// <summary>
        /// Read the quest state through the safe, always-compiled virtual path.
        /// </summary>
        /// <param name="gvar">The quest state variable (e.g. <c>GVarList.GetQuestState()</c>).</param>
        /// <param name="state">The state read, valid when this returns true.</param>
        /// <returns>True when the state could be read.</returns>
        public static bool TryRead(AGVarBase? gvar, out QuestState state)
        {
            state = QuestState.None;
            if (gvar == null)
            {
                return false;
            }

            try
            {
                Il2CppSystem.Object? boxed = gvar.GetGenericValue();
                if (boxed == null)
                {
                    return false;
                }

                IntPtr data = IL2CPP.il2cpp_object_unbox(boxed.Pointer);
                if (data == IntPtr.Zero)
                {
                    return false;
                }

                state = (QuestState)Marshal.ReadInt32(data);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[QuestStateReader] GetGenericValue failed: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Preflight for patching or invoking a closed-generic proxy method. A body that was never AOT-compiled
        /// comes back as a dummy with a null method pointer, and patching or invoking it hard-crashes the game.
        /// </summary>
        /// <param name="proxyMethod">The interop proxy method to check.</param>
        /// <param name="detail">Diagnostic detail for the log, filled either way.</param>
        /// <returns>True when the native body exists and the method is safe to patch or call.</returns>
        public static unsafe bool HasNativeBody(MethodBase? proxyMethod, out string detail)
        {
            detail = "";
            try
            {
                if (proxyMethod == null)
                {
                    detail = "proxy method not found";
                    return false;
                }

                FieldInfo? field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(proxyMethod);
                if (field == null)
                {
                    detail = "no NativeMethodInfoPtr_ field";
                    return false;
                }

                // Reading the field runs the closed generic's cctor, which is itself a thing that can throw.
                IntPtr methodInfo = (IntPtr)field.GetValue(null)!;
                if (methodInfo == IntPtr.Zero)
                {
                    detail = "MethodInfo* == 0";
                    return false;
                }

                INativeMethodInfoStruct wrapped = UnityVersionHandler.Wrap((Il2CppMethodInfo*)methodInfo);
                IntPtr body = wrapped.MethodPointer;
                detail = "MethodInfo*=0x" + methodInfo.ToInt64().ToString("X") + " body=0x" + body.ToInt64().ToString("X");
                return body != IntPtr.Zero;
            }
            catch (Exception e)
            {
                detail = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }
    }
}
