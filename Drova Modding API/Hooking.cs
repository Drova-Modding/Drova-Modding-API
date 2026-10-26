using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace Drova_Modding_API
{
    /// <summary>
    /// Applies Harmony patches one target at a time with per-hook logging, so one missing target
    /// does not take down the whole attribute-patch set.
    /// </summary>
    public static class Hooking
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        // Locked because some hooked methods run on the thread pool (graph deserialization).
        private static readonly Dictionary<MethodBase, int> CallsByTarget = new();
        private static readonly List<(string Label, MethodBase Target)> Applied = new();

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
        /// Postfix overload for targets the caller already resolved, such as a specific overload.
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
                // A name-only lookup prefers an inherited public method over the type's own private one.
                if (targetType.GetMember(targetMethod, MemberTypes.Method, Declared).Length > 0)
                {
                    return FindDeclaredTarget(targetType, targetMethod);
                }

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
                return FindDeclaredTarget(targetType, targetMethod);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Hooking] resolving " + Label(targetType, targetMethod) + " threw: " + e);
                return null;
            }
        }

        // Several overloads are refused, since guessing would patch the wrong method and report success.
        private static MethodInfo? FindDeclaredTarget(Type targetType, string targetMethod)
        {
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

                Applied.Add((label + (asPrefix ? " prefix" : " postfix"), target));
                bool counted;
                lock (CallsByTarget)
                {
                    counted = !CallsByTarget.TryAdd(target, 0);
                }

                if (!counted)
                {
                    harmony.Patch(target, prefix: new HarmonyMethod(AccessTools.Method(typeof(Hooking), nameof(CountPrefix))) { priority = Priority.First });
                }
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Hooking] FAILED to patch " + label + ": " + e);
                return false;
            }
        }

        /// <summary>
        /// How often each hook applied through this class has fired, by label.
        /// A prefix and a postfix on one method share a count.
        /// </summary>
        public static IReadOnlyList<(string Label, int Calls)> FiredCounts()
        {
            List<(string Label, int Calls)> counts = new(Applied.Count);
            foreach ((string label, MethodBase target) in Applied)
            {
                lock (CallsByTarget)
                {
                    counts.Add((label, CallsByTarget.GetValueOrDefault(target)));
                }
            }
            return counts;
        }

        /// <summary>Labels of applied hooks that have not fired yet, often an inlined target.</summary>
        public static IReadOnlyList<string> NeverFired()
        {
            return FiredCounts().Where(c => c.Calls == 0).Select(c => c.Label).ToList();
        }

        private static void CountPrefix(MethodBase __originalMethod)
        {
            if (__originalMethod == null)
            {
                return;
            }

            lock (CallsByTarget)
            {
                if (CallsByTarget.TryGetValue(__originalMethod, out int calls))
                {
                    CallsByTarget[__originalMethod] = calls + 1;
                }
            }
        }

        private static string Label(Type targetType, string targetMethod)
        {
            return targetType.Name + "." + targetMethod;
        }
    }
}
