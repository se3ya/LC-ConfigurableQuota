using System;
using HarmonyLib;
using UnityEngine;

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
                Plugin.Log.LogWarning($"ApplyPenalty display patch failed: {e.Message}");
            }
        }
    }
}

