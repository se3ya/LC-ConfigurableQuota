using System;
using LethalNetworkAPI;
using UnityEngine;

namespace ConfigurableQuota.Patches
{
    /// <summary>
    /// Handles network synchronization of credits and quota penalties between host and clients.
    /// </summary>
    internal static class NetworkSync
    {
        private static LNetworkMessage<int>? _syncCreditsMessage;
        private static LNetworkMessage<int>? _syncQuotaMessage;
        private static LNetworkMessage<SyncValueLossData>? _syncValueLossMessage;

        public static void Initialize()
        {
            try
            {
                // Credits sync: Host -> All Clients
                _syncCreditsMessage = LNetworkMessage<int>.Connect(
                    "ConfigurableQuota_SyncCredits",
                    onClientReceived: OnCreditsReceived
                );

                // Quota sync: Host -> All Clients
                _syncQuotaMessage = LNetworkMessage<int>.Connect(
                    "ConfigurableQuota_SyncQuota",
                    onClientReceived: OnQuotaReceived
                );

                // Value loss sync: Host -> All Clients
                _syncValueLossMessage = LNetworkMessage<SyncValueLossData>.Connect(
                    "ConfigurableQuota_SyncValueLoss",
                    onClientReceived: OnValueLossReceived
                );

                Plugin.Log.LogInfo("[NetworkSync] Network messages initialized");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[NetworkSync] Failed to initialize: {ex.Message}");
            }
        }

        #region Credits Sync

        public static void SyncCreditsToClients(int credits)
        {
            try
            {
                if (_syncCreditsMessage == null)
                {
                    Plugin.Log.LogWarning("[NetworkSync] Credits message not initialized");
                    return;
                }

                _syncCreditsMessage.SendClients(credits);
                Plugin.Log.LogInfo($"[NetworkSync] Sent credits sync to clients: {credits}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[NetworkSync] Failed to sync credits: {ex.Message}");
            }
        }

        private static void OnCreditsReceived(int credits)
        {
            try
            {
                var sor = StartOfRound.Instance;
                if (sor == null) return;

                // Update StartOfRound credits
                var field = typeof(StartOfRound).GetField("groupCredits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                           ?? typeof(StartOfRound).GetField("companyCredits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field?.FieldType == typeof(int))
                {
                    field.SetValue(sor, credits);
                }

                // Update Terminal credits
                var term = UnityEngine.Object.FindObjectOfType<Terminal>();
                if (term != null)
                {
                    var tfield = typeof(Terminal).GetField("groupCredits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                                  ?? typeof(Terminal).GetField("companyCredits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (tfield?.FieldType == typeof(int))
                    {
                        tfield.SetValue(term, credits);
                    }
                }

                Plugin.Log.LogInfo($"[NetworkSync] Client received credits sync: {credits}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[NetworkSync] Failed to apply credits on client: {ex.Message}");
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
                    Plugin.Log.LogWarning("[NetworkSync] Quota message not initialized");
                    return;
                }

                _syncQuotaMessage.SendClients(quota);
                Plugin.Log.LogInfo($"[NetworkSync] Sent quota sync to clients: {quota}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[NetworkSync] Failed to sync quota: {ex.Message}");
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
                    Plugin.Log.LogInfo($"[NetworkSync] Client received quota sync: {quota}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[NetworkSync] Failed to apply quota on client: {ex.Message}");
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
                    Plugin.Log.LogWarning("[NetworkSync] Value loss message not initialized");
                    return;
                }

                // Send each item individually to avoid large packet issues
                foreach (var item in items)
                {
                    _syncValueLossMessage.SendClients(item);
                }

                Plugin.Log.LogInfo($"[NetworkSync] Sent {items.Length} value loss updates to clients");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[NetworkSync] Failed to sync value loss: {ex.Message}");
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
                        if (g != null && g.GetInstanceID() == data.InstanceId && g.itemProperties?.isScrap == true)
                        {
                            g.scrapValue = data.NewValue;
                            try { g.SetScrapValue(data.NewValue); } catch { }
                            Plugin.Log.LogInfo($"[NetworkSync] Client updated scrap value for {g.itemProperties.itemName}: {data.NewValue}");
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[NetworkSync] Failed to apply value loss on client: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Data structure for syncing individual scrap value losses.
    /// </summary>
    [Serializable]
    public struct SyncValueLossData
    {
        public int InstanceId;
        public int NewValue;

        public SyncValueLossData(int instanceId, int newValue)
        {
            InstanceId = instanceId;
            NewValue = newValue;
        }
    }
}
