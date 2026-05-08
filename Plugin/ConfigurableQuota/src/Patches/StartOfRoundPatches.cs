using System;
using HarmonyLib;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal static class StartOfRoundPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void ApplyStartingCredits(Terminal __instance)
        {
            try
            {
                int desired = ConfigManager.StartingCredits.Value;

                if (desired >= 0)
                {
                    __instance.groupCredits = desired;
                    Plugin.Log.LogInfo($"Starting credits set to {desired}.");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not apply starting credits: {e.Message}");
            }
        }
    }
}
