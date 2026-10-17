using System;
using System.Collections;
using LethalNetworkAPI;
using UnityEngine;

namespace ConfigurableQuota.Patches;

internal static class NetworkSync
{
    private static LNetworkMessage<SyncQuotaData>? _syncQuotaMessage;
    private static LNetworkMessage<SyncValueLossData>? _syncValueLossMessage;
    private static LNetworkMessage<int>? _syncDeadlineMessage;
    private static LNetworkMessage<int>? _syncRolloverMessage;
    private static LNetworkMessage<RolloverStatusData>? _rolloverStatusMessage;
    private static LNetworkMessage<SyncScrapLossSummary>? _syncScrapLossSummaryMessage;
    private static LNetworkMessage<SyncBuyingRateData>? _syncBuyingRateMessage;
    private static LNetworkMessage<SyncPenaltySummary>? _syncPenaltySummaryMessage;
    private static LNetworkMessage<float>? _syncInteriorScaleMessage;
    private static LNetworkMessage<int>? _syncDeadlineLengthMessage;

    public static void Initialize()
    {
        try
        {
            _syncQuotaMessage = LNetworkMessage<SyncQuotaData>.Connect(
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

            _rolloverStatusMessage = LNetworkMessage<RolloverStatusData>.Connect(
                "ConfigurableQuota_RolloverStatus",
                onClientReceived: OnRolloverStatusReceived
            );

            _syncScrapLossSummaryMessage = LNetworkMessage<SyncScrapLossSummary>.Connect(
                "ConfigurableQuota_SyncScrapLossSummary",
                onClientReceived: OnScrapLossSummaryReceived
            );

            _syncBuyingRateMessage = LNetworkMessage<SyncBuyingRateData>.Connect(
                "ConfigurableQuota_SyncBuyingRate",
                onClientReceived: OnBuyingRateReceived
            );

            _syncPenaltySummaryMessage = LNetworkMessage<SyncPenaltySummary>.Connect(
                "ConfigurableQuota_SyncPenaltySummary",
                onClientReceived: OnPenaltySummaryReceived
            );

            _syncInteriorScaleMessage = LNetworkMessage<float>.Connect(
                "ConfigurableQuota_SyncInteriorScale",
                onClientReceived: OnInteriorScaleReceived
            );

            _syncDeadlineLengthMessage = LNetworkMessage<int>.Connect(
                "ConfigurableQuota_SyncDeadlineLength",
                onClientReceived: OnDeadlineLengthReceived
            );

            Plugin.Log.LogDebug("Network messages registered.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not initialize network sync: {ex.Message}");
        }
    }

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
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

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

            PenaltiesOnLandingPatch.HasAllDeadSnapshot = true;
            HudQuotaAnimationPatch.TryApplyAdvancedFeaturesEndscreen();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not apply synced scrap loss summary on client: {ex.Message}");
        }
    }

    #endregion

    #region Penalty Summary Sync

    public static void SyncPenaltySummaryToClients(SyncPenaltySummary summary)
    {
        try
        {
            if (_syncPenaltySummaryMessage == null)
            {
                Plugin.Log.LogWarning("Penalty summary message not initialized");
                return;
            }

            _syncPenaltySummaryMessage.SendClients(summary);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync penalty summary: {ex.Message}");
        }
    }

    private static void OnPenaltySummaryReceived(SyncPenaltySummary summary)
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        PenaltiesOnLandingPatch.Summary = summary;
        PenaltiesOnLandingPatch.HasPenaltyCache = true;
    }

    #endregion

    #region Interior Scale Sync

    public static void SyncInteriorScaleToClients(float scale)
    {
        try
        {
            if (_syncInteriorScaleMessage == null)
            {
                Plugin.Log.LogWarning("Interior scale message not initialized");
                return;
            }

            _syncInteriorScaleMessage.SendClients(scale);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync interior scale: {ex.Message}");
        }
    }

    private static void OnInteriorScaleReceived(float scale)
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        DynamicLevelPatches.InteriorScale = scale;
    }

    #endregion

    #region Quota Sync

    public static void SyncQuotaToClients(int quota, int penaltyDelta = 0)
    {
        try
        {
            if (_syncQuotaMessage == null)
            {
                Plugin.Log.LogWarning("Quota message not initialized");
                return;
            }

            _syncQuotaMessage.SendClients(new SyncQuotaData(quota, penaltyDelta));
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync quota: {ex.Message}");
        }
    }

    public static void SyncQuotaToClient(int quota, ulong clientId)
    {
        try
        {
            if (_syncQuotaMessage == null)
            {
                Plugin.Log.LogWarning("Quota message not initialized");
                return;
            }

            _syncQuotaMessage.SendClient(new SyncQuotaData(quota, 0), clientId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync quota to client {clientId}: {ex.Message}");
        }
    }

    private static void OnQuotaReceived(SyncQuotaData data)
    {
        try
        {
            var tod = TimeOfDay.Instance;
            if (tod != null)
            {
                tod.profitQuota = data.Quota;
            }
            PenaltiesOnLandingPatch.CachedQuotaPenaltyDelta = data.PenaltyDelta;
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
            var spawnManager = Unity.Netcode.NetworkManager.Singleton.SpawnManager;
            if (!spawnManager.SpawnedObjects.TryGetValue(data.NetworkObjectId, out var netObj) || netObj == null)
                return;

            var item = netObj.GetComponent<GrabbableObject>();
            if (item == null || item.itemProperties?.isScrap != true) return;

            item.scrapValue = data.NewValue;
            try { item.SetScrapValue(data.NewValue); } catch { }
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
            ApplyDeadline(days);

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
            if (!ApplyDeadline(days) || Unity.Netcode.NetworkManager.Singleton.IsServer) return;

            TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
            TimeOfDayQuotaPatch.RefreshExternalMonitors();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not apply synced deadline on client: {ex.Message}");
        }
    }

    public static void SyncDeadlineLengthToClient(int days, ulong clientId)
    {
        try
        {
            if (_syncDeadlineLengthMessage == null)
            {
                Plugin.Log.LogWarning("Deadline length message not initialized");
                return;
            }

            _syncDeadlineLengthMessage.SendClient(days, clientId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync deadline length to client {clientId}: {ex.Message}");
        }
    }

    private static void OnDeadlineLengthReceived(int days)
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer) return;

        TimeOfDay.Instance.quotaVariables.deadlineDaysAmount = Mathf.Max(1, days);
    }

    private static bool ApplyDeadline(int days)
    {
        var tod = TimeOfDay.Instance;
        if (tod == null) return false;

        int clampedDays = Mathf.Max(1, days);

        float dayDuration = tod.totalTime;
        if (dayDuration <= 0f && tod.daysUntilDeadline > 0 && tod.timeUntilDeadline > 0f)
            dayDuration = tod.timeUntilDeadline / tod.daysUntilDeadline;

        if (tod.quotaVariables != null)
            tod.quotaVariables.deadlineDaysAmount = clampedDays;

        tod.daysUntilDeadline = clampedDays;
        if (dayDuration > 0f)
            tod.timeUntilDeadline = clampedDays * dayDuration;

        return true;
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

    public static void SyncRolloverToClient(int rollover, ulong clientId)
    {
        try
        {
            if (_syncRolloverMessage == null)
            {
                Plugin.Log.LogWarning("Rollover message not initialized");
                return;
            }

            _syncRolloverMessage.SendClient(rollover, clientId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync rollover to client {clientId}: {ex.Message}");
        }
    }

    private static void OnRolloverReceived(int rollover)
    {
        try
        {
            var tod = TimeOfDay.Instance;
            if (tod != null)
                tod.quotaFulfilled = Math.Max(0, rollover);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not apply synced rollover on client: {ex.Message}");
        }
    }

    public static void ReportRolloverStatus(int bank, int quota, int deadline)
    {
        ShowRolloverStatus(bank, quota, deadline);

        try
        {
            if (_rolloverStatusMessage == null)
            {
                Plugin.Log.LogWarning("Rollover status message not initialized");
                return;
            }

            _rolloverStatusMessage.SendClients(new RolloverStatusData(bank, quota, deadline));
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not send the rollover status: {ex.Message}");
        }
    }

    private static void OnRolloverStatusReceived(RolloverStatusData data)
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        ShowRolloverStatus(data.Bank, data.Quota, data.Deadline);
    }

    private static void ShowRolloverStatus(int bank, int quota, int deadline)
    {
        try
        {
            if (!ConfigManager.RolloverAlertEnabled.Value) return;

            var hud = HUDManager.Instance;
            hud.StartCoroutine(RolloverStatusRoutine(hud, bank, quota, deadline));
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not show the rollover status: {ex.Message}");
        }
    }

    private static IEnumerator RolloverStatusRoutine(HUDManager hud, int bank, int quota, int deadline)
    {
        yield return new WaitForSecondsRealtime(1f);

        float waited = 0f;
        while (hud.displayingNewQuota && waited < 60f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.5f);

        string days = deadline == 1 ? "1 day" : $"{deadline} days";

        hud.DisplayTip(
            "<color=#ffc526>ROLLOVER SPENT</color>",
            $"<color=#fcbf17>\n* Rollover only covers ${bank} of ${quota}, sell ${quota - bank} within {days}</color>",
            true,
            false,
            "LC_RolloverTip"
        );
    }

    #endregion

    #region Buying Rate Sync

    public static void SyncBuyingRateToClients(float rate, bool isJackpot, bool silent = false)
    {
        try
        {
            if (_syncBuyingRateMessage == null)
            {
                Plugin.Log.LogWarning("Buying rate message not initialized");
                return;
            }

            _syncBuyingRateMessage.SendClients(new SyncBuyingRateData(rate, isJackpot, silent));
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync buying rate: {ex.Message}");
        }
    }

    public static void SyncBuyingRateToClient(float rate, bool isJackpot, ulong clientId, bool silent = false)
    {
        try
        {
            if (_syncBuyingRateMessage == null)
            {
                Plugin.Log.LogWarning("Buying rate message not initialized");
                return;
            }

            _syncBuyingRateMessage.SendClient(new SyncBuyingRateData(rate, isJackpot, silent), clientId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync buying rate to client {clientId}: {ex.Message}");
        }
    }

    private static void OnBuyingRateReceived(SyncBuyingRateData data)
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        try
        {
            BuyingRatePatch.ApplyReceivedBuyingRate(data.Rate, data.IsJackpot, data.Silent);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not apply synced buying rate on client: {ex.Message}");
        }
    }

    #endregion
}

