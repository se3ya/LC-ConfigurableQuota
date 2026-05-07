using System;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal static class StartOfRoundPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void ApplyStartingCredits(Terminal __instance)
        {
            try
            {
                int desired = ConfigManager.StartingCredits.Value;

                if (desired >= 0)
                {
                    __instance.groupCredits = desired;
                    Plugin.Log.LogInfo($"Starting credits set to {desired}.");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not apply starting credits: {e.Message}");
            }

            try
            {
                var tod = TimeOfDay.Instance;
                if (tod == null || tod.timesFulfilledQuota != 0) return;
                if (!((NetworkBehaviour)tod).IsServer) return;

                if (ConfigManager.RandomizeDeadline.Value)
                {
                    NetworkSync.SyncDeadlineToClients(tod.daysUntilDeadline);
                    Plugin.Log.LogInfo($"Initial deadline synced: {tod.daysUntilDeadline} days.");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not sync the initial deadline: {e.Message}");
            }
        }
    }
}
