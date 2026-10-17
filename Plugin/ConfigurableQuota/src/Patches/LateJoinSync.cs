using System;
using HarmonyLib;
using Unity.Netcode;

namespace ConfigurableQuota.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal static class LateJoinSync
{
    [HarmonyPatch(nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc))]
    [HarmonyPostfix]
    private static void SyncAlreadyHeldObjectsServerRpc_Postfix(StartOfRound __instance, int joiningClientId)
    {
        if (!__instance.IsServer) return;

        ulong clientId = (ulong)joiningClientId;
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        ResyncToClient(clientId);
    }

    private static void ResyncToClient(ulong clientId)
    {
        try
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

            var tod = TimeOfDay.Instance;
            NetworkSync.SyncDeadlineLengthToClient(tod.quotaVariables.deadlineDaysAmount, clientId);
            NetworkSync.SyncQuotaToClient(tod.profitQuota, clientId);
            NetworkSync.SyncRolloverToClient(tod.quotaFulfilled, clientId);
            NetworkSync.SyncBuyingRateToClient(StartOfRound.Instance.companyBuyingRate, isJackpot: false, clientId, silent: true);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Late join resync to client {clientId} failed: {e.Message}");
        }
    }
}
