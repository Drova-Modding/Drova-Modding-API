using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace Drova_Modding_API
{
    /// <summary>
    /// Applies Harmony patches by hand, one target at a time, with per-hook logging.
    ///
    /// Prefer this over <c>[HarmonyPatch]</c> attributes whenever a hook target might not
    /// exist (game updates, stripped IL2CPP bodies) or needs a runtime preflight: MelonLoader
    /// applies attribute patches in bulk at melon registration, so one bad target can take the
    /// whole set down and cannot be gated on a runtime check. Manual application isolates each
    /// failure to its own hook and leaves an honest line in the log either way.
    /// </summary>
    public static class Hooking
    {
        /// <summary>
        /// Patch <paramref name="targetType"/>.<paramref name="targetMethod"/> with a postfix.
        /// </summary>
        /// <param name="harmony">The Harmony instance of your melon.</param>
        /// <param name="targetType">Type that declares the method to patch.</param>
        /// <param name="targetMethod">Name of the method to patch.</param>
        /// <param name="patchType">Type that declares the patch method.</param>
        /// <param name="patchMethod">Name of the (static) patch method.</param>
        /// <returns>True when the patch was applied.</returns>
        public static bool TryPostfix(HarmonyLib.Harmony harmony, Type targetType, string targetMethod, Type patchType, string patchMethod)
        {
            MethodInfo? target = FindTarget(targetType, targetMethod);
            if (target == null)
            {
                return false;
            }
            return Apply(harmony, target, patchType, patchMethod, asPrefix: false, Label(targetType, targetMethod));
        }

        /// <summary>
        /// Patch <paramref name="targetType"/>.<paramref name="targetMethod"/> with a prefix.
        /// </summary>
        /// <inheritdoc cref="TryPostfix(HarmonyLib.Harmony, Type, string, Type, string)"/>
        public static bool TryPrefix(HarmonyLib.Harmony harmony, Type targetType, string targetMethod, Type patchType, string patchMethod)
        {
            MethodInfo? target = FindTarget(targetType, targetMethod);
            if (target == null)
            {
                return false;
            }
            return Apply(harmony, target, patchType, patchMethod, asPrefix: true, Label(targetType, targetMethod));
        }

        /// <summary>
        /// Postfix overload for targets the caller already resolved (e.g. a specific overload via
        /// <see cref="AccessTools.Method(Type, string, Type[], Type[])"/>, or a method that needed
        /// a preflight such as <see cref="Access.QuestStateReader.HasNativeBody"/>).
        /// </summary>
        /// <param name="harmony">The Harmony instance of your melon.</param>
        /// <param name="target">The resolved target method, may be null.</param>
        /// <param name="patchType">Type that declares the patch method.</param>
        /// <param name="patchMethod">Name of the (static) patch method.</param>
        /// <param name="label">Human-readable target name for the log.</param>
        /// <returns>True when the patch was applied.</returns>
        public static bool TryPostfix(HarmonyLib.Harmony harmony, MethodBase? target, Type patchType, string patchMethod, string label)
        {
            if (target == null)
            {
                MelonLogger.Error("[Hooking] no target for " + label + "; hook disabled.");
                return false;
            }
            return Apply(harmony, target, patchType, patchMethod, asPrefix: false, label);
        }

        /// <summary>
        /// Prefix overload for targets the caller already resolved.
        /// </summary>
        /// <inheritdoc cref="TryPostfix(HarmonyLib.Harmony, MethodBase?, Type, string, string)"/>
        public static bool TryPrefix(HarmonyLib.Harmony harmony, MethodBase? target, Type patchType, string patchMethod, string label)
        {
            if (target == null)
            {
                MelonLogger.Error("[Hooking] no target for " + label + "; hook disabled.");
                return false;
            }
            return Apply(harmony, target, patchType, patchMethod, asPrefix: true, label);
        }

        private static MethodInfo? FindTarget(Type targetType, string targetMethod)
        {
            try
            {
                MethodInfo? target = AccessTools.Method(targetType, targetMethod);
                if (target == null)
                {
                    MelonLogger.Error("[Hooking] target method not found: " + Label(targetType, targetMethod) + "; hook disabled.");
                }
                return target;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Hooking] resolving " + Label(targetType, targetMethod) + " threw: " + e);
                return null;
            }
        }

        private static bool Apply(HarmonyLib.Harmony harmony, MethodBase target, Type patchType, string patchMethod, bool asPrefix, string label)
        {
            try
            {
                MethodInfo? patch = AccessTools.Method(patchType, patchMethod);
                if (patch == null)
                {
                    MelonLogger.Error("[Hooking] patch method " + patchType.Name + "." + patchMethod + " not found; " + label + " disabled.");
                    return false;
                }

                HarmonyMethod wrapped = new(patch);
                if (asPrefix)
                {
                    harmony.Patch(target, prefix: wrapped);
                }
                else
                {
                    harmony.Patch(target, postfix: wrapped);
                }
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Hooking] FAILED to patch " + label + ": " + e);
                return false;
            }
        }

        private static string Label(Type targetType, string targetMethod)
        {
            return targetType.Name + "." + targetMethod;
        }
    }
}
