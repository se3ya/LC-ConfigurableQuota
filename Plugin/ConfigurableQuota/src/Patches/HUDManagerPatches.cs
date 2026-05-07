using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using BepInEx.Bootstrap;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal static class HudQuotaAnimationPatch
    {
        private static bool _advancedFeaturesScrapRoutineRunning;

        [HarmonyPatch("rackUpNewQuotaText")]
        [HarmonyPrefix]
        private static bool RackUpNewQuotaText_Prefix(HUDManager __instance, ref System.Collections.IEnumerator __result)
        {
            float speed = Mathf.Clamp(ConfigManager.QuotaAnimationSpeed.Value, 0.1f, 2f);
            __result = CustomRackUp(__instance, speed);
            return false;
        }

        private static System.Collections.IEnumerator CustomRackUp(HUDManager hud, float speed)
        {
            yield return new WaitForSeconds(3.5f / speed);
            int quotaTextAmount = 0;
            int target = TimeOfDay.Instance.profitQuota;
            while (quotaTextAmount < target)
            {
                float step = Time.deltaTime * 250f * speed;
                quotaTextAmount = (int)Mathf.Clamp(quotaTextAmount + step, quotaTextAmount + 3, target + 10);
                hud.newProfitQuotaText.text = "$" + quotaTextAmount.ToString();
                yield return null;
            }
            hud.newProfitQuotaText.text = "$" + target.ToString();
            TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
            hud.UIAudio.PlayOneShot(hud.newProfitQuotaSFX);
            yield return new WaitForSeconds(1.25f / Mathf.Clamp(speed, 0.5f, 2f));
            hud.displayingNewQuota = false;
            hud.reachedProfitQuotaAnimator.SetBool("display", false);
        }

        [HarmonyPatch("FillEndGameStats")]
        [HarmonyPostfix]
        [HarmonyAfter(new[] { "com.example.Advancedfeatures" })]
        private static void FillEndGameStats_Postfix()
        {
            if (!ConfigManager.ScrapLossEnabled.Value) return;
            if (!Chainloader.PluginInfos.ContainsKey("com.example.Advancedfeatures")) return;

            try
            {
                var hud = HUDManager.Instance;
                bool isAllDead = hud?.statsUIElements?.allPlayersDeadOverlay != null
                    && hud.statsUIElements.allPlayersDeadOverlay.enabled;

                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "AdvancedFeatures");
                if (asm == null) return;

                var endscreen = asm.GetType("AdvancedFeatures.Endscreen");
                var areAllDeadField = endscreen?.GetField("AreAllDead",
                    BindingFlags.Public | BindingFlags.Static);

                if (isAllDead && endscreen != null)
                {
                    areAllDeadField?.SetValue(null, true);

                    if (Plugin.Instance != null && !_advancedFeaturesScrapRoutineRunning)
                    {
                        Plugin.Instance.StartCoroutine(
                            WaitAndApplyAdvancedFeaturesScrapLoss(endscreen)
                        );
                    }
                    return;
                }

                if (!isAllDead)
                    areAllDeadField?.SetValue(null, false);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not apply Advanced Features end screen compatibility: {e.Message}");
            }
        }

        private static System.Collections.IEnumerator WaitAndApplyAdvancedFeaturesScrapLoss(Type endscreenType)
        {
            if (_advancedFeaturesScrapRoutineRunning)
                yield break;

            _advancedFeaturesScrapRoutineRunning = true;

            try
            {
                var areAllDeadField = endscreenType.GetField("AreAllDead", BindingFlags.Public | BindingFlags.Static);
                var containerField = endscreenType.GetField("Container", BindingFlags.NonPublic | BindingFlags.Static);
                var scrapLostField = endscreenType.GetField("ScrapLost", BindingFlags.NonPublic | BindingFlags.Static);
                var scrapLostTextField = endscreenType.GetField("ScrapLostText", BindingFlags.NonPublic | BindingFlags.Static);
                var collectedTextField = endscreenType.GetField("CollectedText", BindingFlags.NonPublic | BindingFlags.Static);
                var totalTextField = endscreenType.GetField("TotalText", BindingFlags.NonPublic | BindingFlags.Static);
                var collectedLineField = endscreenType.GetField("CollectedLine", BindingFlags.NonPublic | BindingFlags.Static);
                var collectedLabelField = endscreenType.GetField("CollectedLabel", BindingFlags.NonPublic | BindingFlags.Static);

                bool appliedAtLeastOnce = false;
                int lastPercent = -1;
                int lastLost = -1;
                int lastBefore = -1;
                float timeout = Time.realtimeSinceStartup + 20f;
                while (Time.realtimeSinceStartup < timeout)
                {
                    var container = containerField?.GetValue(null) as GameObject;
                    bool containerActive = container != null && container.activeInHierarchy;

                    if (!containerActive && appliedAtLeastOnce)
                    {
                        break;
                    }

                    if (!PenaltiesOnLandingPatch.TryGetScrapLossSummary(out var beforeValue, out var afterValue, out var lostPercent))
                    {
                        yield return null;
                        continue;
                    }

                    int lostValue = Mathf.Max(0, beforeValue - afterValue);
                    string displayText = $"Lost {Mathf.RoundToInt(Mathf.Clamp01(lostPercent) * 100f)}% scrap (${lostValue}/{beforeValue})";

                    areAllDeadField?.SetValue(null, true);

                    var scrapLostTransform = scrapLostField?.GetValue(null) as Transform;
                    var collectedLineTransform = collectedLineField?.GetValue(null) as Transform;
                    var collectedLabelTransform = collectedLabelField?.GetValue(null) as Transform;
                    var collectedText = collectedTextField?.GetValue(null) as Component;
                    var totalText = totalTextField?.GetValue(null) as Component;
                    object? scrapLostText = scrapLostTextField?.GetValue(null);
                    var textProperty = scrapLostText?.GetType().GetProperty("text");

                    if (collectedText != null) collectedText.gameObject.SetActive(false);
                    if (totalText != null) totalText.gameObject.SetActive(false);
                    if (collectedLineTransform != null) collectedLineTransform.gameObject.SetActive(false);
                    if (collectedLabelTransform != null) collectedLabelTransform.gameObject.SetActive(false);
                    if (scrapLostTransform != null) scrapLostTransform.gameObject.SetActive(true);

                    textProperty?.SetValue(scrapLostText, displayText);

                    appliedAtLeastOnce = true;
                    int percentRounded = Mathf.RoundToInt(lostPercent * 100f);
                    if (percentRounded != lastPercent || lostValue != lastLost || beforeValue != lastBefore)
                    {
                        lastPercent = percentRounded;
                        lastLost = lostValue;
                        lastBefore = beforeValue;
                        Plugin.Log.LogInfo($"Updated Advanced Features scrap-loss text: {percentRounded}% (${lostValue}/{beforeValue}).");
                    }

                    yield return null;
                }
            }
            finally
            {
                _advancedFeaturesScrapRoutineRunning = false;
            }
        }

        [HarmonyPatch("ApplyPenalty")]
        [HarmonyPrefix]
        private static bool ApplyPenalty_Prefix() => false;

        [HarmonyPatch("ApplyPenalty")]
        [HarmonyPostfix]
        private static void ApplyPenalty_Postfix(HUDManager __instance, int playersDead, int bodiesInsured)
        {
            try
            {
                int dead, total, recovered;
                if (PenaltiesOnLandingPatch.HasPenaltyCache)
                {
                    dead = PenaltiesOnLandingPatch.CachedDead;
                    total = PenaltiesOnLandingPatch.CachedTotal;
                    recovered = PenaltiesOnLandingPatch.CachedRecovered;
                }
                else
                {
                    (dead, total, recovered) = PenaltyHelpers.CountDeathsAndRecovered();
                    if (dead == 0 && playersDead > 0)
                    {
                        dead = playersDead;
                        recovered = bodiesInsured;
                        total = Mathf.Max(dead + 1, total);
                    }
                }

                bool atCompany = PenaltyHelpers.IsOnGordion();

                // credit penalty
                float creditPct = 0f;
                int creditLoss = 0;
                bool creditActive = ConfigManager.CreditPenaltiesEnabled.Value
                    && dead > 0
                    && (!atCompany || ConfigManager.CreditPenaltiesOnGordion.Value);

                if (creditActive)
                {
                    var term = UnityEngine.Object.FindObjectOfType<Terminal>();
                    int credits = term?.groupCredits ?? 0;
                    creditPct = PenaltyHelpers.ComputePenaltyPercent(
                        ConfigManager.CreditPenaltiesDynamic.Value,
                        ConfigManager.CreditPenaltyPercentPerPlayer.Value,
                        ConfigManager.CreditPenaltyPercentCap.Value,
                        ConfigManager.CreditPenaltyPercentThreshold.Value,
                        ConfigManager.CreditPenaltyRecoveryBonus.Value,
                        dead, total, recovered);
                    creditLoss = Mathf.RoundToInt(credits * creditPct);
                }

                // quota penalty
                float quotaPct = 0f;
                int quotaDelta = 0;
                bool quotaActive = ConfigManager.QuotaPenaltiesEnabled.Value
                    && dead > 0
                    && (!atCompany || ConfigManager.QuotaPenaltiesOnGordion.Value);

                if (quotaActive)
                {
                    quotaPct = PenaltyHelpers.ComputePenaltyPercent(
                        ConfigManager.QuotaPenaltiesDynamic.Value,
                        ConfigManager.QuotaPenaltyPercentPerPlayer.Value,
                        ConfigManager.QuotaPenaltyPercentCap.Value,
                        ConfigManager.QuotaPenaltyPercentThreshold.Value,
                        ConfigManager.QuotaPenaltyRecoveryBonus.Value,
                        dead, total, recovered);
                    var tod = TimeOfDay.Instance;
                    if (tod != null)
                        quotaDelta = Mathf.RoundToInt(Mathf.Max(1, tod.profitQuota) * quotaPct);
                }

                string line1 = creditPct > 0f
                    ? $"{dead} casualties: -{Mathf.RoundToInt(creditPct * 100)}%"
                    : $"{dead} casualties";
                string line2 = $"({recovered} of {dead} bodies recovered.)";
                string text = line1 + "\n" + line2;

                if (quotaPct > 0f)
                    text += $"\n\nQuota: {Mathf.RoundToInt(quotaPct * 100)}% (${quotaDelta})";

                __instance.statsUIElements.penaltyAddition.text = text;

                __instance.statsUIElements.penaltyTotal.text = creditLoss > 0
                    ? $"DUE: ${creditLoss}"
                    : "";
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update penalty text on the end screen: {e.Message}");
            }
        }
    }
}