[Serializable]
public struct SyncQuotaData
{
    public int Quota;
    public int PenaltyDelta;

    public SyncQuotaData(int quota, int penaltyDelta)
    {
        Quota = quota;
        PenaltyDelta = penaltyDelta;
    }
}

[Serializable]
public struct RolloverStatusData
{
    public int Bank;
    public int Quota;
    public int Deadline;

    public RolloverStatusData(int bank, int quota, int deadline)
    {
        Bank = bank;
        Quota = quota;
        Deadline = deadline;
    }
}

[Serializable]
public struct SyncBuyingRateData
{
    public float Rate;
    public bool IsJackpot;
    public bool Silent;

    public SyncBuyingRateData(float rate, bool isJackpot, bool silent = false)
    {
        Rate = rate;
        IsJackpot = isJackpot;
        Silent = silent;
    }
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
public struct SyncPenaltySummary
{
    public int Dead;
    public int Total;
    public int Recovered;
    public bool CreditApplied;
    public float CreditPercent;
    public int CreditLoss;
    public float QuotaPercent;
    public int QuotaDelta;

    public SyncPenaltySummary(int dead, int total, int recovered, bool creditApplied, float creditPercent, int creditLoss, float quotaPercent, int quotaDelta)
    {
        Dead = dead;
        Total = total;
        Recovered = recovered;
        CreditApplied = creditApplied;
        CreditPercent = creditPercent;
        CreditLoss = creditLoss;
        QuotaPercent = quotaPercent;
        QuotaDelta = quotaDelta;
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
