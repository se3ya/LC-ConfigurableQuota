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
        private static LNetworkMessage<int>? _syncRolloverMessage;
        private static LNetworkMessage<SyncScrapLossSummary>? _syncScrapLossSummaryMessage;

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

                _syncRolloverMessage = LNetworkMessage<int>.Connect(
                    "ConfigurableQuota_SyncRollover",
                    onClientReceived: OnRolloverReceived
                );

                _syncScrapLossSummaryMessage = LNetworkMessage<SyncScrapLossSummary>.Connect(
                    "ConfigurableQuota_SyncScrapLossSummary",
                    onClientReceived: OnScrapLossSummaryReceived
                );

                Plugin.Log.LogInfo("Network sync is ready.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not initialize network sync: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not sync credits: {ex.Message}");
            }
        }

        private static void OnCreditsReceived(int credits)
        {
            try
            {
                var term = UnityEngine.Object.FindObjectOfType<Terminal>();
                if (term != null) term.groupCredits = credits;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not apply synced credits on client: {ex.Message}");
            }
        }

        #endregion

        #region Scrap Loss Summary Sync

        public static void SyncScrapLossSummaryToClients(int beforeValue, int afterValue)
        {
            try
            {
                if (_syncScrapLossSummaryMessage == null)
                {
                    Plugin.Log.LogWarning("Scrap loss summary message not initialized");
                    return;
                }

                _syncScrapLossSummaryMessage.SendClients(new SyncScrapLossSummary(beforeValue, afterValue));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not sync scrap loss summary: {ex.Message}");
            }
        }

        private static void OnScrapLossSummaryReceived(SyncScrapLossSummary data)
        {
            try
            {
                if (data.BeforeValue > 0)
                {
                    PenaltiesOnLandingPatch.CacheScrapLossSummary(data.BeforeValue, data.AfterValue);
                }
                else
                {
                    PenaltiesOnLandingPatch.ClearScrapLossSummary();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not apply synced scrap loss summary on client: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not sync quota: {ex.Message}");
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
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not apply synced quota on client: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not sync value loss: {ex.Message}");
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
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not apply synced value loss on client: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not sync deadline: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not apply synced deadline on client: {ex.Message}");
            }
        }

        #endregion

        #region Rollover Sync

        public static void SyncRolloverToClients(int rollover)
        {
            try
            {
                if (_syncRolloverMessage == null)
                {
                    Plugin.Log.LogWarning("Rollover message not initialized");
                    return;
                }

                _syncRolloverMessage.SendClients(rollover);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not sync rollover: {ex.Message}");
            }
        }

        private static void OnRolloverReceived(int rollover)
        {
            try
            {
                var tod = TimeOfDay.Instance;
                if (tod != null)
                {
                    tod.quotaFulfilled = rollover;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Could not apply synced rollover on client: {ex.Message}");
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

    [Serializable]
    public struct SyncScrapLossSummary
    {
        public int BeforeValue;
        public int AfterValue;

        public SyncScrapLossSummary(int beforeValue, int afterValue)
        {
            BeforeValue = beforeValue;
            AfterValue = afterValue;
        }
    }
}