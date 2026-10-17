using System;
using HarmonyLib;

namespace ConfigurableQuota.Patches;

internal static class QuotaSaveData
{
    private const string SoldThisQuotaKey = "ConfigurableQuota_SoldThisQuota";
    private const string LastAppliedRolloverKey = "ConfigurableQuota_LastAppliedRollover";

    private static bool TryGetSaveFile(out string saveFileName)
    {
        saveFileName = string.Empty;

        string current = GameNetworkManager.Instance.currentSaveFileName;
        if (string.IsNullOrEmpty(current)) return false;

        saveFileName = current;
        return true;
    }

    private static bool VanillaPersistsQuotaThisPass()
    {
        var sor = StartOfRound.Instance;
        if (!GameNetworkManager.Instance.isHostingGame || sor.isChallengeFile) return false;
        if (!sor.inShipPhase || sor.beganLoadingNewLevel) return false;

        return !RoundManager.Instance.dungeonIsGenerating;
    }

    internal static void Save()
    {
        try
        {
            if (!VanillaPersistsQuotaThisPass()) return;
            if (!TryGetSaveFile(out string saveFileName)) return;

            ES3.Save(SoldThisQuotaKey, DepositItemsDeskPatches.SoldThisQuota, saveFileName);
            ES3.Save(LastAppliedRolloverKey, TimeOfDayQuotaPatch.LastAppliedRollover, saveFileName);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Could not save quota progress: {e.Message}");
        }
    }

    internal static void Load()
    {
        try
        {
            if (!TryGetSaveFile(out string saveFileName)) return;

            DepositItemsDeskPatches.SoldThisQuota = ES3.Load(SoldThisQuotaKey, saveFileName, 0);

            if (ES3.KeyExists(LastAppliedRolloverKey, saveFileName))
            {
                TimeOfDayQuotaPatch.LastAppliedRollover = ES3.Load(LastAppliedRolloverKey, saveFileName, 0);
                return;
            }

            TimeOfDayQuotaPatch.LastAppliedRollover = Math.Max(0, TimeOfDay.Instance.quotaFulfilled);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Could not load quota progress: {e.Message}");
        }
    }

    internal static void Clear()
    {
        DepositItemsDeskPatches.SoldThisQuota = 0;
        TimeOfDayQuotaPatch.LastAppliedRollover = 0;

        try
        {
            if (!TryGetSaveFile(out string saveFileName)) return;

            ES3.Save(SoldThisQuotaKey, 0, saveFileName);
            ES3.Save(LastAppliedRolloverKey, 0, saveFileName);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Could not clear saved quota progress: {e.Message}");
        }
    }
}

[HarmonyPatch(typeof(GameNetworkManager), "SaveGameValues")]
internal static class GameNetworkManagerSaveValuesPatch
{
    [HarmonyPostfix]
    private static void SaveGameValues_Postfix() => QuotaSaveData.Save();
}

[HarmonyPatch(typeof(StartOfRound), "SetTimeAndPlanetToSavedSettings")]
internal static class StartOfRoundSavedSettingsPatch
{
    [HarmonyPostfix]
    private static void SetTimeAndPlanetToSavedSettings_Postfix()
    {
        QuotaSaveData.Load();
        AlignDeadlineLength();
    }

    private static void AlignDeadlineLength()
    {
        try
        {
            var tod = TimeOfDay.Instance;
            int remaining = tod.daysUntilDeadline;
            if (remaining <= tod.quotaVariables.deadlineDaysAmount) return;

            Plugin.Log.LogInfo($"Deadline length was {tod.quotaVariables.deadlineDaysAmount} days but {remaining} are left on the clock, using {remaining} so the buy rate stays sane.");
            tod.quotaVariables.deadlineDaysAmount = remaining;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Could not align the deadline length after loading: {e.Message}");
        }
    }
}
