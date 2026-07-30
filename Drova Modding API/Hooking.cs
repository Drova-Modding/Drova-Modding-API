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
            catch (AmbiguousMatchException)
            {
                return FindDeclaredTarget(targetType, targetMethod);
            }
            catch (Exception e) when (e.InnerException is AmbiguousMatchException)
            {
                // Harmony wraps it. Unwrapped here rather than matched on the message, which is localized.
                return FindDeclaredTarget(targetType, targetMethod);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Hooking] resolving " + Label(targetType, targetMethod) + " threw: " + e);
                return null;
            }
        }

        /// <summary>
        /// Resolves a name that matched more than once, by looking only at what the named type declares.
        ///
        /// **Almost always an inherited overload rather than a real choice.** A name-only lookup walks base
        /// types, so a static <c>Foo(A, B, int)</c> on a derived type and the instance <c>Foo(B)</c> on its
        /// base that calls it are two matches for "Foo" - and the caller who named the derived type meant the
        /// one it declares. Narrowing to that is the tie-break; the interop assemblies are full of these
        /// pairs and the alternative is a hook that cannot be applied by name at all.
        ///
        /// Still refused when the type itself declares several. Which overload is meant is then genuinely the
        /// caller's decision, and guessing it would apply a hook to the wrong method and report success -
        /// pass a resolved <see cref="MethodBase"/> instead, from
        /// <see cref="AccessTools.Method(Type, string, Type[], Type[])"/>.
        /// </summary>
        private static MethodInfo? FindDeclaredTarget(Type targetType, string targetMethod)
        {
            const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            MethodInfo[] declared = targetType.GetMethods(Declared);

            MethodInfo? found = null;
            int matches = 0;

            for (int i = 0; i < declared.Length; i++)
            {
                if (declared[i].Name != targetMethod) continue;

                found = declared[i];
                matches++;
            }

            if (matches == 1)
            {
                return found;
            }

            MelonLogger.Error("[Hooking] " + Label(targetType, targetMethod) + " matches " + matches +
                " methods declared on that type and cannot be resolved by name; pass a resolved MethodBase " +
                "(AccessTools.Method with parameter types). Hook disabled.");

            return null;
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
