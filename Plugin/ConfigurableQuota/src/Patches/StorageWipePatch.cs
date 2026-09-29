using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ConfigurableQuota.Compat;

namespace ConfigurableQuota.Patches
{
    internal static class StorageWipePatch
    {
        internal static List<MethodInfo> FindHandlers(Func<MethodInfo, bool> match)
        {
            var handlers = new List<MethodInfo>();

            MethodInfo? despawn = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound));
            if (despawn == null) return handlers;

            var info = Harmony.GetPatchInfo(despawn);
            if (info == null) return handlers;

            foreach (var patch in info.Postfixes)
            {
                try
                {
                    MethodInfo method = patch.PatchMethod;
                    if (method != null && match(method))
                        handlers.Add(method);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogDebug($"Skipped a despawn postfix from {patch.owner}: {e.Message}");
                }
            }

            return handlers;
        }

        internal static bool PatchSelfSorting(Harmony harmony, MethodInfo target) => Patch(harmony, target, nameof(HideForSelfSorting));

        internal static bool PatchHQoL(Harmony harmony, MethodInfo target) => Patch(harmony, target, nameof(HideForHQoL));

        private static bool Patch(Harmony harmony, MethodInfo target, string prefixName)
        {
            try
            {
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(StorageWipePatch), prefixName),
                    finalizer: new HarmonyMethod(typeof(StorageWipePatch), nameof(Restore)));
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not patch {target.DeclaringType?.FullName}.{target.Name}: {e.Message}");
                return false;
            }
        }

        private static void HideForSelfSorting(out bool __state) => __state = Hide(SelfSortingStorageCompat.Processed);

        private static void HideForHQoL(out bool __state) => __state = Hide(HQoLCompat.Processed);

        private static bool Hide(bool processed)
        {
            var sor = StartOfRound.Instance;
            if (!processed || sor == null || !sor.allPlayersDead) return false;

            sor.allPlayersDead = false;
            return true;
        }

        private static void Restore(bool __state)
        {
            if (__state && StartOfRound.Instance != null)
                StartOfRound.Instance.allPlayersDead = true;
        }
    }
}
