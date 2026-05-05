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
    }
}
