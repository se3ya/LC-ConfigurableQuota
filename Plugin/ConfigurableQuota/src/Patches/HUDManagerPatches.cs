using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using BepInEx.Bootstrap;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal static class HudQuotaAnimationPatch
    {
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

            if (TimeOfDay.Instance == null) yield break;

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
            TryApplyAdvancedFeaturesEndscreen();
        }

        internal static void TryApplyAdvancedFeaturesEndscreen()
        {
            if (!ConfigManager.ScrapLossEnabled.Value) return;
            if (!Chainloader.PluginInfos.ContainsKey("com.example.Advancedfeatures")) return;
            if (!PenaltiesOnLandingPatch.HasAllDeadSnapshot) return;

            try
            {
                var hud = HUDManager.Instance;
                bool isAllDead = hud?.statsUIElements?.allPlayersDeadOverlay != null
                    && hud.statsUIElements.allPlayersDeadOverlay.enabled;

                if (!isAllDead) return;

                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "AdvancedFeatures");
                if (asm == null) return;

                var endscreen = asm.GetType("AdvancedFeatures.Endscreen");
                if (endscreen == null) return;

                ApplyAdvancedFeaturesEndscreen(endscreen);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not apply Advanced Features end screen compatibility: {e.Message}");
            }
        }

        private static void ApplyAdvancedFeaturesEndscreen(Type endscreenType)
        {
            var areAllDeadField = AccessTools.Field(endscreenType, "AreAllDead");
            var scrapLostField = AccessTools.Field(endscreenType, "ScrapLost");
            var scrapLostTextField = AccessTools.Field(endscreenType, "ScrapLostText");
            var collectedTextField = AccessTools.Field(endscreenType, "CollectedText");
            var totalTextField = AccessTools.Field(endscreenType, "TotalText");
            var collectedLineField = AccessTools.Field(endscreenType, "CollectedLine");
            var collectedLabelField = AccessTools.Field(endscreenType, "CollectedLabel");

            var scrapLostTransform = scrapLostField?.GetValue(null) as Transform;

            if (PenaltiesOnLandingPatch.TryGetScrapLossSummary(out int beforeValue, out int afterValue, out float lostPercent))
            {
                int lostValue = Mathf.Max(0, beforeValue - afterValue);
                int percentRounded = Mathf.RoundToInt(Mathf.Clamp01(lostPercent) * 100f);
                string displayText = $"Lost {percentRounded}% scrap (${lostValue}/{beforeValue})";

                var collectedText = collectedTextField?.GetValue(null) as Component;
                var totalText = totalTextField?.GetValue(null) as Component;
                var collectedLineTransform = collectedLineField?.GetValue(null) as Transform;
                var collectedLabelTransform = collectedLabelField?.GetValue(null) as Transform;
                object? scrapLostText = scrapLostTextField?.GetValue(null);
                var textProperty = scrapLostText?.GetType().GetProperty("text");

                if (collectedText != null) collectedText.gameObject.SetActive(false);
                if (totalText != null) totalText.gameObject.SetActive(false);
                if (collectedLineTransform != null) collectedLineTransform.gameObject.SetActive(false);
                if (collectedLabelTransform != null) collectedLabelTransform.gameObject.SetActive(false);
                if (scrapLostTransform != null) scrapLostTransform.gameObject.SetActive(true);

                textProperty?.SetValue(scrapLostText, displayText);

                Plugin.Log.LogInfo($"Updated Advanced Features scrap-loss text: {percentRounded}% (${lostValue}/{beforeValue}).");
            }
            else
            {
                if (scrapLostTransform != null) scrapLostTransform.gameObject.SetActive(false);
            }

            areAllDeadField?.SetValue(null, false);
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
                var (dead, total, recovered) = ResolvePenaltyCounts(playersDead, bodiesInsured);
                bool atCompany = PenaltyHelpers.IsOnGordion();

                var (creditPct, creditLoss) = ComputeCreditPenalty(dead, total, recovered, atCompany);
                var (quotaPct, quotaDelta) = ComputeQuotaPenalty(dead, total, recovered, atCompany);

                __instance.statsUIElements.penaltyAddition.text = BuildPenaltyText(dead, recovered, creditPct, quotaPct, quotaDelta);
                __instance.statsUIElements.penaltyTotal.text = creditLoss > 0 ? $"DUE: ${creditLoss}" : "";
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update penalty text on the end screen: {e.Message}");
            }
        }

        private static (int dead, int total, int recovered) ResolvePenaltyCounts(int playersDead, int bodiesInsured)
        {
            if (PenaltiesOnLandingPatch.HasPenaltyCache)
                return (PenaltiesOnLandingPatch.CachedDead, PenaltiesOnLandingPatch.CachedTotal, PenaltiesOnLandingPatch.CachedRecovered);

            var (dead, total, recovered) = PenaltyHelpers.CountDeathsAndRecovered();
            if (dead == 0 && playersDead > 0)
            {
                dead = playersDead;
                recovered = bodiesInsured;
                total = Mathf.Max(dead + 1, total);
            }
            return (dead, total, recovered);
        }

        private static (float pct, int loss) ComputeCreditPenalty(int dead, int total, int recovered, bool atCompany)
        {
            if (!ConfigManager.CreditPenaltiesEnabled.Value || dead == 0 || (atCompany && !ConfigManager.CreditPenaltiesOnGordion.Value))
                return (0f, 0);

            var term = UnityEngine.Object.FindObjectOfType<Terminal>();
            int credits = term?.groupCredits ?? 0;
            float pct = PenaltyHelpers.ComputePenaltyPercent(
                ConfigManager.CreditPenaltiesDynamic.Value,
                ConfigManager.CreditPenaltyPercentPerPlayer.Value,
                ConfigManager.CreditPenaltyPercentCap.Value,
                ConfigManager.CreditPenaltyPercentThreshold.Value,
                ConfigManager.CreditPenaltyRecoveryBonus.Value,
                dead, total, recovered);
            return (pct, Mathf.RoundToInt(credits * pct));
        }

        private static (float pct, int delta) ComputeQuotaPenalty(int dead, int total, int recovered, bool atCompany)
        {
            if (!ConfigManager.QuotaPenaltiesEnabled.Value || dead == 0 || (atCompany && !ConfigManager.QuotaPenaltiesOnGordion.Value))
                return (0f, 0);

            float pct = PenaltyHelpers.ComputePenaltyPercent(
                ConfigManager.QuotaPenaltiesDynamic.Value,
                ConfigManager.QuotaPenaltyPercentPerPlayer.Value,
                ConfigManager.QuotaPenaltyPercentCap.Value,
                ConfigManager.QuotaPenaltyPercentThreshold.Value,
                ConfigManager.QuotaPenaltyRecoveryBonus.Value,
                dead, total, recovered);

            int delta = PenaltiesOnLandingPatch.CachedQuotaPenaltyDelta;
            if (delta <= 0)
            {
                var tod = TimeOfDay.Instance;
                delta = tod != null ? Mathf.RoundToInt(Mathf.Max(1, tod.profitQuota) * pct) : 0;
            }
            return (pct, delta);
        }

        private static string BuildPenaltyText(int dead, int recovered, float creditPct, float quotaPct, int quotaDelta)
        {
            string line1 = creditPct > 0f
                ? $"{dead} casualties: -{Mathf.RoundToInt(creditPct * 100)}%"
                : $"{dead} casualties";
            string text = line1 + $"\n({recovered} of {dead} bodies recovered.)";
            if (quotaPct > 0f)
                text += $"\n\nQuota: {Mathf.RoundToInt(quotaPct * 100)}% (${quotaDelta})";
            return text;
        }
    }
}

