using System;
using System.Reflection;
using HarmonyLib;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal static class StartOfRoundPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void ApplyStartingCredits(StartOfRound __instance)
        {
            try
            {
                int desired = ConfigManager.StartingCredits.Value;

                if (desired < 0)
                    return;

                var type = __instance.GetType();
                var field = type.GetField("groupCredits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            ?? type.GetField("companyCredits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null && field.FieldType == typeof(int))
                {
                    field.SetValue(__instance, desired);
                    Plugin.Log.LogInfo($"[ConfigurableQuota] Applied starting credits: {desired}");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to apply StartingCredits: {e.Message}");
            }
        }
    }
}
