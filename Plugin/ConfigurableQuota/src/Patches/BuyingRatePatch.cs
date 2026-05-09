using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(TimeOfDay))]
    internal static class BuyingRatePatch
    {
        private const float ReapplyDelaySeconds = 3f;

        [HarmonyPatch(nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        private static void SetBuyingRateForDay_Postfix(TimeOfDay __instance)
        {
            try
            {
                if (!__instance.IsServer) return;
                if (ConfigManager.DisableQuota.Value) return;

                var sor = StartOfRound.Instance;
                if (sor == null) return;

                float vanillaRate = sor.companyBuyingRate;
                int daysUntilDeadline = __instance.daysUntilDeadline;

                (float rate, bool isJackpot, string source) = ResolveRate(vanillaRate, daysUntilDeadline);

                sor.companyBuyingRate = rate;

                int rounded = Mathf.RoundToInt(rate * 100f);
                Plugin.Log.LogInfo($"Buy rate set to {rounded}% ({source}, jackpot={isJackpot}).");

                __instance.StartCoroutine(ReapplyBuyRate(rate, ReapplyDelaySeconds));

                NetworkSync.SyncBuyingRateToClients(rate, isJackpot);

                DisplayBuyRateAlert(rate, isJackpot);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not apply buy rate modifiers: {e.Message}");
            }
        }

        internal static void ApplyReceivedBuyingRate(float rate, bool isJackpot)
        {
            var sor = StartOfRound.Instance;
            if (sor != null)
                sor.companyBuyingRate = rate;

            DisplayBuyRateAlert(rate, isJackpot);
        }

        private static (float rate, bool isJackpot, string source) ResolveRate(float vanillaRate, int daysUntilDeadline)
        {
            bool minMax = ConfigManager.MinMaxRateEnabled.Value;
            float minRate = ConfigManager.MinRate.Value;
            float maxRate = Mathf.Max(minRate, ConfigManager.MaxRate.Value);

            bool jackpotEnabled = ConfigManager.JackpotEnabled.Value;
            bool jackpotLDOnly = ConfigManager.JackpotLastDayOnly.Value;
            float jackpotChance = Mathf.Clamp01(ConfigManager.JackpotChance.Value);
            float jackpotMin = ConfigManager.JackpotMinRate.Value;
            float jackpotMax = Mathf.Max(jackpotMin, ConfigManager.JackpotMaxRate.Value);

            bool lastDayEnabled = ConfigManager.LastDayRateEnabled.Value;
            float lastDayChance = Mathf.Clamp01(ConfigManager.LastDayRangeChance.Value);
            float lastDayMin = ConfigManager.LastDayMinRate.Value;
            float lastDayMax = Mathf.Max(lastDayMin, ConfigManager.LastDayMaxRate.Value);

            bool randomEnabled = ConfigManager.RandomRateEnabled.Value;

            bool isLastDay = daysUntilDeadline == 0;

            if (jackpotEnabled && (!jackpotLDOnly || isLastDay) && UnityEngine.Random.value <= jackpotChance)
            {
                float rate = (jackpotMin == jackpotMax)
                    ? jackpotMin
                    : UnityEngine.Random.Range(jackpotMin, jackpotMax);
                return (rate, true, jackpotLDOnly ? "jackpot last-day" : "jackpot any-day");
            }

            if (lastDayEnabled && isLastDay)
            {
                if (lastDayMin == lastDayMax)
                    return (lastDayMin, false, "last-day fixed");

                if (UnityEngine.Random.value <= lastDayChance)
                    return (UnityEngine.Random.Range(lastDayMin, lastDayMax), false, "last-day ranged");

                return (1f, false, "last-day fallback");
            }

            if (randomEnabled && minMax)
                return (UnityEngine.Random.Range(minRate, maxRate), false, "random in range");

            if (minMax)
                return (Mathf.Clamp(vanillaRate, minRate, maxRate), false, "min/max clamp");

            return (vanillaRate, false, "vanilla");
        }

        private static IEnumerator ReapplyBuyRate(float rate, float delay)
        {
            yield return new WaitForSeconds(delay);

            var sor = StartOfRound.Instance;
            if (sor == null) yield break;

            if (!Mathf.Approximately(sor.companyBuyingRate, rate))
            {
                sor.companyBuyingRate = rate;
                Plugin.Log.LogDebug($"Re-applied buy rate {Mathf.RoundToInt(rate * 100f)}% after {delay:0.#}s.");
            }
        }

        internal static void DisplayBuyRateAlert(float rate, bool isJackpot)
        {
            try
            {
                var hud = HUDManager.Instance;
                var tod = TimeOfDay.Instance;
                if (hud == null || tod == null) return;

                bool wantAlert = isJackpot
                    ? ConfigManager.JackpotAlertEnabled.Value
                    : ConfigManager.BuyRateAlertEnabled.Value;
                if (!wantAlert) return;

                int rounded = Mathf.RoundToInt(rate * 100f);
                float delay = Mathf.Max(0f, ConfigManager.AlertDelaySeconds.Value);

                tod.StartCoroutine(AlertCoroutine(rounded, isJackpot, delay));
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not schedule buy-rate alert: {e.Message}");
            }
        }

        private static IEnumerator AlertCoroutine(int rateRounded, bool isJackpot, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            var hud = HUDManager.Instance;
            if (hud == null) yield break;

            if (isJackpot)
            {
                hud.DisplayTip(
                    "<color=#ffc526>SCRAP EMERGENCY</color>",
                    $"<color=#fcbf17>\n* Buying rates have jumped to {rateRounded}%</color>",
                    true,
                    false,
                    "LC_JackpotTip1"
                );

                if (hud.UIAudio != null && hud.globalNotificationSFX != null)
                    hud.UIAudio.PlayOneShot(hud.globalNotificationSFX);
            }
            else
            {
                hud.DisplayTip(
                    "New Scrap Rate",
                    $"\n* Buying rates have changed to {rateRounded}%",
                    false,
                    false,
                    "LC_JackpotTip2"
                );
            }
        }
    }
}
