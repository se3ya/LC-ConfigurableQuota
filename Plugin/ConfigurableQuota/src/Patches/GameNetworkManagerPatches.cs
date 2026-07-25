using System;
using System.Collections;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using ConfigurableQuota.Compat;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal static class GameNetworkManagerPatches
    {
        private const float SettleTimeoutSeconds = 12f;

        private static int _firedResetGeneration;

        [HarmonyPatch(nameof(GameNetworkManager.ResetSavedGameValues))]
        [HarmonyPostfix]
        [HarmonyAfter(new[] { ModGUIDs.LETHAL_MOON_UNLOCKS_GUID })]
        private static void ResetSavedGameValues_Postfix(GameNetworkManager __instance)
        {
            try
            {
                if (!LethalConstellationsCompat.IsInstalled)
                    return;

                var tod = TimeOfDay.Instance;
                if (tod == null)
                    return;

                if (!((NetworkBehaviour)tod).IsServer)
                    return;

                TimeOfDayQuotaPatch.ResetInitialDeadlineTracking();

                LethalConstellationsCompat.TryGetCurrentConstellationName(out string staleConstellation);

                int generation = ++_firedResetGeneration;

                MonoBehaviour host = __instance != null ? __instance : (MonoBehaviour)tod;
                host.StartCoroutine(ReapplyDeadlineWhenConstellationSettles(generation, staleConstellation));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not schedule deadline reapply after firing: {e.Message}");
            }
        }

        private static IEnumerator ReapplyDeadlineWhenConstellationSettles(int generation, string staleConstellation)
        {
            bool hadStale = !string.IsNullOrEmpty(staleConstellation);
            float elapsed = 0f;

            while (true)
            {
                if (generation != _firedResetGeneration)
                    yield break;

                var tod = TimeOfDay.Instance;
                if (tod == null)
                    yield break;

                bool settled = false;

                if (tod.timesFulfilledQuota == 0)
                {
                    bool haveName = LethalConstellationsCompat.TryGetCurrentConstellationName(out string current);

                    if (hadStale)
                    {
                        settled = haveName
                            && !string.Equals(current, staleConstellation, StringComparison.OrdinalIgnoreCase);
                    }
                }

                if (settled || elapsed >= SettleTimeoutSeconds)
                {
                    ApplySettledDeadline(tod);
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private static void ApplySettledDeadline(TimeOfDay tod)
        {
            try
            {
                if (tod == null || tod.timesFulfilledQuota != 0)
                    return;

                if (!((NetworkBehaviour)tod).IsServer)
                    return;

                ConstellationDeadlineConfig.RefreshSections();
                TimeOfDayQuotaPatch.ResetInitialDeadlineTracking();

                bool applied = TimeOfDayQuotaPatch.TryApplyInitialDeadlineFromCurrentMode(
                    tod,
                    allowConstellationOverride: true,
                    logSelection: true);

                if (!applied)
                    return;

                NetworkSync.SyncDeadlineToClients(tod.daysUntilDeadline);
                TimeOfDayQuotaPatch.RefreshExternalMonitors();

                Plugin.Log.LogInfo($"Deadline reapplied after firing: {tod.daysUntilDeadline} days.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not reapply deadline after firing: {e.Message}");
            }
        }
    }
}