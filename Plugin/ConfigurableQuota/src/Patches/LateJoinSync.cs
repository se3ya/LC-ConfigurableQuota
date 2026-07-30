using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ConfigurableQuota.Patches
{
    internal static class LateJoinSync
    {
        private static bool _subscribed;

        internal static void EnsureSubscribed()
        {
            if (_subscribed) return;

            var nm = NetworkManager.Singleton;
            if (nm == null) return;

            nm.OnClientConnectedCallback += OnClientConnected;
            _subscribed = true;
        }

        private static void OnClientConnected(ulong clientId)
        {
            try
            {
                var nm = NetworkManager.Singleton;
                if (nm == null || !nm.IsServer) return;
                if (clientId == nm.LocalClientId) return;

                var host = StartOfRound.Instance;
                if (host == null) return;

                host.StartCoroutine(ResyncRoutine(clientId));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not schedule latejoin resync: {e.Message}");
            }
        }

        private static IEnumerator ResyncRoutine(ulong clientId)
        {
            yield return new WaitForSeconds(4f);
            ResyncToClient(clientId);

            yield return new WaitForSeconds(5f);
            ResyncToClient(clientId);
        }

        private static void ResyncToClient(ulong clientId)
        {
            try
            {
                var nm = NetworkManager.Singleton;
                if (nm == null || !nm.IsServer) return;
                if (!nm.ConnectedClients.ContainsKey(clientId)) return;

                var tod = TimeOfDay.Instance;
                if (tod != null)
                {
                    NetworkSync.SyncDeadlineToClient(tod.daysUntilDeadline, clientId);
                    NetworkSync.SyncQuotaToClient(tod.profitQuota, clientId);
                    NetworkSync.SyncRolloverToClient(tod.quotaFulfilled, clientId);
                }

                var sor = StartOfRound.Instance;
                if (sor != null)
                    NetworkSync.SyncBuyingRateToClient(sor.companyBuyingRate, isJackpot: false, clientId, silent: true);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Latejoin resync to client {clientId} failed: {e.Message}");
            }
        }
    }
}
