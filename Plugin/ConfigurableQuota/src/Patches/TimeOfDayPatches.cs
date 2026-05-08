using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using BepInEx.Bootstrap;
using ConfigurableQuota.Compat;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(TimeOfDay))]
    internal static class TimeOfDayQuotaPatch
    {
        private static bool _initialDeadlineApplied;
        private static bool _initialDeadlineWasConstellationSpecific;

        private readonly struct DeadlineSelection
        {
            internal DeadlineSelection(
                int days,
                bool isRandomized,
                bool fromConstellation,
                ConstellationDeadlineMode constellationMode,
                string constellationName,
                string source)
            {
                Days = days;
                IsRandomized = isRandomized;
                FromConstellation = fromConstellation;
                ConstellationMode = constellationMode;
                ConstellationName = constellationName;
                Source = source;
            }

            internal int Days { get; }
            internal bool IsRandomized { get; }
            internal bool FromConstellation { get; }
            internal ConstellationDeadlineMode ConstellationMode { get; }
            internal string ConstellationName { get; }
            internal string Source { get; }
        }

        [HarmonyPatch(nameof(TimeOfDay.SetNewProfitQuota))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        [HarmonyAfter(new[] { Metadata.LETHAL_MOON_UNLOCKS_GUID })]
        private static bool SetNewProfitQuota_Prefix(TimeOfDay __instance,
            ref int ___timesFulfilledQuota,
            ref int ___profitQuota,
            ref float ___timeUntilDeadline,
            ref int ___quotaFulfilled,
            ref int ___daysUntilDeadline,
            ref float ___totalTime)
        {
            try
            {
                if (!__instance.IsServer) return false;

                if (ConfigManager.DisableQuota.Value)
                {
                    ___profitQuota = Mathf.Max(0, ConfigManager.StartingQuota.Value);
                    SetDeadlineTimer(___totalTime, ref ___daysUntilDeadline, ref ___timeUntilDeadline);
                    return false;
                }

                int previousQuota = ___profitQuota;
                ___timesFulfilledQuota++;

                int newQuota = CalculateNewQuota(previousQuota, ___timesFulfilledQuota);

                int daysLeftAtFulfill = ___daysUntilDeadline;
                int overage = ___quotaFulfilled - previousQuota;
                int overtimeBonus = (overage / 5) + (15 * daysLeftAtFulfill);

                ___profitQuota = newQuota;
                int rollover = CalculateRollover(overage);

                int deadline = SetDeadlineTimer(
                    ___totalTime,
                    ref ___daysUntilDeadline,
                    ref ___timeUntilDeadline,
                    prevDays: daysLeftAtFulfill,
                    logSelection: true
                );

                __instance.quotaVariables.deadlineDaysAmount = deadline;

                __instance.SyncNewProfitQuotaClientRpc(___profitQuota, overtimeBonus, ___timesFulfilledQuota);

                ___quotaFulfilled = rollover;
                ___daysUntilDeadline = deadline;
                ___timeUntilDeadline = ___totalTime * deadline;

                NetworkSync.SyncDeadlineToClients(deadline);
                if (rollover > 0)
                    NetworkSync.SyncRolloverToClients(rollover);

                Plugin.Log.LogInfo(
                    $"Quota {___timesFulfilledQuota}: {previousQuota} -> {newQuota}, deadline {deadline} days, rollover {rollover}, overtime {overtimeBonus} credits.");

                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Could not calculate the next quota: {e.Message}");
                return true;
            }
        }

        private static int CalculateNewQuota(int previousQuota, int timesFulfilled)
        {
            int newQuota;
            int finalLevel = ConfigManager.FinalLevel.Value;

            if (finalLevel != -1 && previousQuota >= finalLevel)
            {
                newQuota = previousQuota + Math.Max(0, ConfigManager.FinalIncrease.Value);
            }
            else
            {
                float sharp = Mathf.Max(0.1f, ConfigManager.CurveSharpness.Value);
                float t = timesFulfilled;
                float timeMult = Mathf.Clamp(1f + (t * (t / sharp)), 0f, 10000f);

                float randFactor = 1f;
                float randMult = ConfigManager.RandomizerMultiplier.Value;
                if (randMult > 0f)
                {
                    randFactor = 1f + (UnityEngine.Random.Range(-0.5f, 0.5f) * randMult);
                }

                float increase = ConfigManager.BaseIncrease.Value * timeMult * randFactor;

                if (ConfigManager.EnablePlayerMultiplier.Value)
                {
                    increase *= CalculatePlayerMultiplier();
                }

                if (ConfigManager.EnableGrowthDampening.Value)
                {
                    int ceiling = ConfigManager.DampeningStartAt.Value;
                    if (timesFulfilled > ceiling)
                    {
                        float excess = timesFulfilled - ceiling;
                        float scale = Mathf.Max(0.1f, ConfigManager.DampeningSharpness.Value);
                        increase /= 1f + Mathf.Pow(excess / scale, 2f);
                    }
                }

                newQuota = Mathf.RoundToInt(Mathf.Clamp(previousQuota + increase, 0f, 1E+09f));
            }

            int cap = ConfigManager.QuotaCap.Value;
            return cap != -1 ? Mathf.Min(newQuota, cap) : newQuota;
        }

        private static float CalculatePlayerMultiplier()
        {
            var netManager = NetworkManager.Singleton;
            if (netManager == null || !netManager.IsServer) return 1f;

            int playerCount = Mathf.Max(1, netManager.ConnectedClientsList?.Count ?? 1);
            int threshold = ConfigManager.PlayerThreshold.Value;
            int extraPlayers = playerCount - threshold;

            if (extraPlayers <= 0) return 1f;

            int cap = ConfigManager.PlayerCap.Value;
            int maxExtra = Mathf.Max(0, cap - threshold);
            extraPlayers = Mathf.Clamp(extraPlayers, 0, maxExtra);

            return 1f + (extraPlayers * Mathf.Max(0f, ConfigManager.MultPerPlayer.Value));
        }

        private static int CalculateRollover(int overage)
        {
            float rolloverAmt = ConfigManager.RolloverAmount.Value;
            if (rolloverAmt <= 0f || overage <= 0) return 0;
            return Mathf.RoundToInt(overage * Mathf.Clamp01(rolloverAmt));
        }

        private static int SetDeadlineTimer(float dayDuration, ref int days, ref float timeUntilDeadline, int prevDays = -1, bool logSelection = false)
        {
            DeadlineSelection selection = ResolveDeadlineSelection(prevDays);
            int d = selection.Days;
            days = d;
            timeUntilDeadline = d * dayDuration;

            if (logSelection)
                Plugin.Log.LogInfo($"Deadline selected ({selection.Source}): {d} day(s).");

            return d;
        }

        [HarmonyPatch(nameof(TimeOfDay.Awake))]
        [HarmonyPostfix]
        private static void TimeOfDay_Awake_Postfix(TimeOfDay __instance)
        {
            ApplyQuotaVariables(__instance);
        }

        [HarmonyPatch(nameof(TimeOfDay.Start))]
        [HarmonyPostfix]
        private static void TimeOfDay_Start_Postfix(TimeOfDay __instance)
        {
            ConstellationDeadlineConfig.RefreshSections();

            if (__instance.timesFulfilledQuota == 0)
            {
                TryApplyInitialDeadlineFromCurrentMode(__instance, allowConstellationOverride: true, logSelection: true);

                if (__instance.IsServer)
                {
                    NetworkSync.SyncDeadlineToClients(__instance.daysUntilDeadline);
                    Plugin.Log.LogInfo($"Initial deadline synced: {__instance.daysUntilDeadline} days.");
                }

                string deadlineDesc = DescribeCurrentDeadlineMode(__instance.daysUntilDeadline);
                string quotaCap = ConfigManager.QuotaCap.Value != -1 ? $", cap={ConfigManager.QuotaCap.Value}" : "";
                string finalLevel = ConfigManager.FinalLevel.Value != -1
                    ? $", finalLevel={ConfigManager.FinalLevel.Value} (+{ConfigManager.FinalIncrease.Value} flat)"
                    : "";
                string creditPenalty = ConfigManager.CreditPenaltiesEnabled.Value
                    ? $"credits={ConfigManager.CreditPenaltyPercentPerPlayer.Value:P0}/player (cap {ConfigManager.CreditPenaltyPercentCap.Value:P0})"
                    : "credits=off";
                string quotaPenalty = ConfigManager.QuotaPenaltiesEnabled.Value
                    ? $"quota={ConfigManager.QuotaPenaltyPercentPerPlayer.Value:P0}/player (cap {ConfigManager.QuotaPenaltyPercentCap.Value:P0})"
                    : "quota=off";
                string losses =
                    $"scrap={ConfigManager.ScrapLossEnabled.Value}" +
                    $", value={ConfigManager.ValueLossEnabled.Value}({ConfigManager.ValueLossPercent.Value:P0})" +
                    $", equip={ConfigManager.EquipmentLossEnabled.Value}";

                Plugin.Log.LogInfo(
                    $"Settings loaded: quota start {ConfigManager.StartingQuota.Value}, base +{ConfigManager.BaseIncrease.Value}/cycle, sharpness {ConfigManager.CurveSharpness.Value}{quotaCap}{finalLevel}; deadline {deadlineDesc}; credits start {ConfigManager.StartingCredits.Value}; penalties [{creditPenalty}, {quotaPenalty}]; losses [{losses}].");
            }

            if (Chainloader.PluginInfos.ContainsKey("ShaosilGaming.GeneralImprovements"))
                __instance.StartCoroutine(GIMonitorRefreshRoutine());
        }

        private static System.Collections.IEnumerator GIMonitorRefreshRoutine()
        {
            yield return null;
            RefreshExternalMonitors();
        }

        internal static void RefreshExternalMonitors()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "GeneralImprovements");
                if (asm == null) return;

                var helper = asm.GetType("GeneralImprovements.Utilities.MonitorsHelper");
                helper?.GetMethod("UpdateTotalDaysMonitors")?.Invoke(null, null);
                helper?.GetMethod("UpdateTotalQuotasMonitors")?.Invoke(null, null);
            }
            catch
            {
                // cool
            }
        }

        private static void ApplyQuotaVariables(TimeOfDay instance)
        {
            try
            {
                if (instance.quotaVariables != null)
                {
                    instance.quotaVariables.startingQuota = ConfigManager.StartingQuota.Value;
                    instance.quotaVariables.startingCredits = ConfigManager.StartingCredits.Value;
                }

                ResetInitialDeadlineTracking();
                TryApplyInitialDeadlineFromCurrentMode(instance, allowConstellationOverride: false, logSelection: true);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Could not apply quota settings at startup: {e.Message}");
            }
        }

        internal static bool TryApplyInitialDeadlineFromCurrentMode(
            TimeOfDay instance,
            bool allowConstellationOverride,
            bool logSelection)
        {
            if (instance == null || instance.timesFulfilledQuota != 0)
                return false;

            DeadlineSelection selection = ResolveDeadlineSelection(prevDays: -1);

            bool shouldApply = !_initialDeadlineApplied;
            if (!shouldApply && allowConstellationOverride)
            {
                bool nowConstellationSpecific = selection.FromConstellation
                    && selection.ConstellationMode != ConstellationDeadlineMode.UseGlobal;
                shouldApply = !_initialDeadlineWasConstellationSpecific && nowConstellationSpecific;
            }

            if (!shouldApply)
                return false;

            int deadlineDays = selection.Days;

            if (instance.quotaVariables != null)
                instance.quotaVariables.deadlineDaysAmount = deadlineDays;

            instance.daysUntilDeadline = deadlineDays;
            if (instance.totalTime > 0f)
                instance.timeUntilDeadline = instance.totalTime * deadlineDays;

            _initialDeadlineApplied = true;
            _initialDeadlineWasConstellationSpecific = selection.FromConstellation
                && selection.ConstellationMode != ConstellationDeadlineMode.UseGlobal;

            if (logSelection)
                Plugin.Log.LogInfo($"Initial deadline selected ({selection.Source}): {deadlineDays} day(s).");

            return true;
        }

        private static DeadlineSelection ResolveDeadlineSelection(int prevDays)
        {
            if (ConstellationDeadlineConfig.TryGetCurrentConstellationSettings(
                out string constellationName,
                out ConstellationDeadlineMode mode,
                out int fixedDays,
                out int min,
                out int max))
            {
                if (mode == ConstellationDeadlineMode.Fixed)
                {
                    int d = Math.Max(1, fixedDays);
                    return new DeadlineSelection(
                        d,
                        isRandomized: false,
                        fromConstellation: true,
                        mode,
                        constellationName,
                        $"constellation '{constellationName}' fixed"
                    );
                }

                if (mode == ConstellationDeadlineMode.Random)
                {
                    int d = RollRandomDeadline(min, max, prevDays, out int cMin, out int cMax);
                    return new DeadlineSelection(
                        d,
                        isRandomized: true,
                        fromConstellation: true,
                        mode,
                        constellationName,
                        $"constellation '{constellationName}' random {cMin}-{cMax}"
                    );
                }

                DeadlineSelection globalSelection = ResolveGlobalDeadlineSelection(prevDays);
                return new DeadlineSelection(
                    globalSelection.Days,
                    globalSelection.IsRandomized,
                    fromConstellation: true,
                    mode,
                    constellationName,
                    $"constellation '{constellationName}' use-global ({globalSelection.Source})"
                );
            }

            return ResolveGlobalDeadlineSelection(prevDays);
        }

        private static DeadlineSelection ResolveGlobalDeadlineSelection(int prevDays)
        {
            if (ConfigManager.RandomizeDeadline.Value)
            {
                int d = RollRandomDeadline(
                    ConfigManager.DeadlineMin.Value,
                    ConfigManager.DeadlineMax.Value,
                    prevDays,
                    out int min,
                    out int max
                );

                return new DeadlineSelection(
                    d,
                    isRandomized: true,
                    fromConstellation: false,
                    ConstellationDeadlineMode.UseGlobal,
                    string.Empty,
                    $"global random {min}-{max}"
                );
            }

            int fixedDays = Math.Max(1, ConfigManager.DaysToDeadline.Value);
            return new DeadlineSelection(
                fixedDays,
                isRandomized: false,
                fromConstellation: false,
                ConstellationDeadlineMode.UseGlobal,
                string.Empty,
                "global fixed"
            );
        }

        private static int RollRandomDeadline(int rawMin, int rawMax, int prevDays, out int min, out int max)
        {
            min = Math.Max(1, rawMin);
            max = Math.Max(min, rawMax);

            int d = UnityEngine.Random.Range(min, max + 1);

            if (ConfigManager.DeadlineMustChange.Value && min != max)
            {
                for (int attempt = 0; attempt < 8 && d == prevDays; attempt++)
                    d = UnityEngine.Random.Range(min, max + 1);
            }

            return d;
        }

        private static string DescribeCurrentDeadlineMode(int firstDeadline)
        {
            if (ConstellationDeadlineConfig.TryGetCurrentConstellationSettings(
                out string constellationName,
                out ConstellationDeadlineMode mode,
                out int fixedDays,
                out int min,
                out int max))
            {
                if (mode == ConstellationDeadlineMode.Fixed)
                    return $"constellation '{constellationName}' fixed {Math.Max(1, fixedDays)}d";

                if (mode == ConstellationDeadlineMode.Random)
                    return $"constellation '{constellationName}' randomized {Math.Max(1, min)}-{Math.Max(Math.Max(1, min), max)}d (first: {firstDeadline}d)";

                return $"constellation '{constellationName}' use-global ({DescribeGlobalDeadlineMode(firstDeadline)})";
            }

            return DescribeGlobalDeadlineMode(firstDeadline);
        }

        private static string DescribeGlobalDeadlineMode(int firstDeadline)
        {
            int min = Math.Max(1, ConfigManager.DeadlineMin.Value);
            int max = Math.Max(min, ConfigManager.DeadlineMax.Value);

            return ConfigManager.RandomizeDeadline.Value
                ? $"randomized {min}-{max}d (first: {firstDeadline}d)"
                : $"fixed {Math.Max(1, ConfigManager.DaysToDeadline.Value)}d";
        }

        private static void ResetInitialDeadlineTracking()
        {
            _initialDeadlineApplied = false;
            _initialDeadlineWasConstellationSpecific = false;
        }

        [HarmonyPatch(nameof(TimeOfDay.UpdateProfitQuotaCurrentTime))]
        [HarmonyPostfix]
        private static void UpdateProfitQuotaCurrentTime_Postfix(
            ref float ___timeUntilDeadline,
            ref float ___totalTime,
            ref int ___daysUntilDeadline)
        {
            if (ConfigManager.DisableQuota.Value)
            {
                ApplyDisableQuotaState(ref ___daysUntilDeadline, ref ___totalTime, ref ___timeUntilDeadline);
            }
        }

        [HarmonyPatch(nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        private static void SetBuyingRateForDay_Postfix(
            ref float ___timeUntilDeadline,
            ref float ___totalTime,
            ref int ___daysUntilDeadline)
        {
            if (ConfigManager.DisableQuota.Value)
            {
                ApplyDisableQuotaState(ref ___daysUntilDeadline, ref ___totalTime, ref ___timeUntilDeadline);
            }
        }

        private static void ApplyDisableQuotaState(ref int days, ref float totalTime, ref float timeUntilDeadline)
        {
            try
            {
                SetDeadlineTimer(totalTime, ref days, ref timeUntilDeadline);

                var sor = StartOfRound.Instance;
                if (sor != null)
                {
                    sor.companyBuyingRate = 1f;
                    if (sor.deadlineMonitorText != null)
                        sor.deadlineMonitorText.text = "DEADLINE:\nNEVER";
                    if (sor.profitQuotaMonitorText != null)
                        sor.profitQuotaMonitorText.text = "QUOTA:\nDISABLED";
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update DisableQuota monitor state: {e.Message}");
            }
        }
    }
}
