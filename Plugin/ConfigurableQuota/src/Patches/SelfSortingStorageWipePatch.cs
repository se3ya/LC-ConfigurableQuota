using System;
using System.Reflection;
using HarmonyLib;
using ConfigurableQuota.Compat;

namespace ConfigurableQuota.Patches
{
    internal static class SelfSortingStorageWipePatch
    {
        internal static void TryPatch(Harmony harmony)
        {
            if (!SelfSortingStorageCompat.IsInstalled) return;

            try
            {
                MethodInfo? target = SelfSortingStorageCompat.FindResetOnAllDeadMethod();
                if (target == null)
                {
                    return;
                }

                MethodInfo? prefix = typeof(SelfSortingStorageWipePatch)
                    .GetMethod(nameof(SkipWhenLossesHandled), BindingFlags.NonPublic | BindingFlags.Static);

                harmony.Patch(target, prefix: new HarmonyMethod(prefix));

                Plugin.Log.LogInfo("SSS stored items now follow crew wipe loss settings.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not patch SSS crew wipe handler: {e.Message}");
            }
        }

        private static bool SkipWhenLossesHandled()
        {
            return !PenaltiesOnLandingPatch._lossesAppliedThisRound;
        }
    }
}
