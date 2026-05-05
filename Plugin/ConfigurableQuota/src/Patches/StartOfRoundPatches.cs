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
                    Plugin.Log.LogInfo($"Applied starting credits: {desired}");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to apply StartingCredits: {e.Message}");
            }

            try
            {
                var tod = TimeOfDay.Instance;
                if (tod == null || tod.timesFulfilledQuota != 0) return;
                if (!((NetworkBehaviour)tod).IsServer) return;

                if (ConfigManager.RandomizeDeadline.Value)
                {
                    NetworkSync.SyncDeadlineToClients(tod.daysUntilDeadline);
                    Plugin.Log.LogInfo($"[Lobby] Initial deadline synced: {tod.daysUntilDeadline}d");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to sync initial deadline: {e.Message}");
            }
        }
    }
}
