using System;
using LethalNetworkAPI;
using UnityEngine;

namespace ConfigurableQuota.Patches
{
    internal static class NetworkSync
    {
        private static LNetworkMessage<int>? _syncCreditsMessage;
        private static LNetworkMessage<int>? _syncQuotaMessage;
        private static LNetworkMessage<SyncValueLossData>? _syncValueLossMessage;
        private static LNetworkMessage<int>? _syncDeadlineMessage;

        public static void Initialize()
        {
            try
            {
                _syncCreditsMessage = LNetworkMessage<int>.Connect(
                    "ConfigurableQuota_SyncCredits",
                    onClientReceived: OnCreditsReceived
                );

                _syncQuotaMessage = LNetworkMessage<int>.Connect(
                    "ConfigurableQuota_SyncQuota",
                    onClientReceived: OnQuotaReceived
                );

                _syncValueLossMessage = LNetworkMessage<SyncValueLossData>.Connect(
                    "ConfigurableQuota_SyncValueLoss",
                    onClientReceived: OnValueLossReceived
                );

                _syncDeadlineMessage = LNetworkMessage<int>.Connect(
                    "ConfigurableQuota_SyncDeadline",
                    onClientReceived: OnDeadlineReceived
                );

                Plugin.Log.LogInfo("Network initialized");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to initialize: {ex.Message}");
            }
        }

        #region Credits Sync

        public static void SyncCreditsToClients(int credits)
        {
            try
            {
                if (_syncCreditsMessage == null)
                {
                    Plugin.Log.LogWarning("Credits message not initialized");
                    return;
                }

                _syncCreditsMessage.SendClients(credits);
                Plugin.Log.LogDebug($"Sent credits sync to clients: {credits}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to sync credits: {ex.Message}");
            }
        }

        private static void OnCreditsReceived(int credits)
        {
            try
            {
                var term = UnityEngine.Object.FindObjectOfType<Terminal>();
                if (term != null) term.groupCredits = credits;

                Plugin.Log.LogDebug($"Client received credits sync: {credits}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to apply credits on client: {ex.Message}");
            }
        }

        #endregion

        #region Quota Sync

        public static void SyncQuotaToClients(int quota)
        {
            try
            {
                if (_syncQuotaMessage == null)
                {
                    Plugin.Log.LogWarning("Quota message not initialized");
                    return;
                }

                _syncQuotaMessage.SendClients(quota);
                Plugin.Log.LogDebug($"Sent quota sync to clients: {quota}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to sync quota: {ex.Message}");
            }
        }

        private static void OnQuotaReceived(int quota)
        {
            try
            {
                var tod = TimeOfDay.Instance;
                if (tod != null)
                {
                    tod.profitQuota = quota;
                    Plugin.Log.LogDebug($"Client received quota sync: {quota}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to apply quota on client: {ex.Message}");
            }
        }

        #endregion

        #region Value Loss Sync

        public static void SyncValueLossToClients(SyncValueLossData[] items)
        {
            try
            {
                if (_syncValueLossMessage == null)
                {
                    Plugin.Log.LogWarning("Value loss message not initialized");
                    return;
                }

                foreach (var item in items)
                {
                    _syncValueLossMessage.SendClients(item);
                }

                Plugin.Log.LogDebug($"Sent {items.Length} value loss updates to clients");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to sync value loss: {ex.Message}");
            }
        }

        private static void OnValueLossReceived(SyncValueLossData data)
        {
            try
            {
                var allGrab = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                foreach (var g in allGrab)
                {
                    try
                    {
                        var netObj = g.GetComponent<Unity.Netcode.NetworkObject>();
                        if (g != null && netObj != null && netObj.NetworkObjectId == data.NetworkObjectId && g.itemProperties?.isScrap == true)
                        {
                            g.scrapValue = data.NewValue;
                            try { g.SetScrapValue(data.NewValue); } catch { }
                            Plugin.Log.LogDebug($"Client updated scrap value for {g.itemProperties.itemName}: {data.NewValue}");
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to apply value loss on client: {ex.Message}");
            }
        }

        #endregion

        #region Deadline Sync

        public static void SyncDeadlineToClients(int days)
        {
            try
            {
                if (_syncDeadlineMessage == null)
                {
                    Plugin.Log.LogWarning("Deadline message not initialized");
                    return;
                }

                _syncDeadlineMessage.SendClients(days);
                Plugin.Log.LogDebug($"Sent deadline sync to clients: {days} days");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to sync deadline: {ex.Message}");
            }
        }

        private static void OnDeadlineReceived(int days)
        {
            try
            {
                var tod = TimeOfDay.Instance;
                if (tod == null) return;

                if (tod.quotaVariables != null)
                    tod.quotaVariables.deadlineDaysAmount = days;

                tod.daysUntilDeadline = days;
                tod.timeUntilDeadline = days * tod.totalTime;

                Plugin.Log.LogDebug($"Client updated deadline: {days} days");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to apply deadline on client: {ex.Message}");
            }
        }

        #endregion
    }

    [Serializable]
    public struct SyncValueLossData
    {
        public ulong NetworkObjectId;
        public int NewValue;

        public SyncValueLossData(ulong networkObjectId, int newValue)
        {
            NetworkObjectId = networkObjectId;
            NewValue = newValue;
        }
    }
}